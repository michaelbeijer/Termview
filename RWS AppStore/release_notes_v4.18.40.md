# RWS App Store Manager — v4.18.40.0

**Version number:** `4.18.40.0`
**Minimum studio version:** `18.0`
**Maximum studio version:** `18.9`

---

## Changelog

### Added
- **SuperMemory** — a self-organizing, AI-maintained translation knowledge base that replaces traditional TMs and term bases with a living wiki of interlinked Markdown files. Stores client profiles, terminology decisions, domain conventions, and style preferences in a human-readable vault that the AI consults when translating. Inspired by Andrej Karpathy's LLM knowledge base architecture. See the [SuperMemory documentation](https://supervertaler.gitbook.io/trados/features/supermemory) for details
- **SuperMemory Quick Add (Ctrl+Alt+M)** — capture terms and corrections from the Trados editor into your SuperMemory vault. Also available via right-click in the editor grid. The dialog pre-fills from your source/target selection and adapts the correction label to your target language (e.g. "Correct Dutch form"). Optionally appends the term to the active translation prompt's terminology table so the correction takes effect on the next Ctrl+T
- **Per-project active prompt** — right-click any translation prompt in the Prompt Manager and choose "Set as active prompt for this project" to designate it as the active prompt. The active prompt is shown with a pin icon and bold blue text in the Prompt Manager, and with a checkmark in the Batch Translate dropdown. Saved per project so different projects can use different prompts
- **Active prompt auto-selection in Batch Translate** — when you open a project that has an active prompt set, the Batch Translate dropdown automatically selects it

### Improved
- **Selectable and copyable proofreading report text** — issue descriptions and suggestions in the Reports tab are now selectable text (click and drag, Ctrl+A, Ctrl+C). Right-click any issue card to copy the issue description, suggestion, or all text via a context menu. Clicking the segment number or card background still navigates to the segment in the editor
- **Batch Translate prompt dropdown layout** — fixed the AutoPrompt button disappearing off-screen when long prompt names were selected; the dropdown and button now resize properly with the panel width
- **Updated help documentation** — SuperMemory page updated with Quick Add and Active Prompt sections; keyboard shortcuts, batch translate, prompts, and project settings pages updated to reflect the new features

### Fixed
- **Active prompt indicator path comparison** — normalised path separators so the active prompt checkmark displays correctly regardless of whether paths were stored with forward or backward slashes

For the full changelog, see: https://github.com/Supervertaler/Supervertaler-for-Trados/releases
