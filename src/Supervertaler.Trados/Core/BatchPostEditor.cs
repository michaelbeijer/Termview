using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Supervertaler.Trados.Models;
using Supervertaler.Trados.Settings;

namespace Supervertaler.Trados.Core
{
    // ─── Event Args ──────────────────────────────────────────

    public class PostEditSegmentResultEventArgs : EventArgs
    {
        public int SegmentIndex { get; set; }
        public string CorrectedText { get; set; }
        public bool WasChanged { get; set; }
        public object SegmentPairRef { get; set; }

        /// <summary>Whether the source segment had inline tags.</summary>
        public bool HasTags { get; set; }

        /// <summary>Tag map for reconstructing tags in the target segment.</summary>
        public Dictionary<int, TagInfo> TagMap { get; set; }
    }

    public class PostEditCompletedEventArgs : EventArgs
    {
        public int TotalProcessed { get; set; }
        public int Changed { get; set; }
        public int Unchanged { get; set; }
        public int Failed { get; set; }
        public TimeSpan TotalTime { get; set; }
        public bool WasCancelled { get; set; }
    }

    // ─── Engine ──────────────────────────────────────────────

    /// <summary>
    /// Core batch post-editing engine. Sends source + existing target to the AI,
    /// which returns corrected text or [NO CHANGE]. UI-agnostic — communicates via events.
    /// </summary>
    public class BatchPostEditor
    {
        public event EventHandler<BatchProgressEventArgs> Progress;
        public event EventHandler<PostEditCompletedEventArgs> Completed;
        public event EventHandler<PostEditSegmentResultEventArgs> SegmentPostEdited;

        /// <summary>
        /// Post-edits segments in batches using the configured LLM provider.
        /// </summary>
        public async Task PostEditAsync(
            List<BatchSegment> segments,
            string sourceLang,
            string targetLang,
            PostEditLevel level,
            AiSettings aiSettings,
            List<TermEntry> termbaseTerms,
            int batchSize,
            CancellationToken cancellationToken,
            string customPromptContent = null)
        {
            var sw = Stopwatch.StartNew();
            int changed = 0;
            int unchanged = 0;
            int failed = 0;

            if (segments == null || segments.Count == 0)
            {
                Completed?.Invoke(this, new PostEditCompletedEventArgs
                {
                    TotalTime = sw.Elapsed
                });
                return;
            }

            // Build system prompt
            var systemPrompt = PostEditPrompt.BuildSystemPrompt(
                sourceLang, targetLang, level, termbaseTerms, customPromptContent);

            // Resolve provider settings
            var provider = aiSettings.SelectedProvider ?? LlmModels.ProviderOpenAi;
            var model = aiSettings.GetSelectedModel();
            var apiKey = LlmClient.ResolveApiKey(provider, aiSettings.ApiKeys);
            string baseUrl = null;

            if (provider == LlmModels.ProviderOllama)
                baseUrl = aiSettings.OllamaEndpoint ?? "http://localhost:11434";
            else if (provider == LlmModels.ProviderCustomOpenAi)
            {
                var profile = aiSettings.GetActiveCustomProfile();
                if (profile != null)
                {
                    baseUrl = profile.Endpoint;
                    model = profile.Model;
                    apiKey = profile.ApiKey;
                }
            }

            var modelInfo = LlmModels.FindModel(model);
            int maxTokens = modelInfo?.DefaultMaxTokens ?? 16384;

            if (batchSize <= 0) batchSize = 10;

            int totalBatches = (segments.Count + batchSize - 1) / batchSize;

            RaiseProgress(0, segments.Count, "Starting post-editing...", false, TimeSpan.Zero);

            using (var client = new LlmClient(provider, model, apiKey, baseUrl, maxTokens))
            {
                for (int batchNum = 0; batchNum < totalBatches; batchNum++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    int startIdx = batchNum * batchSize;
                    int endIdx = Math.Min(startIdx + batchSize, segments.Count);
                    int batchCount = endIdx - startIdx;

                    RaiseProgress(startIdx, segments.Count,
                        $"Post-editing batch {batchNum + 1}/{totalBatches} " +
                        $"(segments {startIdx + 1}\u2013{endIdx})...",
                        false, sw.Elapsed);

                    try
                    {
                        var batchSw = Stopwatch.StartNew();

                        // Build source-target pairs for prompt
                        var promptSegments = new List<(int number, string source, string target)>();
                        for (int i = startIdx; i < endIdx; i++)
                        {
                            promptSegments.Add((
                                number: i + 1,
                                source: segments[i].SourceText,
                                target: segments[i].ExistingTarget
                            ));
                        }

                        // Build user prompt
                        var userPrompt = PostEditPrompt.BuildBatchUserPrompt(promptSegments);

                        // Call LLM
                        var response = await client.SendPromptAsync(
                            userPrompt, systemPrompt, maxTokens, cancellationToken,
                            feature: PromptLogFeature.PostEdit);

                        // Parse response (reuse translation parser)
                        var parsed = TranslationPrompt.ParseBatchResponse(response, batchCount);

                        // Map parsed results back to segments
                        var resultMap = new Dictionary<int, string>();
                        foreach (var p in parsed)
                            resultMap[p.Number] = p.Translation;

                        int batchChanged = 0;
                        int batchUnchanged = 0;
                        int batchFailed = 0;

                        for (int i = startIdx; i < endIdx; i++)
                        {
                            int number = i + 1;

                            string result;
                            if (resultMap.TryGetValue(number, out result)
                                && !string.IsNullOrWhiteSpace(result))
                            {
                                bool isNoChange = PostEditPrompt.IsNoChange(result);

                                // Also treat as no-change if AI returned identical text
                                if (!isNoChange && !string.IsNullOrEmpty(segments[i].ExistingTarget))
                                {
                                    var normalised = result.Trim();
                                    var existingNormalised = segments[i].ExistingTarget.Trim();
                                    if (string.Equals(normalised, existingNormalised, StringComparison.Ordinal))
                                        isNoChange = true;
                                }

                                SegmentPostEdited?.Invoke(this, new PostEditSegmentResultEventArgs
                                {
                                    SegmentIndex = segments[i].Index,
                                    CorrectedText = isNoChange ? null : result,
                                    WasChanged = !isNoChange,
                                    SegmentPairRef = segments[i].SegmentPairRef,
                                    HasTags = segments[i].HasTags,
                                    TagMap = segments[i].TagMap
                                });

                                if (isNoChange)
                                {
                                    batchUnchanged++;
                                    unchanged++;
                                }
                                else
                                {
                                    batchChanged++;
                                    changed++;
                                }
                            }
                            else
                            {
                                batchFailed++;
                                failed++;
                            }

                            RaiseProgress(i + 1, segments.Count, null, false, sw.Elapsed);
                        }

                        batchSw.Stop();
                        RaiseProgress(endIdx, segments.Count,
                            $"\u2713 Batch {batchNum + 1} complete: " +
                            $"{batchChanged} changed, {batchUnchanged} unchanged" +
                            (batchFailed > 0 ? $", {batchFailed} failed" : "") +
                            $" ({batchSw.Elapsed.TotalSeconds:F1}s)",
                            false, sw.Elapsed);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        failed += batchCount;
                        RaiseProgress(endIdx, segments.Count,
                            $"\u2717 \u2717 Batch {batchNum + 1} failed: {ex.Message}",
                            true, sw.Elapsed);
                    }
                }
            }

            sw.Stop();

            Completed?.Invoke(this, new PostEditCompletedEventArgs
            {
                TotalProcessed = changed + unchanged + failed,
                Changed = changed,
                Unchanged = unchanged,
                Failed = failed,
                TotalTime = sw.Elapsed,
                WasCancelled = cancellationToken.IsCancellationRequested
            });
        }

        private void RaiseProgress(int current, int total, string message,
            bool isError, TimeSpan elapsed)
        {
            Progress?.Invoke(this, new BatchProgressEventArgs
            {
                Current = current,
                Total = total,
                Message = message,
                IsError = isError,
                Elapsed = elapsed
            });
        }
    }
}
