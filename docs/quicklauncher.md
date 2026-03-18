{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** — the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

# QuickLauncher

QuickLauncher gives you one-click access to your most-used AI prompts directly from the Trados editor, without switching panels or typing anything.

## How it works

1. **Right-click** anywhere in the editor (or press `Ctrl+Q`)
2. Click **QuickLauncher** in the context menu
3. Select a prompt from the list
4. The prompt is filled in with the current segment context and sent to the Supervertaler Assistant

The AI response appears in the **Supervertaler Assistant** chat panel.

## Keyboard shortcut

| Shortcut (Windows) | Shortcut (Mac) | Action |
|---------------------|----------------|--------|
| `Ctrl+Q` | `Control+Q` | Open QuickLauncher prompt menu |

{% hint style="warning" %}
Trados Studio assigns `Ctrl+Q` to **View Internally Source** by default. To use `Ctrl+Q` for QuickLauncher, go to **File → Options → Keyboard Shortcuts**, search for **View Internally Source**, and remove or reassign its shortcut.
{% endhint %}

## Prompt variables

QuickLauncher prompts have access to the full segment and project context at the moment you trigger them.

### Language variables

| Variable | Replaced with | Example |
|----------|---------------|---------|
| `{{SOURCE_LANGUAGE}}` | Source language name, including locale | `Dutch (Belgium)` |
| `{{TARGET_LANGUAGE}}` | Target language name, including locale | `English (United States)` |

### Segment variables

| Variable | Replaced with | Example |
|----------|---------------|---------|
| `{{SOURCE_SEGMENT}}` | Full text of the **active source segment** | `De stand der techniek...` |
| `{{TARGET_SEGMENT}}` | Full text of the **active target segment** (your translation so far) | `The prior art...` |
| `{{SELECTION}}` | Text currently **selected** in the editor | `vloerbekledingen` |

{% hint style="info" %}
**Segment vs selection:** `{{SOURCE_SEGMENT}}` and `{{TARGET_SEGMENT}}` always give the **entire active segment**. `{{SELECTION}}` gives only the **highlighted portion** — useful for term lookups or focused questions. If nothing is selected, `{{SELECTION}}` is an empty string.
{% endhint %}

### Project variables

| Variable | Replaced with |
|----------|---------------|
| `{{PROJECT_NAME}}` | Trados project name (e.g. `EP3456789_NL_EN`) |
| `{{DOCUMENT_NAME}}` | Active file name (e.g. `EP3456789A1.docx`) |
| `{{SURROUNDING_SEGMENTS}}` | N source segments before and after the active segment, with their actual Trados segment numbers and the active segment marked `← ACTIVE` |
| `{{PROJECT}}` | All source segments in the active document, numbered with their actual Trados segment numbers |

**`{{SURROUNDING_SEGMENTS}}` example output** (with N = 2):

```
[11] Voorgaande zin.
[12] Nog een voorgaande zin.
[13 ← ACTIVE] De stand der techniek op het gebied van vloerbekledingen...
[14] Volgende zin.
[15] Nog een volgende zin.
```

**`{{PROJECT}}` example output** (single-file project):

```
[1] De uitvinding heeft betrekking op een werkwijze...
[2] De stand der techniek op het gebied van vloerbekledingen...
[3] ...
```

In a **multi-file project**, a file header is inserted at each boundary (because Trados restarts segment numbering per file):

```
=== File 1 ===
[1] Conclusie 1 omvat...
[2] Conclusie 2 omvat...

=== File 2 ===
[1] De beschrijving begint hier...
```

{% hint style="warning" %}
`{{PROJECT}}` sends all source segments to the AI. For a typical 10,000-word patent this costs roughly **4–5 cents** per call with a Sonnet-class model — negligible for important work, but avoid using it in high-frequency prompts. The number of surrounding segments for `{{SURROUNDING_SEGMENTS}}` is configured in **Settings → AI Settings → Surrounding segments** (default: 5).
{% endhint %}

### Example: explain a selected term

Select a word in the source segment, press `Ctrl+Q`, and choose a prompt like this:

```
The user is translating from {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}}.
The selected term is: {{SELECTION}}

Explain what this term means and suggest the best {{TARGET_LANGUAGE}} equivalent,
considering the full segment context below:

{{SOURCE_SEGMENT}}
```

### Example: assess the current translation

```
Source ({{SOURCE_LANGUAGE}}):
{{SOURCE_SEGMENT}}

My translation ({{TARGET_LANGUAGE}}):
{{TARGET_SEGMENT}}

Assess how I translated the current segment. Point out any inaccuracies,
awkward phrasing, or terminology issues, and suggest improvements.
```

### Example: translate a selected term using surrounding context

Uses `{{SELECTION}}` together with `{{SURROUNDING_SEGMENTS}}` so the AI sees the passage
around the active segment, not just the active segment itself:

```
I am translating a {{SOURCE_LANGUAGE}} patent into {{TARGET_LANGUAGE}}.

The selected term is: {{SELECTION}}

Here is the passage surrounding the active segment for context:

{{SURROUNDING_SEGMENTS}}

Suggest the best {{TARGET_LANGUAGE}} translation for "{{SELECTION}}" given the
surrounding context. Give a brief explanation of your reasoning.
```

### Example: full-document term query

Uses `{{PROJECT}}` to give the AI the complete source text. Reserve this for high-stakes
queries where full document context matters, such as a key term that appears in multiple
places with different nuances:

```
I am translating a {{SOURCE_LANGUAGE}} patent ({{DOCUMENT_NAME}}) into {{TARGET_LANGUAGE}}.
Project: {{PROJECT_NAME}}

Here is the complete source text, segment by segment:

{{PROJECT}}

Throughout this document, what is the most accurate and consistent {{TARGET_LANGUAGE}}
translation for "{{SELECTION}}"? Consider all occurrences in context and note any
variation in meaning between them.
```

### Example: check specific segments by number

After using `{{PROJECT}}` the AI knows the segment numbers, so you can follow up in the
chat — or build a prompt that asks about specific segments from the start:

```
I am translating a {{SOURCE_LANGUAGE}} patent into {{TARGET_LANGUAGE}}.

Here is the source document:

{{PROJECT}}

My translations so far:
- Segment 1: [paste your translation here]
- Segment 4: [paste your translation here]

Do you think these translations are accurate and consistent with the terminology
used elsewhere in the document?
```

The plugin fills in all variables and sends the expanded prompt straight to the AI.

## Setting up QuickLauncher prompts

Set `category: QuickLauncher` in the YAML frontmatter, or place the file in a folder called `QuickLauncher` inside your `prompt_library`. See [Prompts → Marking a prompt as a QuickLauncher shortcut](settings/prompts.md#marking-a-prompt-as-a-quicklauncher-shortcut) for full details.

## Shared with Supervertaler Workbench

QuickLauncher prompts live in the shared `prompt_library` folder used by both Supervertaler for Trados and Supervertaler Workbench. Any prompt you create in one application is immediately available in the other.

---

## See Also

- [Prompts](settings/prompts.md)
- [Supervertaler Assistant](ai-assistant.md)
- [Keyboard Shortcuts](keyboard-shortcuts.md)
