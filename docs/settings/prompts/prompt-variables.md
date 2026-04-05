{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

Variables are placeholders in your prompt text that are automatically filled in at runtime. Use them to make prompts context-aware without rewriting them for every project or language pair.

### Language variables – all contexts

These work in Batch Translate, Batch Proofread, and QuickLauncher prompts:

| Variable              | Replaced with                                      | Example                   |
| --------------------- | -------------------------------------------------- | ------------------------- |
| `{{SOURCE_LANGUAGE}}` | Full name of the source language, including locale | `Dutch (Belgium)`         |
| `{{TARGET_LANGUAGE}}` | Full name of the target language, including locale | `English (United States)` |

### Segment variables – QuickLauncher only

These are only available in QuickLauncher prompts, because they refer to the specific segment active at the moment you trigger the menu:

| Variable             | Replaced with                                                                                  | Example                                                     |
| -------------------- | ---------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `{{SOURCE_SEGMENT}}` | Full source text of the **active segment**                                                     | `De uitvinding heeft betrekking op een nieuwe werkwijze...` |
| `{{TARGET_SEGMENT}}` | Full target text of the **active segment** (your translation so far – may be empty or partial) | `The invention relates to a novel method...`                |
| `{{SELECTION}}`      | Text currently **selected** in the editor (source side preferred; falls back to target side)   | `werkwijze`                                                 |

{% hint style="info" %}
**Segment vs selection:** `{{SOURCE_SEGMENT}}` and `{{TARGET_SEGMENT}}` always give you the **entire active segment**. `{{SELECTION}}` gives you only the **highlighted portion** – useful for looking up or explaining a specific word or phrase within the segment. If nothing is selected, `{{SELECTION}}` is replaced with an empty string.
{% endhint %}

### Project variables – QuickLauncher only

| Variable                   | Replaced with                                                                                                                                                                                                    |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `{{PROJECT_NAME}}`         | Trados project name (e.g. `Patent_NL_EN_2026`)                                                                                                                                                                   |
| `{{DOCUMENT_NAME}}`        | Active file name (e.g. `source_document.docx`)                                                                                                                                                                   |
| `{{SURROUNDING_SEGMENTS}}` | N source segments before and after the active segment, with actual Trados segment numbers and the active segment marked `← ACTIVE`. N is set in **Settings → AI Settings → Surrounding segments** (default: 5).  |
| `{{PROJECT}}`              | All source segments in the document, numbered with their actual Trados segment numbers. In multi-file projects a `=== File N ===` header separates each file (Trados restarts segment numbering per file).       |
| `{{TM_MATCHES}}`           | Translation memory fuzzy matches (≥70%) for the active segment, showing match percentage, TM name, source text, and target text. If no matches meet the threshold, replaced with "(no fuzzy matches above 70%)". |

{% hint style="warning" %}
`{{PROJECT}}` sends the entire document to the AI and uses significantly more tokens than other variables. For a 10,000-word document, this costs roughly 4–5 cents per call with a Sonnet-class model. Reserve it for prompts where full document context genuinely matters.

To keep the chat history readable, the chat bubble shows a compact summary (e.g. `[source document – 47 segments]`) instead of the full source text. The complete document is still sent to the AI.
{% endhint %}
