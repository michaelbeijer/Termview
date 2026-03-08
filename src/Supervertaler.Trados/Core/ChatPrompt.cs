using System.Collections.Generic;
using System.Linq;
using System.Text;
using Supervertaler.Trados.Models;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Builds the system prompt for the AI Chat Assistant.
    /// Separate from TranslationPrompt because the chat assistant is conversational
    /// rather than a pure translator — it needs different persona and guidelines.
    /// </summary>
    public static class ChatPrompt
    {
        /// <summary>
        /// Builds a context-aware system prompt for the AI chat assistant.
        /// Includes the current segment, language pair, matched terminology, and TM matches.
        /// Called fresh on each message send so the LLM always sees the latest segment.
        /// </summary>
        public static string BuildSystemPrompt(
            string sourceLang,
            string targetLang,
            string sourceText,
            string targetText,
            List<TermPickerMatch> matchedTerms,
            List<TmMatch> tmMatches = null)
        {
            var sb = new StringBuilder(2048);

            sb.AppendLine("You are a professional translation assistant integrated into Trados Studio.");
            sb.AppendLine("You help translators with their work by answering questions about translations,");
            sb.AppendLine("suggesting improvements, explaining terminology, and providing context.");

            // Language pair
            if (!string.IsNullOrEmpty(sourceLang) && !string.IsNullOrEmpty(targetLang))
            {
                sb.AppendLine();
                sb.AppendLine("# CURRENT CONTEXT");
                sb.Append("- Language pair: ").Append(sourceLang).Append(" \u2192 ").AppendLine(targetLang);
            }

            // Current segment
            if (!string.IsNullOrEmpty(sourceText))
            {
                sb.AppendLine();
                sb.AppendLine("## Current Source Segment");
                sb.AppendLine(sourceText);
            }

            if (!string.IsNullOrEmpty(targetText))
            {
                sb.AppendLine();
                sb.AppendLine("## Current Target Segment");
                sb.AppendLine(targetText);
            }

            // TM matches
            if (tmMatches != null && tmMatches.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Translation Memory Matches");
                foreach (var tm in tmMatches)
                {
                    sb.Append("- ").Append(tm.MatchPercentage).Append("% match");
                    if (!string.IsNullOrEmpty(tm.TmName))
                        sb.Append(" (").Append(tm.TmName).Append(")");
                    sb.AppendLine(":");
                    sb.Append("  Source: ").AppendLine(tm.SourceText);
                    sb.Append("  Target: ").AppendLine(tm.TargetText);
                }
            }

            // Matched terminology
            if (matchedTerms != null && matchedTerms.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Matched Terminology");
                foreach (var match in matchedTerms)
                {
                    var targets = match.GetAllTargets();
                    if (targets.Count > 0)
                    {
                        var primary = targets[0];
                        var entry = match.PrimaryEntry;
                        sb.Append("- ").Append(match.SourceText).Append(" \u2192 ").Append(primary.TargetTerm);

                        if (entry != null && entry.IsNonTranslatable)
                            sb.Append(" (do not translate)");
                        else if (entry != null && entry.Forbidden)
                            sb.Append(" (\u26a0\ufe0f forbidden)");

                        // Show synonyms if any
                        if (targets.Count > 1)
                        {
                            var synonyms = targets.Skip(1).Select(t => t.TargetTerm);
                            sb.Append(" (also: ").Append(string.Join(", ", synonyms)).Append(")");
                        }

                        sb.AppendLine();
                    }
                }
            }

            // Guidelines
            sb.AppendLine();
            sb.AppendLine("# GUIDELINES");
            sb.AppendLine("- Answer in the language the user writes in");
            sb.AppendLine("- When suggesting translations, be specific and explain your reasoning");
            sb.AppendLine("- Reference the terminology list and TM matches when relevant");
            sb.AppendLine("- Keep answers concise unless the user asks for detail");
            sb.AppendLine("- If asked to translate or improve text, provide the translation/improvement clearly marked on its own line");

            return sb.ToString();
        }
    }
}
