{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** — the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

Prompts are stored as `.md` files (Markdown with YAML frontmatter). This is the same format used by Supervertaler Workbench, so prompts are automatically shared between both applications via the shared `prompt_library` folder. Legacy `.svprompt` files are still loaded for backward compatibility.

```yaml
---
type: prompt
name: My Patent Prompt
description: Patent and IP translation with strict terminology rules
category: Translate
---

You are an expert {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}} patent translator...
```

| YAML field            | Description                                                                      |
| --------------------- | -------------------------------------------------------------------------------- |
| `type`                | Document type – always `prompt` for prompt files                                 |
| `name`                | Display name shown in the prompt selector                                        |
| `description`         | Optional summary                                                                 |
| `category`            | `Translate`, `Proofread`, or `QuickLauncher` — controls where the prompt appears |
| `quicklauncher_label` | Short label for the QuickLauncher menu (optional, falls back to `name`)          |
| `default`             | `true` for shipped prompts (managed by the plugin)                               |
| `sort_order`          | Numeric order within folder (lower values first). Set automatically by the ▲/▼ buttons. |

{% hint style="info" %}
Older prompts using the `domain` key instead of `category` are still supported for backward compatibility.
{% endhint %}

### System prompt

The plugin automatically prepends a system prompt to every AI call. This system prompt includes language pair information, termbase terms (based on your [AI Context settings](../ai-settings.md)), and TM matches when enabled. The content you write in a prompt `.md` file is the **user prompt** — it is sent after the system prompt.

### Creating and editing prompts

#### New prompt

1. Click **New** in the Prompts tab
2. Fill in Name, Description, Category, and Content
3. Click **Save**

#### Edit a prompt

1. Select a prompt in the list
2. Click **Edit**
3. Modify as needed and click **Save**

#### Inserting variables

While editing prompt content, press **Ctrl+,** to open the variable picker menu. This lists all available variables with a short description. Select a variable to insert it at the cursor position. If text is selected in the editor, it is replaced by the inserted variable.

{% hint style="info" %}
**Ctrl+,** mirrors the variable insertion shortcut used in the Trados Studio editor.
{% endhint %}

#### Delete a prompt

1. Select a custom prompt
2. Click **Delete** and confirm

Built-in prompts cannot be deleted. Click **Restore** to recreate any built-in prompts you have removed.
