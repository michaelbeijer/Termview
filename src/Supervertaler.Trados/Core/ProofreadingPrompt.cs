using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Supervertaler.Trados.Models;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Builds system and user prompts for AI proofreading, and parses batch responses.
    /// Parallel to TranslationPrompt — uses the same composable prompt assembly pattern.
    /// </summary>
    public static class ProofreadingPrompt
    {
        /// <summary>
        /// Builds the full system prompt for proofreading by composing:
        /// Base proofreading instructions -> Language-specific checks -> Termbase -> Custom prompt.
        /// </summary>
        /// <param name="sourceLang">Source language display name</param>
        /// <param name="targetLang">Target language display name</param>
        /// <param name="terms">Optional termbase terms to check against</param>
        /// <param name="customPromptContent">Optional custom prompt content (already variable-substituted)</param>
        public static string BuildSystemPrompt(string sourceLang, string targetLang,
            List<TermEntry> terms = null, string customPromptContent = null,
            List<string> documentSegments = null, int maxDocumentSegments = 500,
            bool includeTermMetadata = true)
        {
            var sb = new StringBuilder(4096);

            // Persona and task
            sb.AppendLine("You are an expert " + sourceLang + " to " + targetLang +
                " translation quality reviewer with deep understanding of both languages.");
            sb.AppendLine();
            sb.AppendLine("**YOUR TASK**: Review the source-target segment pairs below and identify quality issues in the translations.");
            sb.AppendLine();

            // Quality check categories
            sb.AppendLine("**CHECK FOR THE FOLLOWING ISSUES**:");
            sb.AppendLine("1. **Accuracy** \u2014 Does the translation convey the same meaning as the source?");
            sb.AppendLine("2. **Completeness** \u2014 Is anything missing or added that shouldn't be?");
            sb.AppendLine("3. **Terminology** \u2014 Are terms translated consistently and correctly?");
            sb.AppendLine("4. **Grammar & Style** \u2014 Is the target grammatically correct and stylistically appropriate?");
            sb.AppendLine("5. **Number Formatting** \u2014 Are numbers, dates, and measurements formatted correctly for the target language?");
            sb.AppendLine();

            // Language-specific checks
            AppendLanguageSpecificChecks(sb, targetLang);

            // Output format
            sb.AppendLine("**OUTPUT FORMAT**:");
            sb.AppendLine("For each segment, respond in EXACTLY this format:");
            sb.AppendLine();
            sb.AppendLine("[SEGMENT 0001] OK");
            sb.AppendLine("[SEGMENT 0002] ISSUE");
            sb.AppendLine("Issue: <description of the problem>");
            sb.AppendLine("Suggestion: <how to fix it>");
            sb.AppendLine();

            // Rules
            sb.AppendLine("**IMPORTANT RULES**:");
            sb.AppendLine("- You MUST respond for EVERY segment in the batch.");
            sb.AppendLine("- Use OK if the translation is correct.");
            sb.AppendLine("- Use ISSUE if there is a problem, followed by Issue: and Suggestion: lines.");
            sb.AppendLine("- Do NOT provide corrected translations \u2014 only describe the issues and suggest fixes.");
            sb.AppendLine("- Be concise but specific in your descriptions.");
            sb.AppendLine("- Do NOT flag stylistic preferences unless they are clear errors.");

            // Termbase injection
            if (terms != null && terms.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("# TERMBASE");
                sb.AppendLine();
                sb.AppendLine("Check that these approved terms are used correctly in the translations:");
                sb.AppendLine();
                foreach (var term in terms)
                {
                    if (string.IsNullOrEmpty(term.SourceTerm) || string.IsNullOrEmpty(term.TargetTerm))
                        continue;

                    if (term.Forbidden)
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 \u26A0\uFE0F DO NOT USE: " + term.TargetTerm);
                    else if (term.IsNonTranslatable)
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 " + term.TargetTerm + " (do not translate)");
                    else
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 " + term.TargetTerm);

                    // Include term metadata (domain, definition, notes)
                    if (includeTermMetadata)
                    {
                        if (!string.IsNullOrWhiteSpace(term.Domain))
                            sb.Append("  Domain: ").AppendLine(term.Domain);
                        if (!string.IsNullOrWhiteSpace(term.Definition))
                            sb.Append("  Definition: ").AppendLine(term.Definition);
                        if (!string.IsNullOrWhiteSpace(term.Notes))
                            sb.Append("  Notes: ").AppendLine(term.Notes);
                    }

                    if (!string.IsNullOrEmpty(term.SourceAbbreviation) && !string.IsNullOrEmpty(term.TargetAbbreviation))
                        sb.AppendLine("- " + term.SourceAbbreviation + " \u2192 " + term.TargetAbbreviation + " (abbreviation of: " + term.SourceTerm + ")");
                }
            }

            // Custom prompt
            if (!string.IsNullOrWhiteSpace(customPromptContent))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("# CUSTOM INSTRUCTIONS");
                sb.AppendLine();
                sb.Append(customPromptContent);
            }

            // Document context (all source segments for document type analysis)
            if (documentSegments != null && documentSegments.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("# DOCUMENT CONTENT");
                sb.AppendLine("The following is the source document content. Analyze it to determine the document type");
                sb.AppendLine("(legal, medical, technical, marketing, financial, patent, scientific, etc.) and use that");
                sb.AppendLine("assessment to inform your quality judgements about terminology, style, and register.");
                sb.AppendLine();

                var max = maxDocumentSegments > 0 ? maxDocumentSegments : 500;

                if (documentSegments.Count <= max)
                {
                    for (int i = 0; i < documentSegments.Count; i++)
                    {
                        sb.Append(i + 1).Append(". ").AppendLine(documentSegments[i]);
                    }
                }
                else
                {
                    int firstCount = (int)(max * 0.8);
                    int lastCount = max - firstCount;
                    int omitted = documentSegments.Count - max;

                    for (int i = 0; i < firstCount; i++)
                    {
                        sb.Append(i + 1).Append(". ").AppendLine(documentSegments[i]);
                    }

                    sb.AppendLine();
                    sb.Append("[... ").Append(omitted).AppendLine(" segments omitted ...]");
                    sb.AppendLine();

                    int startLast = documentSegments.Count - lastCount;
                    for (int i = startLast; i < documentSegments.Count; i++)
                    {
                        sb.Append(i + 1).Append(". ").AppendLine(documentSegments[i]);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Appends language-specific quality checks based on the target language.
        /// </summary>
        private static void AppendLanguageSpecificChecks(StringBuilder sb, string targetLang)
        {
            if (string.IsNullOrEmpty(targetLang))
                return;

            var lower = targetLang.ToLowerInvariant();

            if (lower.Contains("dutch") || lower.Contains("nederland") || lower.Contains("flemish") || lower.Contains("vlaams"))
            {
                sb.AppendLine("**DUTCH-SPECIFIC CHECKS**:");
                sb.AppendLine("- Compound words: check that compound nouns are written as one word (e.g., 'softwareontwikkeling', not 'software ontwikkeling').");
                sb.AppendLine("- dt-errors: verify correct conjugation of verbs ending in -d, -t, or -dt.");
                sb.AppendLine("- de/het: check correct article usage (de/het).");
                sb.AppendLine();
            }
            else if (lower.Contains("german") || lower.Contains("deutsch"))
            {
                sb.AppendLine("**GERMAN-SPECIFIC CHECKS**:");
                sb.AppendLine("- Compound nouns: check that compound nouns are written as one word (e.g., 'Softwareentwicklung', not 'Software Entwicklung').");
                sb.AppendLine("- Capitalization: all nouns must be capitalized.");
                sb.AppendLine("- Cases: check correct use of nominative, accusative, dative, and genitive cases.");
                sb.AppendLine();
            }
            else if (lower.Contains("french") || lower.Contains("fran\u00E7ais"))
            {
                sb.AppendLine("**FRENCH-SPECIFIC CHECKS**:");
                sb.AppendLine("- Accents: verify correct use of accents (\u00E9, \u00E8, \u00EA, \u00EB, \u00E0, \u00E2, etc.).");
                sb.AppendLine("- Gender/number agreement: check that adjectives and past participles agree with their nouns.");
                sb.AppendLine("- Punctuation spacing: ensure spaces before colons, semicolons, question marks, and exclamation marks.");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Builds the user prompt with numbered source-target segment pairs for batch proofreading.
        /// </summary>
        public static string BuildBatchUserPrompt(List<(int number, string source, string target)> segments)
        {
            var sb = new StringBuilder(segments.Count * 300);

            sb.AppendLine("**SEGMENTS TO REVIEW (" + segments.Count + " segments):**");
            sb.AppendLine();

            foreach (var seg in segments)
            {
                sb.AppendLine("[SEGMENT " + seg.number.ToString("D4") + "]");
                sb.AppendLine("Source: " + seg.source);
                sb.AppendLine("Target: " + seg.target);
                sb.AppendLine();
            }

            sb.Append("**YOUR REVIEW (one verdict per segment):**");

            return sb.ToString();
        }

        /// <summary>
        /// Parses a batch proofreading response into structured results.
        /// Tolerant of formatting variations in AI output.
        /// </summary>
        /// <param name="response">Raw AI response text</param>
        /// <param name="batchStartNumber">1-based number of the first segment in this batch</param>
        /// <param name="batchSize">Number of segments expected in this batch</param>
        /// <returns>List of parsed results: (segmentNumber, isOk, issueDescription, suggestion)</returns>
        public static List<(int segmentNumber, bool isOk, string issueDescription, string suggestion)>
            ParseBatchResponse(string response, int batchStartNumber, int batchSize)
        {
            var results = new List<(int, bool, string, string)>();
            if (string.IsNullOrWhiteSpace(response))
                return results;

            var lines = response.Split(new[] { '\n' }, StringSplitOptions.None);

            // Pattern to match segment header: [SEGMENT 0001] OK/ISSUE
            var segmentPattern = new Regex(
                @"^\s*\[SEGMENT\s+(\d+)\]\s*(OK|ISSUE|PASS|WARNING|FAIL|\u2713|\u2717|\u26A0)",
                RegexOptions.IgnoreCase);
            // Pattern to match Issue: line
            var issuePattern = new Regex(@"^\s*Issue\s*:\s*(.+)", RegexOptions.IgnoreCase);
            // Pattern to match Suggestion: line
            var suggestionPattern = new Regex(@"^\s*Suggestion\s*:\s*(.+)", RegexOptions.IgnoreCase);

            int currentNumber = -1;
            bool currentIsOk = true;
            string currentIssue = null;
            string currentSuggestion = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var segMatch = segmentPattern.Match(line);

                if (segMatch.Success)
                {
                    // Flush previous segment if any
                    if (currentNumber >= 0)
                    {
                        results.Add((currentNumber, currentIsOk, currentIssue, currentSuggestion));
                    }

                    currentNumber = int.Parse(segMatch.Groups[1].Value);
                    var verdict = segMatch.Groups[2].Value.Trim().ToUpperInvariant();

                    currentIsOk = verdict == "OK" || verdict == "PASS" || verdict == "\u2713";
                    currentIssue = null;
                    currentSuggestion = null;
                }
                else if (currentNumber >= 0)
                {
                    var issueMatch = issuePattern.Match(line);
                    if (issueMatch.Success)
                    {
                        currentIssue = issueMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    var sugMatch = suggestionPattern.Match(line);
                    if (sugMatch.Success)
                    {
                        currentSuggestion = sugMatch.Groups[1].Value.Trim();
                    }
                }
            }

            // Flush last segment
            if (currentNumber >= 0)
            {
                results.Add((currentNumber, currentIsOk, currentIssue, currentSuggestion));
            }

            return results;
        }
    }
}
