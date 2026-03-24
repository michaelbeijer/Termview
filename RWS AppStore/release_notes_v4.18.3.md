# RWS App Store Manager — v4.18.3.0

**Version number:** `4.18.3.0`
**Minimum studio version:** `18.0`
**Maximum studio version:** `18.9`
**Checksum:** `507978ffd0e46a92ef01e45cb579bc37c1530030d2372233045b6ab49c39c92b`

---

## Changelog

### Added
- **Delete prompt folders** — right-click any folder in the prompt library tree and select "Delete Folder" to remove it and all prompts inside (with confirmation dialog)
- **Refresh button in Prompt Manager** — toolbar "Refresh" button reloads prompts from disk, reflecting any changes made outside Trados (e.g. in Windows Explorer or Supervertaler Workbench)
- **UI scale setting** — new General tab in Settings with UI scale selector for high-DPI displays
- **Chat font size controls** — A+/A− buttons in the AI Assistant to adjust chat bubble font size; persisted across sessions
- **Diagnostic logging for duplicate source terms** — temporary logging to `%LocalAppData%\Supervertaler.Trados\termlens_diag.log` when multiple entries share the same source term. Helps diagnose any remaining edge cases. Will be removed in a future release.
- **TreeView-based Prompt Manager** — the Prompts settings tab has been completely redesigned. The flat grid has been replaced with a folder-based tree view that mirrors the on-disk `prompt_library` structure. Click any prompt to see its content in the detail pane on the right. Click "System Prompt" to view and edit the system prompt. Folders can be created, and prompts can be dragged and dropped between folders.
- **QuickLauncher keyboard shortcuts** — assign Ctrl+Alt+1 through Ctrl+Alt+0 to individual QuickLauncher prompts in Settings → Prompts. Each shortcut runs the assigned prompt instantly without opening the menu. Shortcuts are shown next to prompt names in the Ctrl+Q menu.
- **Prompt reordering** — prompts within a folder can be moved up or down using the ▲/▼ buttons in the toolbar. The order is persisted via a `sort_order` field in the YAML frontmatter and applies to the Ctrl+Q menu as well.
- **Quick model switching** — click the provider/model label at the bottom of the AI Assistant chat to change models without opening Settings. Shows "Click to change model" tooltip on hover.
- **Document attachments** — attach documents directly to AI Assistant messages for context. The AI receives the full extracted text alongside your message. Supported formats: DOCX, DOC, PDF, RTF, PPTX, PPT, XLSX, XLS, CSV, TSV, TMX, SDLXLIFF, XLIFF/XLF, TBX, TXT, Markdown, HTML, JSON, and XML. Drag and drop files onto the chat input, or use the attach button to browse.
- **Multi-line Definition and Notes fields** — the term entry editor now uses expandable multi-line text areas for Definition and Notes fields, with a pop-open button to toggle between compact (3 lines) and expanded (8 lines) views. Line breaks in definitions and notes are now preserved correctly.
- **TermScan filtering for prompt generation** — "Analyse Project & Generate Prompt" now scans your document's source segments and filters termbase terms down to only those that actually appear in the document. This produces dramatically smaller, more focused glossaries in the generated prompt (e.g. 123 relevant terms from 2,680 total). The status message shows the filter count: "filtered X relevant from Y total".
- **One-click plugin update** — the "Update Available" dialogue now has an "Install Update" button that downloads and installs the new version directly, without opening a browser. Just click, restart Trados, and you're running the latest version.
- **"What's new" link in update dialogue** — view the release notes before updating.

### Changed
- **DPI-aware UI rendering** — TermLens, AI Assistant chat bubbles, term blocks, and settings dialog now scale correctly on high-DPI displays using the new `UiScale` helper
- **TermLens colour coding docs** — renamed "Lavender" to "Purple" for abbreviation match chips, as it is more recognisable as a colour name.
- **Improved term popup readability** — definition and notes text is now darker and more readable in the TermLens hover popup.
- **Multi-line term fields** — Definition and Notes in the term entry editor now have expand/collapse buttons (▲/▼) to toggle between compact and expanded views.
- **Resizable Prompts panel** — the divider between the tree and detail pane in Settings → Prompts can now be dragged left or right to resize.
- **Prompt generator respects TM toggle** — the "Analyse Project & Generate Prompt" feature now respects the "Include TM matches in AI context" setting. Previously, TM reference pairs were always collected regardless of this toggle.
- **Auto-restart after update** — clicking "Install Update" now offers to automatically restart Trados Studio, instead of asking the user to close and reopen it manually. Saves time on the lengthy Trados startup cycle.
- **Unified attach button** — the image-only attach button has been replaced with a universal paperclip (📎) icon that handles both images and documents. The file dialogue is organised into categories: Images, Documents, Spreadsheets, Translation files, and Text files. The tooltip lists all supported formats.
- **Improved term popup formatting** — definition and notes labels are now bold in the hover popup, and multi-line content uses hanging indentation so continuation lines align with the first line of text rather than the label.
- **Chat avatars** — each message in the AI Assistant now has a small avatar header: a gray "AI" circle with "Supervertaler Assistant" for AI responses, and a blue person silhouette with "You" for your messages. Makes it easy to tell who said what at a glance.
- **Animated thinking indicator** — the AI Assistant now shows a persistent animated bubble in the chat area while waiting for a response. The bubble cycles through reassuring messages ("Thinking…", "Still working on it…", "Generating response…", etc.) so you always know the AI is still processing. Previously, the thin "Thinking…" label at the bottom could disappear during long operations, making it look like the request had silently failed.
- **System-initiated messages styled as assistant bubbles** — messages triggered by buttons (e.g. "Analyse Project & Generate Prompt") now display with assistant styling (gray, left-aligned) instead of user styling, since you didn't type them yourself.
- **TM reference pairs filtered to confirmed segments** — the prompt generator now only includes human-confirmed segments (Translated, Approved, or Signed-off) as reference pairs. Unconfirmed AI-generated translations are excluded to avoid feeding unverified output back as "correct" references. Pairs are sampled evenly across the document for diversity.

### Fixed
- **Right-click context menu on prompt folders** — right-clicking a folder in the prompt library now correctly selects the folder before showing the context menu (WinForms TreeView doesn't auto-select on right-click)
- **Manifest version sync** — the `pluginpackage.manifest.xml` version was stuck at 4.18.0.0 while the DLL and plugin.xml were at 4.18.1.0. All three version files are now correctly synced to 4.18.2.
- **Editing terms in inverted-direction termbases corrupted entries** — when editing a term via the multi-entry editor (right-click → Edit Term) and the termbase direction was opposite to the project direction (e.g. EN→NL termbase in a NL→EN project), the source and target terms were saved swapped. This caused the edited entry to disappear from TermLens on the next refresh and left corrupted data in the termbase. The multi-entry editor now correctly detects and handles inverted termbase directions, matching the behaviour of the single-entry editor.
- **HttpClient timeout causing prompt generation failures** — the .NET HttpClient's default 100-second timeout was silently overriding our per-request timeout settings (up to 10 minutes). This caused all long-running API calls (prompt generation, large batch translations) to fail after exactly 100 seconds across all providers. Fixed by setting HttpClient.Timeout to infinite and managing timeouts via CancellationToken as intended.
- **Silent timeout errors** — when an API request timed out, the thinking indicator would disappear with no error message. Now shows a diagnostic message with model name, prompt size, and max output tokens to help troubleshoot.
- **Expand buttons in term editor** — fixed z-order issue where the Definition expand button was hidden behind the text box. Both expand buttons now render correctly.
- **Stale prompt dropdown** — deleting prompts from the Prompt Manager no longer leaves ghost entries in the Batch Operations prompt dropdown. The dropdown now refreshes whenever the Prompt Manager closes, regardless of whether you clicked OK or Cancel (because deletions happen immediately on disk).
- **API timeout for large output requests** — prompt generation and other requests that produce long AI responses (> 8,192 tokens) now use a 10-minute timeout instead of timing out prematurely. This prevents the "thinking" indicator from disappearing mid-generation on complex documents.
- **Prompt generation truncation** — the "Analyse Project & Generate Prompt" feature no longer cuts off long prompts. Output token limit increased from 4,096 to 32,768, allowing comprehensive prompts with large glossaries and TM reference pairs.
- **Correct version in plugin packages** — the `.sdlplugin` manifest now reads the version from the project file instead of using a hardcoded value. The DLL, manifest, and plugin.xml versions are guaranteed to match.
- **Stale assembly references** — fixed two action entries in plugin.xml that were stuck on old version numbers (4.5.0 and 4.10.0). The version bump script now uses pattern matching to catch all references, preventing this from recurring.