{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** — the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

Text transforms are a special type of QuickLauncher prompt that performs local find-and-replace operations on the active target segment — instantly, without calling an AI provider.

## When to use text transforms

Text transforms are useful for cleaning up invisible or problematic characters in your translations. For example:

- **InDesign (IDML) forced line breaks** — InDesign uses invisible Unicode LINE SEPARATOR (U+2028) characters as forced line breaks (Shift+Enter). These are invisible in Trados but can cause problems in your translations.
- **Zero-width spaces and joiners** — invisible characters from PDF or web sources
- **Normalising quotes or dashes** — replacing curly quotes with straight quotes, or em dashes with en dashes

## How it works

1. Navigate to the segment you want to clean
2. Press **Ctrl+Q** (or right-click → QuickLauncher)
3. Open the **Text operations** folder
4. Click **Strip U+2028** (or another transform)

The transform runs instantly. A dialog confirms how many replacements were made, and the cleaned text is copied to your clipboard.

{% hint style="info" %}
Text transforms modify the **target segment** only. The source segment is never changed.
{% endhint %}

## Built-in transforms

Supervertaler ships with one built-in text transform:

### Strip U+2028

Removes invisible Unicode LINE SEPARATOR (U+2028) and PARAGRAPH SEPARATOR (U+2029) characters from the target segment, replacing them with spaces. Consecutive spaces are collapsed to a single space.

These characters are commonly inserted by InDesign (IDML) as forced line breaks (Shift+Enter). They are invisible in the Trados editor but can corrupt translations — the AI may produce spurious line breaks, or the characters may cause formatting issues in the final document.

## Creating your own transforms

Text transforms are stored as `.md` files in your prompt library, just like regular prompts. The only difference is the YAML frontmatter has `type: transform` instead of `type: prompt`, and the content body contains find/replace rules instead of a prompt.

### Step by step

1. Open **Settings → Prompts**
2. Click **New**
3. Set the **Category** to `QuickLauncher/Text operations` (or any QuickLauncher subfolder)
4. In the YAML frontmatter, change `type: prompt` to `type: transform`
5. In the content body, write your find/replace rules

### Rule format

Each rule is a `find:` / `replace:` pair. Blank lines between rules are optional but improve readability. Lines starting with `#` are comments.

```
# Replace curly quotes with straight quotes
find: "\u201C"
replace: "\u0022"

find: "\u201D"
replace: "\u0022"
```

### Unicode escapes

Use `\uXXXX` to specify Unicode characters by their code point. This is essential for invisible characters that cannot be typed or seen in a text editor.

| Escape | Character | Description |
|--------|-----------|-------------|
| `\u2028` | (invisible) | LINE SEPARATOR — InDesign forced line break |
| `\u2029` | (invisible) | PARAGRAPH SEPARATOR |
| `\u200B` | (invisible) | ZERO WIDTH SPACE |
| `\u200C` | (invisible) | ZERO WIDTH NON-JOINER |
| `\u200D` | (invisible) | ZERO WIDTH JOINER |
| `\uFEFF` | (invisible) | BYTE ORDER MARK (BOM) |
| `\u00A0` | (invisible) | NON-BREAKING SPACE |
| `\u201C` | " | LEFT DOUBLE QUOTATION MARK |
| `\u201D` | " | RIGHT DOUBLE QUOTATION MARK |

### Example: Strip U+2028 (built-in)

```yaml
---
type: transform
name: "Strip U+2028"
description: "Removes invisible Unicode LINE SEPARATOR and PARAGRAPH SEPARATOR"
category: "QuickLauncher/Text operations"
default: true
---

# Strip invisible Unicode line/paragraph separators.
# These are commonly inserted by InDesign (IDML) as forced line breaks.

find: "\u2028"
replace: " "

find: "\u2029"
replace: " "
```

### Example: Normalise non-breaking spaces

```yaml
---
type: transform
name: "Fix non-breaking spaces"
description: "Replaces non-breaking spaces with regular spaces"
category: "QuickLauncher/Text operations"
---

# Replace non-breaking spaces (U+00A0) with regular spaces
find: "\u00A0"
replace: " "
```

## Keyboard shortcuts

Text transforms appear in the QuickLauncher menu alongside regular prompts. You can assign them to keyboard slots (Ctrl+Alt+1 through Ctrl+Alt+0) for instant access:

1. Open **Settings → Prompts**
2. Select the transform in the tree
3. Choose a **Shortcut** slot from the dropdown at the bottom

## Clipboard

After a transform runs, the cleaned target text is automatically copied to your clipboard. This is useful if you need to paste the cleaned text elsewhere — for example, into a text editor or a QA tool.

## Technical notes

- Transforms use Trados's `ProcessSegmentPair` API to commit changes, the same mechanism used by Batch Translate. This ensures all formatting tags (bold, italic, etc.) are preserved.
- After replacements, consecutive spaces are collapsed to a single space to prevent double spaces where an invisible character sat next to an existing space.
- Transforms do not appear in the AI Assistant chat — they run locally and show a brief confirmation dialog.

---

## See Also

- [QuickLauncher](quicklauncher.md)
- [Prompts](settings/prompts.md)
- [Keyboard Shortcuts](keyboard-shortcuts.md)
