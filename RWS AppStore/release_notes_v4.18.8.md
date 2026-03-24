# RWS App Store Manager — v4.18.8.0

**Version number:** `4.18.8.0`
**Minimum studio version:** `18.0`
**Maximum studio version:** `18.9`
**Checksum:** `dea6d6bf8faf0447b42a06bc28ea9862b021379afa5a13739712659e97108ff5`

---

## Changelog (since v4.18.3)

### v4.18.8 — 2026-03-24

#### Fixed
- **In-plugin purchase links now use live checkout** — the "Buy" links in Settings → License were still pointing to test mode URLs

### v4.18.7 — 2026-03-24

#### Changed
- **Trial period reduced to 14 days** — down from 90 days; the free trial now runs for 14 days from first launch
- **Live payment processing** — switched from test mode to live Lemon Squeezy checkout

### v4.18.6 — 2026-03-23

#### Added
- **Prompt inspector in Reports tab** — every AI API call can now be logged with the full system prompt, messages, response, token counts, and estimated cost; enable via "Log prompts and responses to Reports tab" in AI Settings
- **Expandable prompt sections** — click "Show system prompt...", "Show messages...", or "Show response..." to view the full text; press Escape to collapse; "Copy" copies a single section, "Copy all" copies everything
- **Batch translate and proofread logging** — batch operations now appear in the Reports tab when prompt logging is enabled
- **Prompt name in Reports tab** — entries show the prompt template name (e.g. "QuickLauncher · Explain in Context · 14:32:05")
- **Clone prompt** — right-click any prompt in the Prompt Manager and select "Clone" to create a copy with "(2)" appended
- **QuickLauncher menu heading** — Ctrl+Q menu now shows "Supervertaler QuickLauncher" at the top; click it to open Settings → Prompts tab

#### Fixed
- **Tracked changes no longer corrupt term additions** — Add Term, Quick-Add, Non-Translatable, QuickLauncher prompts, and Expand Selection now strip deleted tracked changes, adding only the final text
- **QuickLauncher segment context** — QuickLauncher prompts now pass clean segment text without tracked changes markup
- **Prompt log cards no longer squashed** — the resize handler was recalculating prompt log card heights using the proofreading layout logic, hiding all expandable sections
- **New prompts in subfolders now saved correctly** — creating a prompt while a subfolder was selected would create a wrongly-named folder instead of placing the file in the correct subfolder

### v4.18.4 — 2026-03-23

#### Fixed
- **Tracked changes no longer confuse AI features** — proofreading, batch translation, AI chat, and prompt generation now read only the final (accepted) text from segments with tracked changes, instead of including both deleted and inserted text

#### Changed
- **Updated LLM model lineup** — OpenAI: GPT-4.1, GPT-4.1 Mini, GPT-5.4, o4-mini; Gemini: added 3.1 Pro (Preview); Ollama: Qwen 3 bumped to 14B
- **Descriptive model tooltips** — each model now shows a short description to help translators choose the right one

---
For the full changelog, see: https://github.com/Supervertaler/Supervertaler-for-Trados/releases
