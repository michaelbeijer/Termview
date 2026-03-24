using System.Collections.Generic;
using System.Text;
using Supervertaler.Trados.Models;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Builds system and user prompts for AI post-editing (MTPE).
    /// Sends source + existing target to the AI. The AI returns either the corrected
    /// target or [NO CHANGE] if the translation is acceptable.
    /// Reuses TranslationPrompt.ParseBatchResponse() for parsing numbered responses.
    /// </summary>
    public static class PostEditPrompt
    {
        /// <summary>
        /// The marker returned by the AI when a segment needs no editing.
        /// </summary>
        public const string NoChangeMarker = "[NO CHANGE]";

        /// <summary>
        /// Builds the full system prompt for post-editing.
        /// Composable: Base (with level) → Tag rules → Termbase → Custom prompt.
        /// </summary>
        public static string BuildSystemPrompt(string sourceLang, string targetLang,
            PostEditLevel level,
            List<TermEntry> termbaseTerms = null,
            string customPromptContent = null)
        {
            var sb = new StringBuilder(4096);

            // Persona
            sb.AppendLine("You are an expert " + sourceLang + " to " + targetLang +
                " post-editor. Your task is to review machine-translated segments and " +
                "improve them where necessary.");
            sb.AppendLine();

            // Task
            sb.AppendLine("**YOUR TASK**: For each source-target pair below, review the " +
                "target translation and either return the corrected version or " +
                "`" + NoChangeMarker + "` if the translation is acceptable.");
            sb.AppendLine();

            // Level-specific instructions
            AppendLevelInstructions(sb, level);

            // Tag preservation (reuse from TranslationPrompt)
            sb.AppendLine("**CRITICAL: NUMBERED TAG PLACEHOLDER PRESERVATION**:");
            sb.AppendLine("- Source and target segments may contain numbered tag placeholders:");
            sb.AppendLine("  - Paired tags: <t1>formatted text</t1>, <t2>another format</t2>");
            sb.AppendLine("  - Standalone tags: <t3/> (e.g., page breaks, field codes)");
            sb.AppendLine("- You MUST preserve ALL tag placeholders exactly as they appear");
            sb.AppendLine("- Place paired tags around the CORRESPONDING translated words");
            sb.AppendLine("- Keep standalone tags (<tN/>) in the correct position");
            sb.AppendLine("- Never translate, remove, or modify the tag placeholders themselves");
            sb.AppendLine();

            // Number formatting
            sb.AppendLine("**LANGUAGE-SPECIFIC NUMBER FORMATTING**:");
            sb.AppendLine("- If the target language is Dutch, French, German, Italian, Spanish, or another " +
                "continental European language, use a comma as the decimal separator (e.g., 17,1 cm).");
            sb.AppendLine("- If the target language is English or Irish, use a period as the decimal " +
                "separator (e.g., 17.1 cm).");
            sb.AppendLine("- Follow the number formatting conventions of the target language.");
            sb.AppendLine();

            // Language-specific checks
            AppendLanguageSpecificChecks(sb, targetLang);

            // Output format
            sb.AppendLine("**OUTPUT FORMAT**:");
            sb.AppendLine("- Return a numbered list matching the input segment numbers.");
            sb.AppendLine("- For unchanged segments: `N. " + NoChangeMarker + "`");
            sb.AppendLine("- For corrected segments: `N. <your corrected target text>`");
            sb.AppendLine("- Provide EXACTLY one response per segment.");
            sb.AppendLine("- NO explanations, NO commentary — only the numbered results.");

            // Termbase
            if (termbaseTerms != null && termbaseTerms.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("# TERMBASE");
                sb.AppendLine();
                sb.AppendLine("Ensure these approved terms are used correctly. Flag and fix any deviations:");
                sb.AppendLine();
                foreach (var term in termbaseTerms)
                {
                    if (string.IsNullOrEmpty(term.SourceTerm) || string.IsNullOrEmpty(term.TargetTerm))
                        continue;

                    if (term.Forbidden)
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 \u26A0\uFE0F DO NOT USE: " + term.TargetTerm);
                    else if (term.IsNonTranslatable)
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 " + term.TargetTerm + " (do not translate)");
                    else
                        sb.AppendLine("- " + term.SourceTerm + " \u2192 " + term.TargetTerm);

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

            return sb.ToString().TrimEnd();
        }

        private static void AppendLevelInstructions(StringBuilder sb, PostEditLevel level)
        {
            switch (level)
            {
                case PostEditLevel.Light:
                    sb.AppendLine("**POST-EDIT LEVEL: LIGHT (errors only)**");
                    sb.AppendLine("- Fix ONLY clear errors: mistranslations, omissions, additions, wrong terminology, number formatting errors.");
                    sb.AppendLine("- Do NOT rephrase for style or fluency — if the meaning is correct and the text is understandable, return `" + NoChangeMarker + "`.");
                    sb.AppendLine("- Leave stylistic preferences alone. Your goal is accuracy, not polish.");
                    sb.AppendLine("- When in doubt, return `" + NoChangeMarker + "`.");
                    break;

                case PostEditLevel.Medium:
                    sb.AppendLine("**POST-EDIT LEVEL: MEDIUM (errors + phrasing)**");
                    sb.AppendLine("- Fix errors: mistranslations, omissions, additions, wrong terminology, number formatting.");
                    sb.AppendLine("- Also fix awkward phrasing, unnatural word order, and register mismatches.");
                    sb.AppendLine("- Preserve the original translation's structure when it is correct.");
                    sb.AppendLine("- Return `" + NoChangeMarker + "` for translations that read naturally and convey the correct meaning.");
                    break;

                case PostEditLevel.Heavy:
                    sb.AppendLine("**POST-EDIT LEVEL: HEAVY (full polish)**");
                    sb.AppendLine("- Fix all errors and improve fluency to publication quality.");
                    sb.AppendLine("- Optimise word choice, ensure idiomatic expression, improve readability.");
                    sb.AppendLine("- Only return `" + NoChangeMarker + "` for translations that are already publication-ready.");
                    sb.AppendLine("- The output should read as if written by a native speaker, not translated.");
                    break;
            }

            sb.AppendLine();
        }

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
                sb.AppendLine("- de/het: check correct article usage.");
                sb.AppendLine();
            }
            else if (lower.Contains("german") || lower.Contains("deutsch"))
            {
                sb.AppendLine("**GERMAN-SPECIFIC CHECKS**:");
                sb.AppendLine("- Compound nouns: check that compound nouns are written as one word.");
                sb.AppendLine("- Capitalization: all nouns must be capitalized.");
                sb.AppendLine("- Cases: check correct use of nominative, accusative, dative, and genitive.");
                sb.AppendLine();
            }
            else if (lower.Contains("french") || lower.Contains("fran\u00E7ais"))
            {
                sb.AppendLine("**FRENCH-SPECIFIC CHECKS**:");
                sb.AppendLine("- Accents: verify correct use of accents (\u00E9, \u00E8, \u00EA, \u00EB, \u00E0, \u00E2, etc.).");
                sb.AppendLine("- Gender/number agreement: check that adjectives and past participles agree.");
                sb.AppendLine("- Punctuation spacing: spaces before colons, semicolons, question marks, exclamation marks.");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Builds the user prompt with numbered source-target pairs for batch post-editing.
        /// </summary>
        public static string BuildBatchUserPrompt(List<(int number, string source, string target)> segments)
        {
            var sb = new StringBuilder(segments.Count * 400);

            sb.AppendLine("**SEGMENTS TO POST-EDIT (" + segments.Count + " segments):**");
            sb.AppendLine();

            foreach (var seg in segments)
            {
                sb.AppendLine(seg.number + ". Source: " + seg.source);
                sb.AppendLine("   Target: " + seg.target);
                sb.AppendLine();
            }

            sb.AppendLine("**YOUR POST-EDITED TRANSLATIONS (numbered list):**");
            sb.AppendLine("For each segment, respond with EXACTLY one of:");
            sb.AppendLine("- N. " + NoChangeMarker + " \u2014 if the translation is acceptable");
            sb.AppendLine("- N. <corrected translation> \u2014 if you made changes");
            sb.Append("Begin now:");

            return sb.ToString();
        }

        /// <summary>
        /// Returns true if the parsed translation text indicates no change was needed.
        /// </summary>
        public static bool IsNoChange(string translationText)
        {
            if (string.IsNullOrWhiteSpace(translationText))
                return true;
            var trimmed = translationText.Trim();
            return trimmed.Equals(NoChangeMarker, System.StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[NO CHANGE]", System.StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("NO CHANGE", System.StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[NOCHANGE]", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
