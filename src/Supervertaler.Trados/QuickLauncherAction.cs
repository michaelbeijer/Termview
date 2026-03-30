using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;
using Supervertaler.Trados.Core;
using Supervertaler.Trados.Licensing;
using Supervertaler.Trados.Models;
using Supervertaler.Trados.Settings;

namespace Supervertaler.Trados
{
    /// <summary>
    /// Editor context menu action: "QuickLauncher".
    /// Appears as a single entry in the right-click context menu.
    /// When clicked, shows a submenu listing all prompts marked as QuickLauncher
    /// (sv_quickmenu: true or category: QuickLauncher in YAML frontmatter).
    /// Selecting a prompt expands its variables from the current segment context
    /// and submits it to the AI Assistant chat.
    /// </summary>
    [Action("Supervertaler_QuickLauncher", typeof(EditorController),
        Name = "QuickLauncher",
        Description = "Run a QuickLauncher prompt on the current segment using the AI Assistant")]
    [ActionLayout(
        typeof(TranslationStudioDefaultContextMenus.EditorDocumentContextMenuLocation), 9,
        DisplayType.Default, "", true)]
    [Shortcut(Keys.Control | Keys.Q)]
    public class QuickLauncherAction : AbstractAction
    {
        private static readonly PromptLibrary _library = new PromptLibrary();

        protected override void Execute()
        {
            if (!LicenseManager.Instance.HasTier2Access)
            {
                LicenseManager.ShowUpgradeMessage();
                return;
            }

            // Always refresh so newly created or edited prompts appear immediately
            // without requiring a Trados restart.
            _library.Refresh();
            var prompts = _library.GetQuickLauncherPrompts();

            if (prompts.Count == 0)
            {
                MessageBox.Show(
                    "No QuickLauncher prompts are configured.\n\n" +
                    "Set category: QuickLauncher in a prompt file's YAML frontmatter, " +
                    "or place the file in a folder named 'QuickLauncher' inside your prompt library.",
                    "Supervertaler \u2014 QuickLauncher",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Gather segment context once (before showing the menu)
            var editorController = SdlTradosStudio.Application.GetController<EditorController>();
            var doc = editorController?.ActiveDocument;

            var sourceText = "";
            var targetText = "";
            var selection = "";
            var sourceLang = "";
            var targetLang = "";
            var projectName = "";
            var documentName = "";

            if (doc != null)
            {
                sourceText = doc.ActiveSegmentPair?.Source != null
                    ? SegmentTagHandler.GetFinalText(doc.ActiveSegmentPair.Source) : "";
                targetText = doc.ActiveSegmentPair?.Target != null
                    ? SegmentTagHandler.GetFinalText(doc.ActiveSegmentPair.Target) : "";

                try
                {
                    var sel = doc.Selection;
                    if (sel != null)
                    {
                        var srcSel = sel.Source?.ToString();
                        var tgtSel = sel.Target?.ToString();
                        // Use whichever side has a selection; prefer source
                        if (!string.IsNullOrWhiteSpace(srcSel))
                            selection = srcSel.Trim();
                        else if (!string.IsNullOrWhiteSpace(tgtSel))
                            selection = tgtSel.Trim();
                    }
                }
                catch { /* Selection API may not be available */ }

                try
                {
                    var file = doc.ActiveFile;
                    if (file != null)
                    {
                        sourceLang = file.SourceFile?.Language?.DisplayName ?? "";
                        targetLang = file.Language?.DisplayName ?? "";
                    }
                }
                catch { /* Language info may not be available */ }

                projectName = DocumentContextHelper.GetProjectName(doc);
                documentName = DocumentContextHelper.GetDocumentName(doc);
            }

            // Load settings once for surrounding segments count
            var settings = TermLensSettings.Load();
            var surroundingCount = settings?.AiSettings?.QuickLauncherSurroundingSegments ?? 5;

            // Build and show the context menu at the current cursor position.
            // Do NOT use a 'using' block or dispose on Closed — Show() is non-blocking
            // and Closed fires before item click handlers run, causing ObjectDisposedException.
            // ContextMenuStrip is small; GC handles it.
            var menu = new ContextMenuStrip();
            menu.ShowItemToolTips = true;

            // Header: "Supervertaler QuickLauncher" — opens Settings → Prompts tab
            var header = new ToolStripMenuItem("Supervertaler QuickLauncher");
            header.Font = new System.Drawing.Font(header.Font, System.Drawing.FontStyle.Bold);
            header.ToolTipText = "Click to open the Prompt Manager";
            header.Click += (s, e) =>
            {
                using (var form = new Settings.TermLensSettingsForm(
                    Settings.TermLensSettings.Load(), new Core.PromptLibrary(), defaultTab: 3))
                {
                    form.ShowDialog();
                }
            };
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());

            // Determine whether custom slot assignments exist
            var hasCustomSlots = settings?.AiSettings?.QuickLauncherSlots != null
                                 && settings.AiSettings.QuickLauncherSlots.Count > 0;

            // Build a position map for keyboard shortcut numbering (flat order).
            // This keeps Ctrl+Alt+1..0 consistent with QuickLauncherSlotRunner.
            var slotPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < prompts.Count; i++)
            {
                if (!string.IsNullOrEmpty(prompts[i].FilePath))
                    slotPositions[prompts[i].FilePath] = i + 1; // 1-based
            }

            // Build folder tree and populate the menu recursively
            var folderTree = _library.GetQuickLauncherFolderStructure();

            // Determine which folders should render as flat sections
            var flatFolders = settings?.AiSettings?.QuickLauncherFlatFolders ?? new List<string>();

            // Add subfolders first (Default pinned first by GetQuickLauncherFolderStructure)
            foreach (var child in folderTree.Children)
            {
                // Tree node tags use the full relative path (e.g. "QuickLauncher/Default")
                // while GetQuickLauncherFolderStructure strips the prefix (e.g. "Default").
                // Check both forms so the setting always matches.
                var rel = child.RelativePath ?? "";
                var fullRel = "QuickLauncher/" + rel;
                bool isFlat = flatFolders.Contains(rel) || flatFolders.Contains(fullRel);

                if (isFlat)
                {
                    // Flat section: separator, bold header, then items directly in the menu
                    var items = new List<ToolStripItem>();
                    PopulateFlatSection(items, child, flatFolders, slotPositions,
                        hasCustomSlots, settings, doc, sourceText, targetText, selection,
                        sourceLang, targetLang, projectName, documentName, surroundingCount);

                    if (items.Count > 0)
                    {
                        // Only add separator if the last item isn't already one
                        if (menu.Items.Count > 0 && !(menu.Items[menu.Items.Count - 1] is ToolStripSeparator))
                            menu.Items.Add(new ToolStripSeparator());

                        var sectionHeader = new ToolStripMenuItem(child.Name.ToUpperInvariant() + ":");
                        sectionHeader.Font = new System.Drawing.Font(sectionHeader.Font, System.Drawing.FontStyle.Bold);
                        sectionHeader.Enabled = false;
                        menu.Items.Add(sectionHeader);

                        foreach (var item in items)
                            menu.Items.Add(item);
                    }
                }
                else
                {
                    // Expandable submenu (existing behaviour)
                    var subMenu = new ToolStripMenuItem(child.Name);
                    PopulateFolderMenu(subMenu.DropDownItems, child, slotPositions,
                        hasCustomSlots, settings, doc, sourceText, targetText, selection,
                        sourceLang, targetLang, projectName, documentName, surroundingCount);

                    if (subMenu.DropDownItems.Count > 0)
                        menu.Items.Add(subMenu);
                }
            }

            // Add top-level prompts (not in any subfolder)
            if (folderTree.Prompts.Count > 0)
            {
                if (folderTree.Children.Count > 0)
                    menu.Items.Add(new ToolStripSeparator());

                foreach (var p in folderTree.Prompts)
                {
                    slotPositions.TryGetValue(p.FilePath ?? "", out var slotNum);
                    menu.Items.Add(CreatePromptMenuItem(p, slotNum, hasCustomSlots, settings,
                        doc, sourceText, targetText, selection,
                        sourceLang, targetLang, projectName, documentName, surroundingCount));
                }
            }

            menu.Show(Cursor.Position);
        }

        /// <summary>
        /// Collects menu items from a folder (and its children) into a flat list
        /// for rendering as a section with a bold header.
        /// Child subfolders that are also marked as flat get their own section header;
        /// others are rendered as expandable submenus.
        /// </summary>
        private void PopulateFlatSection(
            List<ToolStripItem> items,
            PromptFolderNode folder,
            List<string> flatFolders,
            Dictionary<string, int> slotPositions,
            bool hasCustomSlots,
            TermLensSettings settings,
            Sdl.TranslationStudioAutomation.IntegrationApi.IStudioDocument doc,
            string sourceText, string targetText, string selection,
            string sourceLang, string targetLang,
            string projectName, string documentName, int surroundingCount)
        {
            // Add prompts in this folder first
            foreach (var p in folder.Prompts)
            {
                slotPositions.TryGetValue(p.FilePath ?? "", out var slotNum);
                items.Add(CreatePromptMenuItem(p, slotNum, hasCustomSlots, settings,
                    doc, sourceText, targetText, selection,
                    sourceLang, targetLang, projectName, documentName, surroundingCount));
            }

            // Then child subfolders — flat children get their own section header
            foreach (var child in folder.Children)
            {
                var childRel = child.RelativePath ?? "";
                var childFullRel = "QuickLauncher/" + childRel;
                bool childIsFlat = flatFolders.Contains(childRel) || flatFolders.Contains(childFullRel);

                if (childIsFlat)
                {
                    var childItems = new List<ToolStripItem>();
                    PopulateFlatSection(childItems, child, flatFolders, slotPositions,
                        hasCustomSlots, settings, doc, sourceText, targetText, selection,
                        sourceLang, targetLang, projectName, documentName, surroundingCount);

                    if (childItems.Count > 0)
                    {
                        items.Add(new ToolStripSeparator());

                        var childHeader = new ToolStripMenuItem(child.Name.ToUpperInvariant() + ":");
                        childHeader.Font = new System.Drawing.Font(childHeader.Font, System.Drawing.FontStyle.Bold);
                        childHeader.Enabled = false;
                        items.Add(childHeader);

                        foreach (var ci in childItems)
                            items.Add(ci);
                    }
                }
                else
                {
                    var subMenu = new ToolStripMenuItem(child.Name);
                    PopulateFolderMenu(subMenu.DropDownItems, child, slotPositions,
                        hasCustomSlots, settings, doc, sourceText, targetText, selection,
                        sourceLang, targetLang, projectName, documentName, surroundingCount);

                    if (subMenu.DropDownItems.Count > 0)
                        items.Add(subMenu);
                }
            }
        }

        /// <summary>
        /// Recursively populates a menu/submenu from a PromptFolderNode.
        /// </summary>
        private void PopulateFolderMenu(
            ToolStripItemCollection parent,
            PromptFolderNode folder,
            Dictionary<string, int> slotPositions,
            bool hasCustomSlots,
            TermLensSettings settings,
            Sdl.TranslationStudioAutomation.IntegrationApi.IStudioDocument doc,
            string sourceText, string targetText, string selection,
            string sourceLang, string targetLang,
            string projectName, string documentName, int surroundingCount)
        {
            // Add child subfolders first
            foreach (var child in folder.Children)
            {
                var subMenu = new ToolStripMenuItem(child.Name);
                PopulateFolderMenu(subMenu.DropDownItems, child, slotPositions,
                    hasCustomSlots, settings, doc, sourceText, targetText, selection,
                    sourceLang, targetLang, projectName, documentName, surroundingCount);

                if (subMenu.DropDownItems.Count > 0)
                    parent.Add(subMenu);
            }

            // Separator between subfolders and prompts
            if (folder.Children.Count > 0 && folder.Prompts.Count > 0)
                parent.Add(new ToolStripSeparator());

            // Add prompts in this folder
            foreach (var p in folder.Prompts)
            {
                slotPositions.TryGetValue(p.FilePath ?? "", out var slotNum);
                parent.Add(CreatePromptMenuItem(p, slotNum, hasCustomSlots, settings,
                    doc, sourceText, targetText, selection,
                    sourceLang, targetLang, projectName, documentName, surroundingCount));
            }
        }

        /// <summary>
        /// Creates a single ToolStripMenuItem for a QuickLauncher prompt,
        /// including shortcut display and click handler.
        /// </summary>
        private ToolStripMenuItem CreatePromptMenuItem(
            PromptTemplate prompt, int slotNum,
            bool hasCustomSlots, TermLensSettings settings,
            Sdl.TranslationStudioAutomation.IntegrationApi.IStudioDocument doc,
            string sourceText, string targetText, string selection,
            string sourceLang, string targetLang,
            string projectName, string documentName, int surroundingCount)
        {
            var item = new ToolStripMenuItem(prompt.MenuLabel);

            if (hasCustomSlots)
            {
                var shortcutDisplay = QuickLauncherSlotRunner.GetShortcutDisplay(
                    prompt.FilePath, settings?.AiSettings);
                if (shortcutDisplay != null)
                    item.ShortcutKeyDisplayString = shortcutDisplay;
            }
            else if (slotNum >= 1 && slotNum <= 10)
            {
                var keyDigit = slotNum == 10 ? "0" : slotNum.ToString();
                item.ShortcutKeyDisplayString = $"Ctrl+Alt+{keyDigit}";
            }

            if (!string.IsNullOrEmpty(prompt.Description))
                item.ToolTipText = prompt.Description;

            // Capture for closure
            var capturedPrompt = prompt;
            var capturedDoc = doc;
            var capturedSourceText = sourceText;
            var capturedTargetText = targetText;
            var capturedSelection = selection;
            var capturedSourceLang = sourceLang;
            var capturedTargetLang = targetLang;
            var capturedProjectName = projectName;
            var capturedDocumentName = documentName;
            var capturedSurroundingCount = surroundingCount;

            item.Click += (s, e) =>
            {
                // Text transforms: apply find/replace directly to target — no AI call
                if (capturedPrompt.IsTransform)
                {
                    var result = AiAssistantViewPart.RunTextTransform(capturedPrompt);
                    AiAssistantViewPart.ShowTransformResult(capturedPrompt.Name, result);
                    return;
                }

                var content = capturedPrompt.Content;

                // Lazily gather expensive context only if the prompt actually uses it
                var surroundingSegments = content.Contains("{{SURROUNDING_SEGMENTS}}")
                    ? DocumentContextHelper.FormatSurroundingSegments(capturedDoc, capturedSurroundingCount)
                    : null;

                var projectText = content.Contains("{{PROJECT}}")
                    ? DocumentContextHelper.FormatProjectText(capturedDoc)
                    : null;

                var tmMatchesText = content.Contains("{{TM_MATCHES}}")
                    ? PromptLibrary.FormatTmMatches(
                        DocumentContextHelper.GetTmMatches(capturedDoc), 70)
                    : null;

                var expanded = PromptLibrary.ApplyVariables(
                    content,
                    capturedSourceLang, capturedTargetLang,
                    capturedSourceText, capturedTargetText, capturedSelection,
                    capturedProjectName, capturedDocumentName,
                    surroundingSegments, projectText, tmMatchesText);

                string displayExpanded = null;
                if (projectText != null)
                {
                    var segCount = 0;
                    foreach (var line in projectText.Split('\n'))
                        if (line.TrimStart().StartsWith("[")) segCount++;

                    var placeholder = $"[source document \u2014 {segCount} segment{(segCount == 1 ? "" : "s")}]";
                    displayExpanded = PromptLibrary.ApplyVariables(
                        content,
                        capturedSourceLang, capturedTargetLang,
                        capturedSourceText, capturedTargetText, capturedSelection,
                        capturedProjectName, capturedDocumentName,
                        surroundingSegments, placeholder, tmMatchesText);
                }

                AiAssistantViewPart.RunQuickLauncherPrompt(expanded, displayExpanded, capturedPrompt.Name);
            };

            return item;
        }
    }
}
