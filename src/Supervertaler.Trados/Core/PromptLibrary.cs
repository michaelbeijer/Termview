using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Supervertaler.Trados.Models;
using Supervertaler.Trados.Settings;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Manages the prompt template library: loading, saving, and built-in prompt seeding.
    /// Prompts are stored as .md files (Markdown with YAML frontmatter) in the shared UserDataPath.PromptLibraryDir,
    /// which is the same folder Supervertaler Workbench uses — so prompts are automatically
    /// shared between both products.
    /// </summary>
    public class PromptLibrary
    {
        private static string PromptsDir => UserDataPath.PromptLibraryDir;

        /// <summary>
        /// Full path to the prompts folder on disk.
        /// </summary>
        public static string PromptsFolderPath => UserDataPath.PromptLibraryDir;

        private List<PromptTemplate> _cache;

        /// <summary>
        /// Returns all prompts from the library (cached; call Refresh() to reload).
        /// </summary>
        public List<PromptTemplate> GetAllPrompts()
        {
            if (_cache == null)
                Refresh();
            return _cache;
        }

        /// <summary>
        /// Reloads all prompts from the shared prompt_library folder.
        /// Both Workbench and this plugin read from the same location, so there is
        /// no longer a separate "desktop prompts" scan.
        /// </summary>
        public void Refresh()
        {
            _cache = new List<PromptTemplate>();

            if (Directory.Exists(PromptsDir))
                ScanDirectory(PromptsDir, PromptsDir, isReadOnly: false);

            // Mark prompts that match built-in definitions as IsBuiltIn,
            // even if the file on disk was created by Workbench without that flag.
            // Only match prompts that are in a Default subfolder (or whose domain
            // matches the built-in domain exactly) to avoid marking user clones.
            var builtInLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in GetBuiltInPromptDefinitions())
                builtInLookup[b.Name] = b.Category ?? "";
            foreach (var p in _cache)
            {
                string builtInCategory;
                if (builtInLookup.TryGetValue(p.Name, out builtInCategory))
                {
                    // Match if the prompt is in the expected Default domain,
                    // or if its file is inside a "Default" or legacy "Built-in" folder
                    var pCategory = p.Category ?? "";
                    if (pCategory.Equals(builtInCategory, StringComparison.OrdinalIgnoreCase) ||
                        pCategory.IndexOf("Default", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pCategory.IndexOf("Built-in", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (p.FilePath ?? "").IndexOf(Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (p.FilePath ?? "").IndexOf(Path.DirectorySeparatorChar + "Built-in" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        p.IsBuiltIn = true;
                    }
                }
            }
        }

        /// <summary>
        /// Finds a prompt by its relative path. Returns null if not found.
        /// </summary>
        public PromptTemplate GetPromptByRelativePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            foreach (var p in GetAllPrompts())
            {
                if (string.Equals(p.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Applies variable substitution to prompt content.
        /// Supports both {{UPPERCASE}} and {lowercase} placeholder formats.
        /// </summary>
        public static string ApplyVariables(string content, string sourceLang, string targetLang)
        {
            return ApplyVariables(content, sourceLang, targetLang, null, null, null);
        }

        /// <summary>
        /// Applies variable substitution to prompt content, including segment-level variables.
        /// Supports both {{UPPERCASE}} and {lowercase} placeholder formats.
        /// </summary>
        public static string ApplyVariables(string content, string sourceLang, string targetLang,
            string sourceText, string targetText, string selection)
        {
            return ApplyVariables(content, sourceLang, targetLang,
                sourceText, targetText, selection,
                null, null, null, null);
        }

        /// <summary>
        /// Applies variable substitution to prompt content, including all segment-level
        /// and project-level variables.
        /// Supports both {{UPPERCASE}} and {lowercase} placeholder formats.
        /// </summary>
        public static string ApplyVariables(string content, string sourceLang, string targetLang,
            string sourceText, string targetText, string selection,
            string projectName, string documentName, string surroundingSegments, string projectText,
            string tmMatches = null)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // {{UPPERCASE}} format (Python Supervertaler / Workbench standard)
            content = content.Replace("{{SOURCE_LANGUAGE}}", sourceLang ?? "");
            content = content.Replace("{{TARGET_LANGUAGE}}", targetLang ?? "");

            // Canonical segment variable names
            content = content.Replace("{{SOURCE_SEGMENT}}", sourceText ?? "");
            content = content.Replace("{{TARGET_SEGMENT}}", targetText ?? "");

            // Legacy aliases — kept for backward compatibility
            content = content.Replace("{{SOURCE_TEXT}}", sourceText ?? "");
            content = content.Replace("{{TARGET_TEXT}}", targetText ?? "");

            content = content.Replace("{{SELECTION}}", selection ?? "");

            // Project / document variables
            content = content.Replace("{{PROJECT_NAME}}", projectName ?? "");
            content = content.Replace("{{DOCUMENT_NAME}}", documentName ?? "");
            content = content.Replace("{{SURROUNDING_SEGMENTS}}", surroundingSegments ?? "");
            content = content.Replace("{{PROJECT}}", projectText ?? "");

            // TM matches
            content = content.Replace("{{TM_MATCHES}}", tmMatches ?? "");

            // {lowercase} format (legacy compatibility with Python domain prompts)
            content = content.Replace("{source_lang}", sourceLang ?? "");
            content = content.Replace("{target_lang}", targetLang ?? "");

            return content;
        }

        /// <summary>
        /// Formats a list of TM matches into a human-readable string for prompt injection.
        /// Only includes matches at or above the specified minimum percentage.
        /// </summary>
        public static string FormatTmMatches(List<TmMatch> matches, int minPercent = 70)
        {
            if (matches == null || matches.Count == 0)
                return "(no fuzzy matches above " + minPercent + "%)";

            var filtered = matches.Where(m => m.MatchPercentage >= minPercent).ToList();
            if (filtered.Count == 0)
                return "(no fuzzy matches above " + minPercent + "%)";

            var sb = new StringBuilder();
            foreach (var m in filtered.OrderByDescending(m => m.MatchPercentage))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine($"- {m.MatchPercentage}% match{(string.IsNullOrEmpty(m.TmName) ? "" : " (" + m.TmName + ")")}:");
                sb.AppendLine($"  Source: {m.SourceText}");
                sb.Append($"  Target: {m.TargetText}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns all prompts that should appear in the QuickLauncher right-click menu.
        /// </summary>
        public List<PromptTemplate> GetQuickLauncherPrompts()
        {
            var all = GetAllPrompts();
            var result = new List<PromptTemplate>();
            foreach (var p in all)
            {
                if (p.IsQuickLauncher && !p.HiddenFromMenu)
                    result.Add(p);
            }
            result.Sort((a, b) =>
            {
                var cmp = a.SortOrder.CompareTo(b.SortOrder);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        /// <summary>
        /// Returns QuickLauncher prompts organised into a folder tree for submenu rendering.
        /// Uses the actual file path on disk (RelativePath) to determine folder placement,
        /// so moving a file to a new folder takes effect immediately without editing YAML.
        /// Top-level prompts (directly in QuickLauncher/) go in root.Prompts;
        /// subfolder prompts go into child PromptFolderNodes.
        /// "Default" is pinned first among children. Empty folders are pruned.
        /// </summary>
        public PromptFolderNode GetQuickLauncherFolderStructure()
        {
            var prompts = GetQuickLauncherPrompts();

            var root = new PromptFolderNode { Name = "", RelativePath = "" };
            var folderMap = new Dictionary<string, PromptFolderNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in prompts)
            {
                // Use the actual file path to determine subfolder placement.
                // RelativePath examples:
                //   "QuickLauncher/Make it sound better.md" → root (file directly in QuickLauncher/)
                //   "QuickLauncher/Default/Define.md"       → "Default" subfolder
                //   "QuickLauncher/Research/Deep/foo.md"     → "Research/Deep" subfolder
                var relPath = (p.RelativePath ?? "").Replace('\\', '/');
                var sub = "";

                // Strip "QuickLauncher/" prefix from the directory portion
                var relDir = "";
                var lastSlash = relPath.LastIndexOf('/');
                if (lastSlash >= 0)
                    relDir = relPath.Substring(0, lastSlash);

                var qlPrefix = "QuickLauncher/";
                if (relDir.StartsWith(qlPrefix, StringComparison.OrdinalIgnoreCase))
                    sub = relDir.Substring(qlPrefix.Length);
                else if (relDir.Equals("QuickLauncher", StringComparison.OrdinalIgnoreCase))
                    sub = "";

                if (string.IsNullOrEmpty(sub))
                {
                    root.Prompts.Add(p);
                }
                else
                {
                    if (!folderMap.TryGetValue(sub, out var folder))
                    {
                        folder = new PromptFolderNode
                        {
                            Name = sub.Contains("/") ? sub.Substring(sub.LastIndexOf('/') + 1) : sub,
                            RelativePath = sub
                        };
                        folderMap[sub] = folder;

                        // Build parent chain for nested folders (e.g. "Research/Deep")
                        var parts = sub.Split('/');
                        if (parts.Length > 1)
                        {
                            var parentPath = string.Join("/", parts, 0, parts.Length - 1);
                            if (!folderMap.TryGetValue(parentPath, out var parent))
                            {
                                parent = new PromptFolderNode
                                {
                                    Name = parts[parts.Length - 2],
                                    RelativePath = parentPath
                                };
                                folderMap[parentPath] = parent;
                                root.Children.Add(parent);
                            }
                            parent.Children.Add(folder);
                        }
                        else
                        {
                            root.Children.Add(folder);
                        }
                    }
                    folder.Prompts.Add(p);
                }
            }

            // Sort children: pin "Default" first, then alphabetical
            root.Children.Sort((a, b) =>
            {
                bool aDefault = a.Name.Equals("Default", StringComparison.OrdinalIgnoreCase);
                bool bDefault = b.Name.Equals("Default", StringComparison.OrdinalIgnoreCase);
                if (aDefault && !bDefault) return -1;
                if (!aDefault && bDefault) return 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            // Recursively sort nested children
            foreach (var child in root.Children)
                SortFolderChildren(child);

            return root;
        }

        private static void SortFolderChildren(PromptFolderNode node)
        {
            node.Children.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            foreach (var child in node.Children)
                SortFolderChildren(child);
        }

        /// <summary>
        /// Saves a prompt template to disk as a Markdown file with YAML frontmatter.
        /// Creates directories as needed.
        /// </summary>
        public void SavePrompt(PromptTemplate prompt)
        {
            if (prompt == null || string.IsNullOrEmpty(prompt.Name))
                return;

            // Determine file path
            string filePath;
            string oldFilePath = null;
            if (!string.IsNullOrEmpty(prompt.FilePath) && !prompt.IsReadOnly)
            {
                filePath = prompt.FilePath;

                // If the prompt was renamed, update the filename to match.
                // Skip for built-in prompts — their filenames are managed by EnsureBuiltInPrompts.
                if (!prompt.IsBuiltIn)
                {
                    var currentFileName = Path.GetFileNameWithoutExtension(filePath);
                    var expectedFileName = SanitizeFileName(prompt.Name);
                    if (!string.Equals(currentFileName, expectedFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(filePath);
                        var newPath = Path.Combine(dir, expectedFileName + ".md");
                        if (!File.Exists(newPath))
                        {
                            oldFilePath = filePath;
                            filePath = newPath;
                        }
                        // If a file with the new name already exists, keep the old filename
                    }
                }
            }
            else
            {
                // New prompt: build path from domain + name
                // Category may contain forward slashes for nested folders (e.g. "QuickLauncher/Trados-specific")
                // — sanitise each path segment individually to preserve the directory structure
                string folder;
                if (string.IsNullOrEmpty(prompt.Category))
                {
                    folder = PromptsDir;
                }
                else
                {
                    var parts = prompt.Category.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    folder = PromptsDir;
                    foreach (var part in parts)
                        folder = Path.Combine(folder, SanitizeFileName(part));
                }
                Directory.CreateDirectory(folder);
                filePath = Path.Combine(folder, SanitizeFileName(prompt.Name) + ".md");
            }

            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("type: prompt");
            sb.AppendLine("name: \"" + EscapeYamlString(prompt.Name) + "\"");
            if (!string.IsNullOrEmpty(prompt.Description))
                sb.AppendLine("description: \"" + EscapeYamlString(prompt.Description) + "\"");
            if (!string.IsNullOrEmpty(prompt.Category))
                sb.AppendLine("category: \"" + EscapeYamlString(prompt.Category) + "\"");
            if (!string.IsNullOrEmpty(prompt.App) &&
                !prompt.App.Equals("both", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine("app: \"" + EscapeYamlString(prompt.App) + "\"");
            if (prompt.IsBuiltIn)
                sb.AppendLine("built_in: true");
            if (prompt.SortOrder != 100)
                sb.AppendLine("sort_order: " + prompt.SortOrder);
            if (prompt.HiddenFromMenu)
                sb.AppendLine("hidden: true");
            sb.AppendLine("---");
            sb.AppendLine();
            sb.Append(prompt.Content ?? "");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            // Delete old file after successfully writing the new one (rename scenario)
            if (oldFilePath != null && File.Exists(oldFilePath))
            {
                try { File.Delete(oldFilePath); } catch { /* ignore */ }
            }

            prompt.FilePath = filePath;
            prompt.RelativePath = GetRelativePath(filePath, PromptsDir);

            Refresh();
        }

        /// <summary>
        /// Updates just the sort_order of a prompt and re-saves it to disk.
        /// </summary>
        public void UpdateSortOrder(PromptTemplate prompt, int newOrder)
        {
            if (prompt == null || prompt.IsReadOnly || string.IsNullOrEmpty(prompt.FilePath))
                return;

            prompt.SortOrder = newOrder;
            SavePrompt(prompt);
        }

        /// <summary>
        /// Deletes a prompt file from disk.
        /// </summary>
        public void DeletePrompt(PromptTemplate prompt)
        {
            if (prompt == null || prompt.IsReadOnly || string.IsNullOrEmpty(prompt.FilePath))
                return;

            if (File.Exists(prompt.FilePath))
                File.Delete(prompt.FilePath);

            Refresh();
        }

        /// <summary>
        /// Ensures built-in prompts exist in the prompts directory.
        /// Creates any that are missing (idempotent — safe to call on every startup).
        /// Also removes domain-specific translate prompts that were shipped in v4.12.x
        /// but replaced by the single Default Translation Prompt in v4.13.0.
        /// </summary>
        public void EnsureBuiltInPrompts()
        {
            Directory.CreateDirectory(PromptsDir);

            // One-time migration: rename all .svprompt files to .md
            MigrateSvpromptToMd();

            // Clean up domain-specific translate prompts removed in v4.13.0
            CleanUpRetiredPrompts();

            foreach (var builtin in GetBuiltInPromptDefinitions())
            {
                // builtin.Category is now e.g. "QuickLauncher/Default"
                var folder = string.IsNullOrEmpty(builtin.Category)
                    ? PromptsDir
                    : Path.Combine(PromptsDir, builtin.Category.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(folder);

                var sanitisedName = SanitizeFileName(builtin.Name);

                // ─── Migration: move files from old locations to Default subfolder ───
                // Handles both the original flat layout (e.g. "QuickLauncher/Define.md")
                // and the intermediate "Built-in" subfolder (e.g. "QuickLauncher/Built-in/Define.md").
                var domainParts = builtin.Category.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (domainParts.Length >= 2 && domainParts[domainParts.Length - 1].Equals("Default", StringComparison.OrdinalIgnoreCase))
                {
                    var newMdPath = Path.Combine(folder, sanitisedName + ".md");

                    // Parent folder (e.g. "QuickLauncher") — flat layout migration
                    var parentFolder = PromptsDir;
                    for (int i = 0; i < domainParts.Length - 1; i++)
                        parentFolder = Path.Combine(parentFolder, domainParts[i]);
                    var flatPath = Path.Combine(parentFolder, sanitisedName + ".md");

                    // Old "Built-in" subfolder migration
                    var builtInFolder = Path.Combine(parentFolder, "Built-in");
                    var builtInPath = Path.Combine(builtInFolder, sanitisedName + ".md");

                    if (!File.Exists(newMdPath))
                    {
                        // Prefer Built-in folder (more recent), then flat
                        string sourcePath = null;
                        if (File.Exists(builtInPath))
                            sourcePath = builtInPath;
                        else if (File.Exists(flatPath))
                            sourcePath = flatPath;

                        if (sourcePath != null)
                        {
                            try
                            {
                                var content = File.ReadAllText(sourcePath);
                                if (content.Contains("built_in: true"))
                                    File.Move(sourcePath, newMdPath);
                            }
                            catch { /* ignore — file locked or permissions */ }
                        }
                    }
                }

                // Clean up duplicate when the sanitised filename differs from the
                // original name stripped of invalid chars (e.g. "What file is this
                // segment from?" → sanitised "from_" vs old "from" without the '?').
                // Only delete the old file if the correctly sanitised version exists
                // and the old file is still marked as built-in.
                var strippedName = builtin.Name.TrimEnd('?', '!', '.', '_');
                if (!string.Equals(sanitisedName, strippedName, StringComparison.OrdinalIgnoreCase))
                {
                    var sanitisedPath = Path.Combine(folder, sanitisedName + ".md");
                    var strippedPath = Path.Combine(folder, strippedName + ".md");
                    if (File.Exists(sanitisedPath) && File.Exists(strippedPath))
                    {
                        try
                        {
                            var oldContent = File.ReadAllText(strippedPath);
                            if (oldContent.Contains("built_in: true"))
                                File.Delete(strippedPath);
                        }
                        catch { /* ignore — file locked or permissions */ }
                    }
                }

                // Clean up old .svprompt version if it's still a built-in (not user-modified)
                var oldSvpromptPath = Path.Combine(folder, sanitisedName + ".svprompt");
                if (File.Exists(oldSvpromptPath))
                {
                    try
                    {
                        var oldContent = File.ReadAllText(oldSvpromptPath);
                        if (oldContent.Contains("built_in: true"))
                            File.Delete(oldSvpromptPath);
                    }
                    catch { /* ignore — file locked or permissions */ }
                }

                var filePath = Path.Combine(folder, sanitisedName + ".md");
                if (!File.Exists(filePath))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("---");
                    sb.AppendLine("type: " + (builtin.IsTransform ? "transform" : "prompt"));
                    sb.AppendLine("name: \"" + EscapeYamlString(builtin.Name) + "\"");
                    if (!string.IsNullOrEmpty(builtin.Description))
                        sb.AppendLine("description: \"" + EscapeYamlString(builtin.Description) + "\"");
                    if (!string.IsNullOrEmpty(builtin.Category))
                        sb.AppendLine("category: \"" + EscapeYamlString(builtin.Category) + "\"");
                    if (builtin.SortOrder != 100)
                        sb.AppendLine("sort_order: " + builtin.SortOrder);
                    sb.AppendLine("built_in: true");
                    sb.AppendLine("---");
                    if (!string.IsNullOrEmpty(builtin.Content))
                    {
                        sb.AppendLine();
                        sb.Append(builtin.Content);
                    }

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
            }

            // Invalidate cache so next GetAllPrompts() reloads
            _cache = null;
        }

        /// <summary>
        /// Removes domain-specific translate prompts that were shipped in v4.12.x.
        /// Only deletes files that still contain "built_in: true" — user-modified copies are left alone.
        /// </summary>
        private void CleanUpRetiredPrompts()
        {
            var retiredNames = new[]
            {
                "Medical Translation Specialist",
                "Legal Translation Specialist",
                "Patent Translation Specialist",
                "Financial Translation Specialist",
                "Technical Translation Specialist",
                "Marketing & Creative Specialist",
                "IT & Software Localization Specialist",
                "Professional Tone & Style",
                "Preserve Formatting & Layout"
            };

            var translateDir = Path.Combine(PromptsDir, "Translate");
            if (Directory.Exists(translateDir))
            {
                foreach (var name in retiredNames)
                {
                    var filePath = Path.Combine(translateDir, SanitizeFileName(name) + ".svprompt");
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var content = File.ReadAllText(filePath);
                            if (content.Contains("built_in: true"))
                                File.Delete(filePath);
                        }
                        catch { /* ignore */ }
                    }
                }
            }

            // Old explain prompts replaced in v4.18.22 by the "Explain selection (...)" variants
            // in the QuickLauncher/Default/Explain subfolder.
            var retiredExplainPaths = new[]
            {
                Path.Combine(PromptsDir, "QuickLauncher", "Default", "Explain (in general).md"),
                Path.Combine(PromptsDir, "QuickLauncher", "Default", "Explain (within project context).md"),
                Path.Combine(PromptsDir, "QuickLauncher", "Default", "Explain", "Explain (in general).md"),
                Path.Combine(PromptsDir, "QuickLauncher", "Default", "Explain", "Explain (within project context).md"),
            };
            foreach (var path in retiredExplainPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var content = File.ReadAllText(path);
                        if (content.Contains("built_in: true"))
                            File.Delete(path);
                    }
                    catch { /* ignore */ }
                }
            }
        }

        /// <summary>
        /// One-time migration: renames all .svprompt files to .md in the prompt library.
        /// Skips files where a .md version already exists (to avoid overwriting).
        /// </summary>
        private void MigrateSvpromptToMd()
        {
            try
            {
                if (!Directory.Exists(PromptsDir)) return;

                foreach (var svpromptFile in Directory.GetFiles(PromptsDir, "*.svprompt", SearchOption.AllDirectories))
                {
                    try
                    {
                        var mdFile = Path.ChangeExtension(svpromptFile, ".md");
                        if (!File.Exists(mdFile))
                            File.Move(svpromptFile, mdFile);
                        else
                            File.Delete(svpromptFile); // .md version already exists
                    }
                    catch { /* ignore — file locked or permissions */ }
                }
            }
            catch { /* ignore — directory access failure */ }
        }

        /// <summary>
        /// Restores all built-in prompts (overwrites any user edits).
        /// </summary>
        public void RestoreBuiltInPrompts()
        {
            foreach (var builtin in GetBuiltInPromptDefinitions())
            {
                var folder = string.IsNullOrEmpty(builtin.Category)
                    ? PromptsDir
                    : Path.Combine(PromptsDir, builtin.Category.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(folder);

                var sanitisedName = SanitizeFileName(builtin.Name);

                // Clean up old .svprompt version
                var oldSvpromptPath = Path.Combine(folder, sanitisedName + ".svprompt");
                if (File.Exists(oldSvpromptPath))
                {
                    try { File.Delete(oldSvpromptPath); } catch { }
                }

                var filePath = Path.Combine(folder, sanitisedName + ".md");

                var sb = new StringBuilder();
                sb.AppendLine("---");
                sb.AppendLine("type: prompt");
                sb.AppendLine("name: \"" + EscapeYamlString(builtin.Name) + "\"");
                if (!string.IsNullOrEmpty(builtin.Description))
                    sb.AppendLine("description: \"" + EscapeYamlString(builtin.Description) + "\"");
                if (!string.IsNullOrEmpty(builtin.Category))
                    sb.AppendLine("category: \"" + EscapeYamlString(builtin.Category) + "\"");
                sb.AppendLine("built_in: true");
                sb.AppendLine("---");
                sb.AppendLine();
                sb.Append(builtin.Content);

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }

            _cache = null;
        }

        // ─── Private Methods ─────────────────────────────────────────

        private void ScanDirectory(string dir, string rootDir, bool isReadOnly)
        {
            try
            {
                var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Scan .md files first (preferred format)
                foreach (var file in Directory.GetFiles(dir, "*.md", SearchOption.AllDirectories))
                {
                    try
                    {
                        var prompt = ParsePromptFile(file, rootDir);
                        if (prompt != null && !IsWorkbenchOnly(prompt))
                        {
                            prompt.IsReadOnly = isReadOnly;
                            _cache.Add(prompt);
                            seenNames.Add(prompt.Name);
                        }
                    }
                    catch
                    {
                        // Skip files that can't be parsed
                    }
                }

                // Also scan .svprompt files (legacy format) — skip if .md version exists
                foreach (var file in Directory.GetFiles(dir, "*.svprompt", SearchOption.AllDirectories))
                {
                    try
                    {
                        var prompt = ParsePromptFile(file, rootDir);
                        if (prompt != null && !seenNames.Contains(prompt.Name) && !IsWorkbenchOnly(prompt))
                        {
                            prompt.IsReadOnly = isReadOnly;
                            _cache.Add(prompt);
                        }
                    }
                    catch
                    {
                        // Skip files that can't be parsed
                    }
                }
            }
            catch
            {
                // Skip directories that can't be accessed
            }
        }

        /// <summary>
        /// Parses a Markdown file with optional YAML frontmatter.
        /// YAML is parsed as simple key: "value" pairs (no external library needed).
        /// </summary>
        private PromptTemplate ParsePromptFile(string filePath, string rootDir)
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Normalize line endings to CRLF for Windows TextBox controls
            text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            var prompt = new PromptTemplate
            {
                FilePath = filePath,
                RelativePath = GetRelativePath(filePath, rootDir)
            };

            // Parse YAML frontmatter (between --- delimiters)
            if (text.TrimStart().StartsWith("---"))
            {
                var idx1 = text.IndexOf("---", StringComparison.Ordinal);
                var idx2 = text.IndexOf("---", idx1 + 3, StringComparison.Ordinal);

                if (idx2 > idx1)
                {
                    var yaml = text.Substring(idx1 + 3, idx2 - idx1 - 3);
                    ParseYamlFrontmatter(prompt, yaml);

                    // Content is everything after the second ---
                    var contentStart = idx2 + 3;
                    prompt.Content = text.Substring(contentStart).TrimStart('\r', '\n');
                }
                else
                {
                    // Malformed frontmatter — treat entire file as content
                    prompt.Content = text;
                }
            }
            else
            {
                // No frontmatter — entire file is content
                prompt.Content = text;
            }

            // Fallback: use filename if no name in frontmatter
            if (string.IsNullOrEmpty(prompt.Name))
                prompt.Name = Path.GetFileNameWithoutExtension(filePath);

            // Fallback: use folder name as domain if not specified in YAML
            if (string.IsNullOrEmpty(prompt.Category))
            {
                var relDir = Path.GetDirectoryName(prompt.RelativePath);
                if (!string.IsNullOrEmpty(relDir))
                    prompt.Category = relDir;
            }

            // Normalise domain regardless of whether it came from YAML or folder name
            NormaliseCategory(prompt);

            // For text transforms, parse find/replace rules from the content body.
            // Format: find: "..." / replace: "..." pairs, separated by blank lines.
            // Lines starting with # are comments. Supports \uXXXX escape sequences.
            if (prompt.IsTransform && !string.IsNullOrEmpty(prompt.Content))
                ParseTransformContent(prompt);

            return prompt;
        }

        /// <summary>
        /// Parses find/replace rules from a text transform's content body.
        /// Each rule is a find:/replace: pair. Blank lines and # comments are ignored.
        /// Supports \uXXXX escape sequences and quoted values.
        /// </summary>
        private void ParseTransformContent(PromptTemplate prompt)
        {
            prompt.Replacements.Clear();
            TextReplacement current = null;

            var lines = prompt.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("find:", StringComparison.OrdinalIgnoreCase))
                {
                    current = new TextReplacement
                    {
                        Find = UnescapeYamlString(trimmed.Substring(5).Trim())
                    };
                    prompt.Replacements.Add(current);
                }
                else if (trimmed.StartsWith("replace:", StringComparison.OrdinalIgnoreCase) && current != null)
                {
                    current.Replace = UnescapeYamlString(trimmed.Substring(8).Trim());
                }
            }
        }

        /// <summary>
        /// Returns true if the prompt is targeted exclusively at Supervertaler Workbench
        /// and should not appear in the Trados plugin.
        /// </summary>
        private static bool IsWorkbenchOnly(PromptTemplate prompt)
        {
            return string.Equals(prompt.App, "workbench", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies canonical normalisation to prompt.Category and sets IsQuickLauncher.
        /// Called after both YAML parsing and the folder-name fallback so the logic
        /// is consistent regardless of how the domain was determined.
        /// </summary>
        private static void NormaliseCategory(PromptTemplate prompt)
        {
            if (string.IsNullOrEmpty(prompt.Category)) return;

            // Normalise legacy names → canonical "QuickLauncher"
            if (prompt.Category.Equals("quickmenu_prompts", StringComparison.OrdinalIgnoreCase) ||
                prompt.Category.Equals("quicklauncher_prompts", StringComparison.OrdinalIgnoreCase))
            {
                prompt.Category = "QuickLauncher";
            }

            // Mark as QuickLauncher if the domain is "QuickLauncher" or starts with "QuickLauncher/"
            // (e.g. "QuickLauncher/Default")
            if (prompt.Category.Equals("QuickLauncher", StringComparison.OrdinalIgnoreCase) ||
                prompt.Category.StartsWith("QuickLauncher/", StringComparison.OrdinalIgnoreCase) ||
                prompt.Category.StartsWith("QuickLauncher\\", StringComparison.OrdinalIgnoreCase))
                prompt.IsQuickLauncher = true;
        }

        private void ParseYamlFrontmatter(PromptTemplate prompt, string yaml)
        {
            var lines = yaml.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var colonIdx = trimmed.IndexOf(':');
                if (colonIdx <= 0)
                    continue;

                var key = trimmed.Substring(0, colonIdx).Trim().ToLowerInvariant();
                var value = trimmed.Substring(colonIdx + 1).Trim();

                // Strip quotes
                if (value.Length >= 2 &&
                    ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                     (value.StartsWith("'") && value.EndsWith("'"))))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                switch (key)
                {
                    case "type":
                        prompt.Type = value;
                        break;
                    case "name":
                        prompt.Name = value;
                        break;
                    case "description":
                        prompt.Description = value;
                        break;
                    case "category":
                    case "domain": // backward compatibility
                        prompt.Category = value;
                        // Full normalisation (legacy names → QuickLauncher, IsQuickLauncher flag)
                        // runs after all YAML is parsed, in NormaliseCategory().
                        break;
                    case "app":
                        // Unified schema: "workbench", "trados", or "both"
                        prompt.App = value.ToLowerInvariant();
                        break;
                    case "built_in":
                        prompt.IsBuiltIn = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "quickmenu":       // unified schema
                    case "sv_quickmenu":    // backward compatibility (Workbench legacy)
                    case "quick_run":       // backward compatibility (Workbench legacy)
                        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                            prompt.IsQuickLauncher = true;
                        break;
                    case "quicklauncher_label":
                    case "quickmenu_label": // backward compatibility
                        prompt.QuickLauncherLabel = value;
                        break;
                    case "sort_order":
                        int order;
                        if (int.TryParse(value, out order))
                            prompt.SortOrder = order;
                        break;
                    case "hidden":
                        prompt.HiddenFromMenu = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }
        }

        /// <summary>
        /// Unescapes common YAML string escape sequences (\uXXXX, \n, \t)
        /// and strips surrounding quotes.
        /// </summary>
        private static string UnescapeYamlString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            // Strip surrounding quotes
            if (s.Length >= 2 &&
                ((s.StartsWith("\"") && s.EndsWith("\"")) ||
                 (s.StartsWith("'") && s.EndsWith("'"))))
            {
                s = s.Substring(1, s.Length - 2);
            }

            // Unescape \uXXXX sequences
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    var next = s[i + 1];
                    if (next == 'u' && i + 5 < s.Length)
                    {
                        int code;
                        if (int.TryParse(s.Substring(i + 2, 4),
                            System.Globalization.NumberStyles.HexNumber, null, out code))
                        {
                            sb.Append((char)code);
                            i += 5;
                            continue;
                        }
                    }
                    else if (next == 'n') { sb.Append('\n'); i++; continue; }
                    else if (next == 't') { sb.Append('\t'); i++; continue; }
                    else if (next == '\\') { sb.Append('\\'); i++; continue; }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static string GetRelativePath(string fullPath, string rootDir)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(rootDir))
                return fullPath;

            var root = rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(root.Length);

            return fullPath;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static string EscapeYamlString(string s)
        {
            return EscapeYaml(s);
        }

        /// <summary>
        /// Escapes a string for safe use inside double-quoted YAML values.
        /// </summary>
        public static string EscapeYaml(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Escapes a string for YAML, converting non-ASCII characters to \uXXXX sequences.
        /// Used for replacement find/replace values that may contain invisible Unicode characters.
        /// </summary>
        private static string EscapeUnicodeForYaml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length * 2);
            foreach (char c in s)
            {
                if (c > 127)
                    sb.AppendFormat("\\u{0:X4}", (int)c);
                else if (c == '\\')
                    sb.Append("\\\\");
                else if (c == '"')
                    sb.Append("\\\"");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // ─── Built-in Prompt Definitions ──────────────────────────────

        private List<PromptTemplate> GetBuiltInPromptDefinitions()
        {
            return new List<PromptTemplate>
            {
                // ─── Default Translation Prompt ──────────────────────
                new PromptTemplate
                {
                    Name = "Default Translation Prompt",
                    Description = "General-purpose translation prompt — use as-is or as a starting point for your own prompts",
                    Category = "Translate/Default",
                    IsBuiltIn = true,
                    Content = @"You are a professional translator working from {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}}. Translate the source text accurately and naturally, following these guidelines:

## Core principles
- Produce a fluent, idiomatic translation that reads as if originally written in {{TARGET_LANGUAGE}}
- Preserve the meaning, tone, and register of the source text
- Maintain consistency in terminology throughout the document
- Keep numbers, dates, measurements, and proper nouns accurate
- Preserve all formatting, tags, and placeholders exactly as they appear

## Terminology
- Use the glossary terms provided (if any) — they take priority over alternative translations
- When a term has no established equivalent, keep the source term and add a brief explanation in parentheses if needed

## Style
- Match the formality level of the source (formal documents stay formal, casual content stays casual)
- Use natural sentence structures in the target language rather than mirroring source syntax
- Avoid unnecessary additions, omissions, or explanatory notes unless explicitly requested"
                },

                // ─── Proofreading ─────────────────────────────────────────
                new PromptTemplate
                {
                    Name = "Default Proofreading Prompt",
                    Description = "Reviews translations for accuracy, completeness, terminology, grammar, and style issues",
                    Category = "Proofread/Default",
                    IsBuiltIn = true,
                    Content = @"You are a professional translation proofreader. Your task is to review {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}} translation pairs and identify issues. You must check EVERY segment provided — do not skip any.

For each segment, check the following:

## 1. Accuracy
- Does the translation faithfully convey the meaning of the source?
- Are there any mistranslations, shifts in meaning, or misinterpretations?
- Are ambiguous source phrases resolved appropriately for the target language?

## 2. Completeness
- Is any source content omitted in the translation?
- Is any content added that is not present in the source?
- Are all numbers, dates, and references carried over correctly?

## 3. Terminology Consistency
- Are key terms translated consistently across segments?
- Are domain-specific terms translated correctly?
- Are proper nouns, brand names, and product names handled appropriately?

## 4. Grammar & Style
- Is the translation grammatically correct in {{TARGET_LANGUAGE}}?
- Is the style appropriate for the text type and register?
- Is the sentence structure natural and fluent in the target language?

## 5. Number & Unit Formatting
- Are numbers formatted according to {{TARGET_LANGUAGE}} conventions (decimal separators, thousand separators)?
- Are units of measurement correct and properly formatted?
- Are currency symbols and codes appropriate for the target locale?

## Language-Specific Checks

### Dutch
- Compound words: verify correct spelling (e.g., 'ziekenhuisopname' not 'ziekenhuis opname')
- dt-errors: check verb conjugation (word/wordt, vind/vindt, etc.)
- de/het articles: verify correct article usage with nouns
- Spelling: follow current Woordenlijst Nederlandse Taal (het Groene Boekje)

### German
- Compound nouns: verify correct formation (Zusammenschreibung)
- Capitalization: all nouns must be capitalized
- Case system: check correct use of Nominativ, Akkusativ, Dativ, Genitiv
- Verb position: verify correct verb placement in main and subordinate clauses

### French
- Accents: verify all accents are correct (é, è, ê, ë, à, ç, etc.)
- Gender/number agreement: check adjective-noun and subject-verb agreement
- Punctuation spacing: non-breaking space before ; : ! ? and inside « »
- Elision and liaison rules

## Output Format

You MUST use this exact format for every segment. Check ALL segments — do not skip any.

For segments with no issues:
[SEGMENT XXXX] OK

For segments with issues:
[SEGMENT XXXX] ISSUE
Issue: <brief description of the problem>
Suggestion: <describe what should be changed — do NOT provide a full corrected translation>

IMPORTANT RULES:
- NEVER provide corrected full translations. Only describe the issue and suggest what specifically should be fixed.
- Use the segment number as it appears in the input (e.g., [SEGMENT 0042]).
- Report each distinct issue on its own ISSUE block if a segment has multiple problems.
- You MUST review ALL segments. Do not stop early or summarize remaining segments as 'OK'."
                },
                new PromptTemplate
                {
                    Name = "UK to US English Localization",
                    Description = "Flags British English spelling, vocabulary, and conventions that need changing to American English",
                    Category = "Proofread/Default",
                    IsBuiltIn = true,
                    Content = @"You are a professional English localizer specializing in adapting British English (BrE) text to American English (AmE). Your task is to review each segment and flag any British English forms that should be changed to their American English equivalents.

IMPORTANT: This is a linguistic localization check only. Do NOT flag style, tone, sentence structure, readability, or rewriting suggestions. Only flag words, spellings, and conventions that differ between British and American English.

## 1. Spelling Differences

### -ise/-ize and -isation/-ization
British -ise spellings must be changed to American -ize:
- localise → localize, organise → organize, recognise → recognize, specialise → specialize, optimise → optimize, customise → customize, minimise → minimize, maximise → maximize, utilise → utilize, standardise → standardize, synchronise → synchronize, authorise → authorize, characterise → characterize, initialise → initialize, finalise → finalize, normalise → normalize, prioritise → prioritize, summarise → summarize, categorise → categorize, analyse → analyze, paralyse → paralyze, catalyse → catalyze
- localisation → localization, organisation → organization, specialisation → specialization, optimisation → optimization, synchronisation → synchronization, authorisation → authorization, initialisation → initialization, normalisation → normalization, serialisation → serialization, visualisation → visualization

### -our/-or
- colour → color, favour → favor, behaviour → behavior, honour → honor, labour → labor, neighbour → neighbor, humour → humor, vapour → vapor, flavour → flavor, harbour → harbor, rumour → rumor, vigour → vigor, savour → savor, armour → armor, glamour → glamor (though ""glamour"" is sometimes kept in AmE)
- Also their derivatives: coloured → colored, favourite → favorite, flavoured → flavored, honourable → honorable, neighbouring → neighboring, behavioural → behavioral, favourable → favorable, unfavourable → unfavorable, colourful → colorful, humorous stays ""humorous"" in both

### -re/-er
- centre → center, metre → meter, litre → liter, fibre → fiber, theatre → theater, calibre → caliber, lustre → luster, manoeuvre → maneuver, spectre → specter, sombre → somber, meagre → meager
- Also: centred → centered, centring → centering, metreing → metering

### -ence/-ense
- defence → defense, offence → offense, licence (noun) → license, pretence → pretense

### -lled/-led, -lling/-ling, -ller/-ler
British English doubles the L before suffixes; American English does not:
- travelling → traveling, traveller → traveler, cancelled → canceled, cancelling → canceling, modelling → modeling, modeller → modeler, labelling → labeling, labelled → labeled, levelled → leveled, levelling → leveling, fuelling → fueling, fuelled → fueled, dialled → dialed, dialling → dialing, marshalled → marshaled, marshalling → marshaling, channelled → channeled, channelling → channeling, signalling → signaling, signalled → signaled, panelling → paneling, panelled → paneled, jewellery → jewelry

### -ogue/-og
- analogue → analog, catalogue → catalog, dialogue → dialog, prologue → prolog, monologue → monolog, epilogue → epilog
- Note: in technical contexts, ""analog"" and ""dialog"" are strongly preferred in AmE

### -ae-/-oe- to -e-
- anaemia → anemia, anaesthesia → anesthesia, oestrogen → estrogen, paediatric → pediatric, encyclopaedia → encyclopedia, orthopaedic → orthopedic, haemorrhage → hemorrhage, haemoglobin → hemoglobin, foetus → fetus, diarrhoea → diarrhea, oesophagus → esophagus, gynaecology → gynecology, leukaemia → leukemia, manoeuvre → maneuver

### -t/-ed past tense
- learnt → learned, burnt → burned, spelt → spelled, dreamt → dreamed, leapt → leaped, spoilt → spoiled, knelt → kneeled (though ""knelt"" is acceptable in AmE too)

### Other spelling differences
- grey → gray, tyre → tire, kerb → curb, draught → draft, plough → plow, cheque → check (financial), gaol → jail, aeroplane → airplane, aluminium → aluminum, artefact → artifact, cosy → cozy, doughnut stays ""doughnut"" (both acceptable in AmE), fulfil → fulfill, enrol → enroll, enthral → enthrall, instal → install, skilful → skillful, wilful → willful, distil → distill, instalment → installment, fulfilment → fulfillment, enrolment → enrollment, judgement → judgment (in legal/technical contexts), ageing → aging, likeable → likable, sizeable → sizable, moveable → movable, saleable → salable, acknowledgement → acknowledgment, programme → program, whilst → while, amongst → among, amidst → amid, towards → toward, forwards → forward, backwards → backward, afterwards → afterward, upwards → upward, outwards → outward

## 2. Vocabulary Differences

Flag any British vocabulary that has a standard American equivalent:
- lorry → truck, boot (of car) → trunk, bonnet (of car) → hood, windscreen → windshield, petrol → gas/gasoline, motorway → highway/freeway, dual carriageway → divided highway, car park → parking lot/parking garage, pavement → sidewalk, zebra crossing → crosswalk, roundabout → traffic circle/rotary, flyover → overpass, estate car → station wagon, gear lever → gearshift, number plate → license plate
- lift → elevator, flat → apartment, ground floor → first floor, first floor → second floor, garden → yard (residential outdoor area), bin → trash can/garbage can, rubbish → trash/garbage, skip → dumpster, tap → faucet, torch → flashlight, nappy → diaper, dummy (baby) → pacifier, pram → stroller/baby carriage, queue → line, post (mail) → mail, postbox → mailbox, postcode → zip code, mobile (phone) → cell phone, cooker → stove/range, hob → stovetop/burner, grill → broiler (when top-heat cooking), crisps → chips, chips → fries/French fries, biscuit → cookie (sweet) or cracker (savory), tin → can, courgette → zucchini, aubergine → eggplant, rocket (salad) → arugula, coriander → cilantro (leaves), spring onion → scallion/green onion, swede → rutabaga, mince → ground meat, candyfloss → cotton candy
- holiday → vacation (but ""holiday"" for public holidays is fine in AmE), autumn → fall (though ""autumn"" is understood in AmE), fortnight → two weeks, anti-clockwise → counterclockwise, full stop → period (punctuation), inverted commas → quotation marks, maths → math, sport → sports (when general), at the weekend → on the weekend, ring (phone) → call, have a go → try/give it a try, sorted → taken care of/resolved

### Technical/IT vocabulary
- mobile → cell/mobile (both acceptable in US tech), spanner → wrench, earth (electrical) → ground, earthing → grounding, mains (electrical) → power supply/AC power, flex (electrical cable) → cord, socket → outlet, adaptor → adapter

## 3. Punctuation and Formatting Conventions

- Quotation marks: single quotes ('…') are BrE convention; AmE uses double quotes (""…"") as primary. Flag if the text consistently uses BrE single-quote convention.
- Dates: flag dd/mm/yyyy format if clearly British ordering (e.g., ""05/03/2025"" meaning 5 March). Note: only flag if context makes the British ordering clear.
- Mr, Mrs, Dr without periods → Mr., Mrs., Dr. with periods in AmE.

## 4. What NOT to Flag

- Do NOT flag style, sentence structure, or phrasing preferences
- Do NOT suggest rewriting for ""more natural"" American phrasing
- Do NOT flag words that are the same in both variants (e.g., ""transport,"" ""government"")
- Do NOT flag proper nouns, brand names, place names, or quoted text
- Do NOT flag ""humorous"" (same spelling in both)
- Do NOT flag technical terms that are conventionally spelled the same way in both variants
- Do NOT provide corrected full translations — only describe what should be changed

## Output Format

You MUST use this exact format for every segment. Check ALL segments — do not skip any.

For segments with no British English forms:
[SEGMENT XXXX] OK

For segments with British English forms to localize:
[SEGMENT XXXX] ISSUE
Issue: <identify the British English word/spelling>
Suggestion: <state the American English equivalent>

IMPORTANT RULES:
- NEVER provide corrected full translations. Only identify the specific British English word(s) and their American English replacement(s).
- Use the segment number as it appears in the input (e.g., [SEGMENT 0042]).
- Report each distinct issue on its own ISSUE block if a segment has multiple British English forms.
- You MUST review ALL segments. Do not stop early or summarize remaining segments as ""OK"".
- If a segment contains multiple British English words, list each as a separate ISSUE block under the same segment header."
                },

                // ─── QuickLauncher ─────────────────────────────────────
                new PromptTemplate
                {
                    Name = "Assess how I translated the current segment",
                    Description = "Reviews your translation of the active segment and suggests improvements",
                    Category = "QuickLauncher/Default",
                    IsBuiltIn = true,
                    Content = @"Source ({{SOURCE_LANGUAGE}}):
{{SOURCE_TEXT}}

My translation ({{TARGET_LANGUAGE}}):
{{TARGET_TEXT}}

Assess how I translated the current segment. Point out any inaccuracies, awkward phrasing, or terminology issues, and suggest improvements."
                },
                new PromptTemplate
                {
                    Name = "Define",
                    Description = "Defines the selected term and provides usage examples",
                    Category = "QuickLauncher/Default",
                    IsBuiltIn = true,
                    Content = @"Define ""{{SELECTION}}"" and give practical examples showing how it's used."
                },
                new PromptTemplate
                {
                    Name = "Explain selection (in general)",
                    Category = "QuickLauncher/Default/Explain",
                    IsBuiltIn = true,
                    Content = @"Explain ""{{SELECTION}}"" in simple, clear language. Include a practical example if helpful."
                },
                new PromptTemplate
                {
                    Name = "Explain selection (within context of surrounding segments)",
                    Description = "This is a much lighter version than the original, which sends the entire document source text.",
                    Category = "QuickLauncher/Default/Explain",
                    IsBuiltIn = true,
                    Content = @"PROJECT CONTEXT - The surrounding segments from the current translation project:

{{SURROUNDING_SEGMENTS}}

---

Explain ""{{SELECTION}}"" in simple, clear language. If the project context above provides relevant information about how it is used in this specific document, reference those segments in your explanation."
                },
                new PromptTemplate
                {
                    Name = "Explain selection (within full project context)",
                    Description = "Explains the selection using the full document as context",
                    Category = "QuickLauncher/Default/Explain",
                    IsBuiltIn = true,
                    Content = @"PROJECT CONTEXT - The complete source text from the current translation project:

{{PROJECT}}

---

Explain ""{{SELECTION}}"" in simple, clear language. If the project context above provides relevant information about how it is used in this specific document, reference those segments in your explanation."
                },
                new PromptTemplate
                {
                    Name = "Show current filename",
                    Description = "Displays the filename of the file you are currently translating",
                    Category = "QuickLauncher/Default/Files",
                    IsBuiltIn = true,
                    Content = @"Simply reply with the filename below and nothing else:

{{DOCUMENT_NAME}}"
                },
                new PromptTemplate
                {
                    Name = "What file is this segment from?",
                    Description = "Shows the filename and project context for the current segment",
                    Category = "QuickLauncher/Default/Files",
                    IsBuiltIn = true,
                    Content = @"The current segment is from the file ""{{DOCUMENT_NAME}}"" in the project ""{{PROJECT_NAME}}"".

Source ({{SOURCE_LANGUAGE}}):
{{SOURCE_TEXT}}

What type of file is this, and what can you tell me about it based on the filename and segment content?"
                },
                new PromptTemplate
                {
                    Name = "Translate segment using fuzzy matches as reference",
                    Description = "Translates the active segment, using TM fuzzy matches and surrounding context",
                    Category = "QuickLauncher/Default",
                    IsBuiltIn = true,
                    Content = @"Translate the following from {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}}.

Source: {{SOURCE_SEGMENT}}

Surrounding context:
{{SURROUNDING_SEGMENTS}}

TM fuzzy matches:
{{TM_MATCHES}}

Use the fuzzy matches and surrounding context as reference, but produce a fresh, accurate translation of the source segment."
                },
                new PromptTemplate
                {
                    Name = "Translate selection in context of current project",
                    Description = "Suggests the best translation for a selected term using full document context",
                    Category = "QuickLauncher/Default",
                    IsBuiltIn = true,
                    Content = @"PROJECT CONTEXT - The complete source text of the current translation project:

{{PROJECT}}

---

Using the project context above, suggest the best translation for ""{{SELECTION}}"" from {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}}. Reference relevant segments in your explanation."
                },
                new PromptTemplate
                {
                    Name = "Generate project brief",
                    Description = "Generates a comprehensive project summary in Markdown that you can paste into any AI tool for context while translating",
                    Category = "QuickLauncher/Default",
                    IsBuiltIn = true,
                    Content = @"You are a senior translation project analyst. Your job is to produce a comprehensive briefing document that a professional translator (or an AI assistant helping a translator) can use as reference material throughout an entire translation project.

The briefing will be used as context when pasting into AI chat interfaces (Claude, ChatGPT, Gemini, etc.) to ask questions while translating.

PROJECT METADATA:
- Project name: {{PROJECT_NAME}}
- Document: {{DOCUMENT_NAME}}
- Source language: {{SOURCE_LANGUAGE}}
- Target language: {{TARGET_LANGUAGE}}

FULL SOURCE TEXT:
{{PROJECT}}

---

Analyse the complete source text above and produce a **Project Brief** in clean Markdown with the following sections. Be thorough but concise – the briefing should be a practical reference, not a literary essay.

## 1. Document Overview
- What type of document is this? (legal, technical, marketing, medical, patent, financial, academic, etc.)
- What is the document about? (2–4 sentences summarising the subject matter)
- Who is the likely audience?
- What is the overall tone and register? (formal, informal, technical, persuasive, neutral, etc.)

## 2. Key Concepts and Subject Matter
- List the main topics and concepts discussed in the document
- Briefly explain any domain-specific concepts that a general translator might not immediately understand

## 3. Terminology
- List all important technical terms, abbreviations, and acronyms found in the source text
- Format as a table: | Term | Explanation | Notes for translator |
- Include any terms that appear frequently or that are critical to translate consistently
- Flag any terms that are ambiguous or could be translated multiple ways

## 4. Named Entities
- List all proper nouns: people, organisations, products, brands, places, legislation, standards
- Note which ones should likely remain untranslated and which may need localisation

## 5. Structure and Patterns
- Describe the document structure (sections, headings, lists, numbering patterns)
- Note any recurring sentence patterns, boilerplate text, or formulaic language
- Flag any segments that appear to be duplicated or near-duplicated

## 6. Translation Challenges
- Identify specific passages that are likely to be difficult to translate
- Note any culturally specific references, wordplay, or idiomatic expressions
- Flag any ambiguous passages where the meaning is unclear even in the source language
- Note any inconsistencies in the source text (terminology, style, numbering)

## 7. Style and Consistency Notes
- Note the source text's style characteristics that should be preserved or adapted
- Any patterns in capitalisation, punctuation, or formatting conventions
- Recommendations for maintaining consistency throughout the translation

Format the entire output as a single Markdown document that can be copied and pasted directly into another AI tool's chat interface."
                },

                // ─── Text operations (transforms — no AI call) ───────────
                new PromptTemplate
                {
                    Name = "Strip U+2028",
                    Description = "Removes invisible Unicode LINE SEPARATOR (U+2028) and PARAGRAPH SEPARATOR (U+2029) characters from the target segment, replacing them with spaces",
                    Category = "QuickLauncher/Text operations",
                    Type = "transform",
                    IsBuiltIn = true,
                    SortOrder = 10,
                    Content = @"# Strip invisible Unicode line/paragraph separators.
# These are commonly inserted by InDesign (IDML) as forced line breaks
# (Shift+Enter). They are invisible in Trados but can corrupt translations.
#
# Each rule is a find:/replace: pair. Use \uXXXX for Unicode characters.
# Lines starting with # are comments.

find: ""\u2028""
replace: "" ""

find: ""\u2029""
replace: "" """
                }
            };
        }

        // ─── Folder-aware tree structure ──────────────────────────────

        /// <summary>
        /// Builds a tree of folders and prompts mirroring the on-disk structure.
        /// Includes empty folders. Root-level prompts go into a list on the returned node.
        /// </summary>
        public PromptFolderNode GetFolderStructure()
        {
            if (_cache == null)
                Refresh();

            var root = new PromptFolderNode
            {
                Name = "",
                RelativePath = ""
            };

            if (!Directory.Exists(PromptsDir))
                return root;

            // Build folder tree from disk (includes empty folders)
            BuildFolderTree(root, PromptsDir, PromptsDir);

            // Place each cached prompt into the right folder node
            foreach (var prompt in _cache)
            {
                var folderPath = Path.GetDirectoryName(prompt.RelativePath) ?? "";
                folderPath = folderPath.Replace('\\', '/');
                var folder = FindOrCreateFolder(root, folderPath);
                folder.Prompts.Add(prompt);
            }

            // Sort folders and prompts alphabetically
            SortFolderTree(root);

            return root;
        }

        private void BuildFolderTree(PromptFolderNode parent, string currentDir, string rootDir)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || dirName == "__pycache__")
                        continue;

                    var relativePath = subDir.Length > rootDir.Length
                        ? subDir.Substring(rootDir.Length + 1).Replace('\\', '/')
                        : dirName;

                    var child = new PromptFolderNode
                    {
                        Name = dirName,
                        RelativePath = relativePath
                    };
                    parent.Children.Add(child);
                    BuildFolderTree(child, subDir, rootDir);
                }
            }
            catch { /* Ignore permission errors */ }
        }

        private PromptFolderNode FindOrCreateFolder(PromptFolderNode root, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return root;

            var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            foreach (var part in parts)
            {
                PromptFolderNode found = null;
                foreach (var child in current.Children)
                {
                    if (string.Equals(child.Name, part, StringComparison.OrdinalIgnoreCase))
                    {
                        found = child;
                        break;
                    }
                }
                if (found == null)
                {
                    found = new PromptFolderNode
                    {
                        Name = part,
                        RelativePath = current.RelativePath.Length > 0
                            ? current.RelativePath + "/" + part
                            : part
                    };
                    current.Children.Add(found);
                }
                current = found;
            }
            return current;
        }

        private void SortFolderTree(PromptFolderNode node)
        {
            node.Children.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            node.Prompts.Sort((a, b) =>
            {
                var cmp = a.SortOrder.CompareTo(b.SortOrder);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            foreach (var child in node.Children)
                SortFolderTree(child);
        }

        /// <summary>
        /// Creates a new subfolder in the prompt library.
        /// </summary>
        public void CreateFolder(string relativePath)
        {
            var fullPath = Path.Combine(PromptsDir, relativePath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        /// <summary>
        /// Deletes a folder and all its contents from the prompt library.
        /// </summary>
        public void DeleteFolder(string relativePath)
        {
            var fullPath = Path.Combine(PromptsDir, relativePath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
                Refresh();
            }
        }

        /// <summary>
        /// Moves a prompt file to a different folder within the prompt library.
        /// </summary>
        public void MovePrompt(PromptTemplate prompt, string newFolderRelative)
        {
            if (prompt == null || prompt.IsReadOnly) return;

            var fileName = Path.GetFileName(prompt.FilePath);
            var newDir = string.IsNullOrEmpty(newFolderRelative)
                ? PromptsDir
                : Path.Combine(PromptsDir, newFolderRelative);

            if (!Directory.Exists(newDir))
                Directory.CreateDirectory(newDir);

            var newPath = Path.Combine(newDir, fileName);
            if (File.Exists(newPath)) return; // Don't overwrite

            File.Move(prompt.FilePath, newPath);
            Refresh();
        }
    }
}
