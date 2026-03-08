using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.Desktop.IntegrationApi.Interfaces;
using Sdl.FileTypeSupport.Framework.BilingualApi;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Supervertaler.Trados.Controls;
using Supervertaler.Trados.Core;
using Supervertaler.Trados.Models;
using Supervertaler.Trados.Settings;

namespace Supervertaler.Trados
{
    /// <summary>
    /// Trados Studio editor ViewPart that docks the TermLens panel above the editor.
    /// Listens to segment changes and updates the terminology display accordingly.
    /// </summary>
    [ViewPart(
        Id = "TermLensEditorViewPart",
        Name = "Supervertaler TermLens",
        Description = "Terminology display for Trados Studio",
        Icon = "TermLensIcon"
    )]
    [ViewPartLayout(typeof(EditorController), Dock = DockType.Top, Pinned = true)]
    public class TermLensEditorViewPart : AbstractViewPartController
    {
        private static readonly Lazy<TermLensControl> _control =
            new Lazy<TermLensControl>(() => new TermLensControl());

        private static readonly Lazy<MainPanelControl> _mainPanel =
            new Lazy<MainPanelControl>(() => new MainPanelControl(_control.Value));

        // Single instance — Trados creates exactly one ViewPart of each type.
        // Used by AddTermAction to trigger a reload after inserting a term.
        private static TermLensEditorViewPart _currentInstance;

        private EditorController _editorController;
        private IStudioDocument _activeDocument;
        private TermLensSettings _settings;

        // Prompt library (shared — used by settings dialog)
        private PromptLibrary _promptLibrary;

        // --- Alt+digit chord state machine ---
        private static int? _pendingDigit;
        private static System.Windows.Forms.Timer _chordTimer;

        protected override IUIControl GetContentControl()
        {
            return _mainPanel.Value;
        }

        protected override void Initialize()
        {
            _currentInstance = this;

            // Load persisted settings
            _settings = TermLensSettings.Load();

            // Initialize prompt library and seed built-in prompts on first run
            _promptLibrary = new PromptLibrary();
            _promptLibrary.EnsureBuiltInPrompts();

            _editorController = SdlTradosStudio.Application.GetController<EditorController>();

            if (_editorController != null)
            {
                _editorController.ActiveDocumentChanged += OnActiveDocumentChanged;

                // If a document is already open, wire up to it immediately
                if (_editorController.ActiveDocument != null)
                {
                    _activeDocument = _editorController.ActiveDocument;
                    _activeDocument.ActiveSegmentChanged += OnActiveSegmentChanged;
                }
            }

            // Wire up term insertion — when user clicks a translation in the panel
            _control.Value.TermInsertRequested += OnTermInsertRequested;

            // Wire up right-click edit/delete/non-translatable on term blocks
            _control.Value.TermEditRequested += OnTermEditRequested;
            _control.Value.TermDeleteRequested += OnTermDeleteRequested;
            _control.Value.TermNonTranslatableToggled += OnTermNonTranslatableToggled;

            // Wire up the gear/settings button (on the MainPanelControl, visible on all tabs)
            _mainPanel.Value.SettingsRequested += OnSettingsRequested;

            // Wire up font size changes from the A+/A- buttons in the panel header
            _control.Value.FontSizeChanged += OnFontSizeChanged;

            // Apply persisted font size
            _control.Value.SetFontSize(_settings.PanelFontSize);

            // Load termbase: prefer saved setting, fall back to auto-detect
            LoadTermbase();

            // Display the current segment immediately (even without a termbase, show all words)
            UpdateFromActiveSegment();
        }

        private void LoadTermbase(bool forceReload = false)
        {
            var disabled = _settings.DisabledTermbaseIds != null && _settings.DisabledTermbaseIds.Count > 0
                ? new HashSet<long>(_settings.DisabledTermbaseIds)
                : null;

            // Push project termbase ID to the control for pink/blue coloring
            _control.Value.SetProjectTermbaseId(_settings.ProjectTermbaseId);

            // 1. Use the saved termbase path if set and the file exists
            if (!string.IsNullOrEmpty(_settings.TermbasePath) && File.Exists(_settings.TermbasePath))
            {
                _control.Value.LoadTermbase(_settings.TermbasePath, disabled, forceReload);
                return;
            }

            // 2. Fallback: auto-detect Supervertaler's default locations
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Supervertaler_Data", "resources", "supervertaler.db"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Supervertaler", "resources", "supervertaler.db"),
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    _control.Value.LoadTermbase(path, disabled, forceReload);
                    return;
                }
            }
        }

        private void OnSettingsRequested(object sender, EventArgs e)
        {
            SafeInvoke(() =>
            {
                using (var form = new TermLensSettingsForm(_settings, _promptLibrary, defaultTab: 0))
                {
                    // Find a parent window handle for proper dialog parenting
                    var parent = _control.Value.FindForm();
                    var result = parent != null
                        ? form.ShowDialog(parent)
                        : form.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        // Settings already saved inside the form's OK handler.
                        // Apply font size change (user may have adjusted it in settings)
                        _control.Value.SetFontSize(_settings.PanelFontSize);

                        // Force reload — the user may have toggled glossaries.
                        LoadTermbase(forceReload: true);
                        UpdateFromActiveSegment();

                        // Refresh prompt library (user may have added/edited/deleted prompts)
                        _promptLibrary.Refresh();
                    }
                }
            });
        }

        private void OnFontSizeChanged(object sender, EventArgs e)
        {
            // Persist the new font size from the A+/A- buttons
            _settings.PanelFontSize = _control.Value.Font.Size;
            _settings.Save();

            // Refresh the segment display with the new font
            UpdateFromActiveSegment();
        }

        private void OnActiveDocumentChanged(object sender, DocumentEventArgs e)
        {
            if (_activeDocument != null)
            {
                _activeDocument.ActiveSegmentChanged -= OnActiveSegmentChanged;
            }

            _activeDocument = _editorController?.ActiveDocument;

            if (_activeDocument != null)
            {
                _activeDocument.ActiveSegmentChanged += OnActiveSegmentChanged;
                UpdateFromActiveSegment();
            }
            else
            {
                SafeInvoke(() => _control.Value.Clear());
            }
        }

        private void OnActiveSegmentChanged(object sender, EventArgs e)
        {
            UpdateFromActiveSegment();
        }

        private void UpdateFromActiveSegment()
        {
            if (_activeDocument?.ActiveSegmentPair == null)
            {
                SafeInvoke(() => _control.Value.Clear());
                return;
            }

            try
            {
                var sourceSegment = _activeDocument.ActiveSegmentPair.Source;
                var sourceText = GetPlainText(sourceSegment);
                SafeInvoke(() => _control.Value.UpdateSegment(sourceText));
            }
            catch (Exception)
            {
                // Silently handle — segment may not be available during transitions
            }
        }

        /// <summary>
        /// Extracts only the human-readable text from a Trados segment,
        /// skipping inline tag metadata (URLs, tag attributes, etc.).
        /// Falls back to ToString() if the bilingual API iteration fails.
        /// </summary>
        internal static string GetPlainText(ISegment segment)
        {
            if (segment == null) return "";
            try
            {
                var sb = new StringBuilder();
                foreach (var item in segment.AllSubItems)
                {
                    if (item is IText textItem)
                        sb.Append(textItem.Properties.Text);
                }
                var result = sb.ToString();
                // If we got text, use it; otherwise fall back to ToString()
                return !string.IsNullOrEmpty(result) ? result : segment.ToString() ?? "";
            }
            catch
            {
                return segment.ToString() ?? "";
            }
        }

        private void SafeInvoke(Action action)
        {
            var ctrl = _control.Value;
            if (ctrl.InvokeRequired)
                ctrl.BeginInvoke(action);
            else
                action();
        }

        private void OnTermInsertRequested(object sender, TermInsertEventArgs e)
        {
            if (_activeDocument == null || string.IsNullOrEmpty(e.TargetTerm))
                return;

            try
            {
                _activeDocument.Selection.Target.Replace(e.TargetTerm, "TermLens");
            }
            catch (Exception)
            {
                // Silently handle — editor may not allow insertion at this moment
            }
        }

        private void OnTermEditRequested(object sender, TermEditEventArgs e)
        {
            if (e.Entry == null) return;

            SafeInvoke(() =>
            {
                var dbPath = _settings.TermbasePath;
                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return;

                // Multi-entry mode: look up termbase info for ALL entries
                var allEntries = e.AllEntries;
                if (allEntries != null && allEntries.Count > 1)
                {
                    var entryTermbases = new List<KeyValuePair<TermEntry, TermbaseInfo>>();
                    using (var reader = new TermbaseReader(dbPath))
                    {
                        if (reader.Open())
                        {
                            foreach (var entry in allEntries)
                            {
                                var tb = reader.GetTermbaseById(entry.TermbaseId);
                                if (tb != null)
                                    entryTermbases.Add(new KeyValuePair<TermEntry, TermbaseInfo>(entry, tb));
                            }
                        }
                    }

                    if (entryTermbases.Count == 0) return;

                    using (var dlg = new TermEntryEditorDialog(entryTermbases, dbPath))
                    {
                        var parent = _control.Value.FindForm();
                        var result = parent != null ? dlg.ShowDialog(parent) : dlg.ShowDialog();

                        if (result == DialogResult.OK || result == DialogResult.Abort)
                        {
                            // Force reload to rebuild index after save or delete
                            LoadTermbase(forceReload: true);
                            UpdateFromActiveSegment();
                        }
                    }
                    return;
                }

                // Single-entry mode (fallback)
                TermbaseInfo termbase = null;
                using (var reader = new TermbaseReader(dbPath))
                {
                    if (reader.Open())
                        termbase = reader.GetTermbaseById(e.Entry.TermbaseId);
                }

                using (var dlg = new TermEntryEditorDialog(e.Entry, dbPath, termbase))
                {
                    var parent = _control.Value.FindForm();
                    var result = parent != null ? dlg.ShowDialog(parent) : dlg.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        // Term was saved (possibly with synonym changes) — force reload
                        // to rebuild the index including source synonym keys
                        LoadTermbase(forceReload: true);
                        UpdateFromActiveSegment();
                    }
                    else if (result == DialogResult.Abort)
                    {
                        // Term was deleted from the editor
                        _control.Value.RemoveTermFromIndex(e.Entry.Id);
                        UpdateFromActiveSegment();
                    }
                }
            });
        }

        private void OnTermDeleteRequested(object sender, TermEditEventArgs e)
        {
            if (e.Entry == null) return;

            SafeInvoke(() =>
            {
                var confirmResult = MessageBox.Show(
                    $"Delete the term \u201c{e.Entry.SourceTerm} \u2192 {e.Entry.TargetTerm}\u201d?\n\n" +
                    "This cannot be undone.",
                    "TermLens \u2014 Delete Term",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (confirmResult != DialogResult.Yes) return;

                try
                {
                    bool deleted = TermbaseReader.DeleteTerm(
                        _settings.TermbasePath,
                        e.Entry.Id);

                    if (deleted)
                        NotifyTermDeleted(e.Entry.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to delete term: {ex.Message}\n\n" +
                        "The database may be locked by another application.",
                        "TermLens \u2014 Delete Term",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void OnTermNonTranslatableToggled(object sender, TermEditEventArgs e)
        {
            if (e.Entry == null) return;

            SafeInvoke(() =>
            {
                bool newState = !e.Entry.IsNonTranslatable;

                try
                {
                    bool updated = TermbaseReader.SetNonTranslatable(
                        _settings.TermbasePath, e.Entry.Id, newState, e.Entry.SourceTerm);

                    if (updated)
                    {
                        // Incremental update: remove old entry, add updated one
                        _control.Value.RemoveTermFromIndex(e.Entry.Id);
                        var updatedEntry = new TermEntry
                        {
                            Id = e.Entry.Id,
                            SourceTerm = e.Entry.SourceTerm,
                            TargetTerm = newState ? e.Entry.SourceTerm : e.Entry.TargetTerm,
                            SourceLang = e.Entry.SourceLang,
                            TargetLang = e.Entry.TargetLang,
                            TermbaseId = e.Entry.TermbaseId,
                            TermbaseName = e.Entry.TermbaseName,
                            IsProjectTermbase = e.Entry.IsProjectTermbase,
                            Ranking = e.Entry.Ranking,
                            Definition = e.Entry.Definition ?? "",
                            Domain = e.Entry.Domain,
                            Notes = e.Entry.Notes,
                            Forbidden = e.Entry.Forbidden,
                            CaseSensitive = e.Entry.CaseSensitive,
                            IsNonTranslatable = newState,
                            TargetSynonyms = e.Entry.TargetSynonyms
                        };
                        _control.Value.AddTermToIndex(updatedEntry);
                        UpdateFromActiveSegment();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to toggle non-translatable: {ex.Message}\n\n" +
                        "The database may be locked by another application.",
                        "TermLens \u2014 Non-Translatable",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        /// <summary>
        /// Called by AddTermAction after a term is inserted.
        /// Reloads settings and the term index so the new term appears immediately.
        /// </summary>
        public static void NotifyTermAdded()
        {
            var instance = _currentInstance;
            if (instance == null) return;

            // Re-read settings in case WriteTermbaseId or disabled list changed
            instance._settings = TermLensSettings.Load();
            instance.LoadTermbase(forceReload: true);
            instance.UpdateFromActiveSegment();
        }

        /// <summary>
        /// Called after a term is inserted via quick-add. Incrementally updates the
        /// in-memory index and refreshes the segment display, without reloading the
        /// entire database. Much faster than NotifyTermAdded() for single inserts.
        /// </summary>

        // ─── Context sharing for AI Assistant ─────────────────────

        /// <summary>
        /// Returns all loaded termbase terms for the AI Assistant context.
        /// Returns already-computed data — no DB queries.
        /// </summary>
        public static List<TermEntry> GetCurrentTermbaseTerms()
        {
            if (_currentInstance == null) return new List<TermEntry>();
            try { return _control.Value.GetAllLoadedTerms() ?? new List<TermEntry>(); }
            catch { return new List<TermEntry>(); }
        }

        /// <summary>
        /// Returns the matched terms for the active segment.
        /// Used by the AI Assistant to inject terminology context into prompts.
        /// Returns already-computed data — no DB queries.
        /// </summary>
        public static List<TermPickerMatch> GetCurrentSegmentMatches()
        {
            if (_currentInstance == null) return new List<TermPickerMatch>();
            try { return _control.Value.GetCurrentMatches() ?? new List<TermPickerMatch>(); }
            catch { return new List<TermPickerMatch>(); }
        }

        public static void NotifyTermInserted(List<Models.TermEntry> newEntries)
        {
            var instance = _currentInstance;
            if (instance == null) return;

            foreach (var entry in newEntries)
                _control.Value.AddTermToIndex(entry);

            instance.UpdateFromActiveSegment();
        }

        /// <summary>
        /// Called after a term is deleted. Removes it from the in-memory index
        /// and refreshes the segment display, without reloading the database.
        /// </summary>
        public static void NotifyTermDeleted(long termId)
        {
            var instance = _currentInstance;
            if (instance == null) return;

            _control.Value.RemoveTermFromIndex(termId);
            instance.UpdateFromActiveSegment();
        }

        /// <summary>
        /// Returns the prompt library for sharing with other ViewParts (e.g., AiAssistantViewPart).
        /// </summary>
        public static PromptLibrary GetPromptLibrary()
        {
            return _currentInstance?._promptLibrary;
        }

        private string GetDocumentSourceLanguage()
        {
            try
            {
                var file = _activeDocument?.ActiveFile;
                if (file != null)
                {
                    var lang = file.SourceFile?.Language;
                    if (lang != null)
                        return lang.DisplayName;
                }
            }
            catch (Exception) { }
            return null;
        }

        private string GetDocumentTargetLanguage()
        {
            try
            {
                var file = _activeDocument?.ActiveFile;
                if (file != null)
                {
                    var lang = file.Language;
                    if (lang != null)
                        return lang.DisplayName;
                }
            }
            catch (Exception) { }
            return null;
        }

        // ─── Alt+digit term insertion ────────────────────────────────

        /// <summary>
        /// Called by TermInsertDigitNAction when Alt+digit is pressed.
        /// Implements a two-digit chord state machine with 400ms timeout.
        /// </summary>
        public static void HandleDigitPress(int digit)
        {
            var instance = _currentInstance;
            if (instance == null) return;

            // If there's already a pending first digit, combine into a two-digit number
            if (_pendingDigit.HasValue)
            {
                StopChordTimer();
                int number = _pendingDigit.Value * 10 + digit;
                _pendingDigit = null;
                instance.InsertTermByIndex(number);
                return;
            }

            // Check how many matched terms are in the current segment
            int matchCount = _control.Value.MatchCount;

            if (matchCount <= 9)
            {
                // ≤9 terms: insert immediately, no chord wait needed
                int number = digit == 0 ? 10 : digit;
                instance.InsertTermByIndex(number);
            }
            else
            {
                // 10+ terms: start chord timer, wait for possible second digit
                _pendingDigit = digit;
                StartChordTimer();
            }
        }

        private static void StartChordTimer()
        {
            StopChordTimer();
            _chordTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _chordTimer.Tick += OnChordTimerTick;
            _chordTimer.Start();
        }

        private static void StopChordTimer()
        {
            if (_chordTimer != null)
            {
                _chordTimer.Stop();
                _chordTimer.Tick -= OnChordTimerTick;
                _chordTimer.Dispose();
                _chordTimer = null;
            }
        }

        private static void OnChordTimerTick(object sender, EventArgs e)
        {
            StopChordTimer();

            var instance = _currentInstance;
            if (instance == null || !_pendingDigit.HasValue) return;

            int digit = _pendingDigit.Value;
            _pendingDigit = null;

            // Single digit: 0 means term 10, otherwise 1-9
            int number = digit == 0 ? 10 : digit;
            instance.InsertTermByIndex(number);
        }

        private void InsertTermByIndex(int oneBasedIndex)
        {
            if (_activeDocument == null) return;

            var entry = _control.Value.GetTermByIndex(oneBasedIndex);
            if (entry == null) return;

            try
            {
                _activeDocument.Selection.Target.Replace(entry.TargetTerm, "TermLens");
            }
            catch (Exception)
            {
                // Silently handle — editor may not allow insertion at this moment
            }
        }

        // ─── Term Picker dialog ─────────────────────────────────────

        /// <summary>
        /// Called by TermPickerAction (Ctrl+Shift+G).
        /// Opens a dialog showing all matched terms for the current segment.
        /// </summary>
        public static void HandleTermPicker()
        {
            var instance = _currentInstance;
            if (instance == null || instance._activeDocument == null) return;

            var matches = _control.Value.GetCurrentMatches();
            if (matches.Count == 0) return;

            instance.SafeInvoke(() =>
            {
                using (var dlg = new TermPickerDialog(matches, instance._settings))
                {
                    var parent = _control.Value.FindForm();
                    var result = parent != null
                        ? dlg.ShowDialog(parent)
                        : dlg.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedTargetTerm))
                    {
                        try
                        {
                            instance._activeDocument.Selection.Target.Replace(
                                dlg.SelectedTargetTerm, "TermLens");
                        }
                        catch (Exception)
                        {
                            // Silently handle
                        }
                    }
                }
            });
        }

        // ─────────────────────────────────────────────────────────────

        public override void Dispose()
        {
            if (_currentInstance == this)
                _currentInstance = null;

            StopChordTimer();

            if (_activeDocument != null)
            {
                _activeDocument.ActiveSegmentChanged -= OnActiveSegmentChanged;
            }

            if (_editorController != null)
                _editorController.ActiveDocumentChanged -= OnActiveDocumentChanged;

            base.Dispose();
        }
    }
}
