{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

### Marking a prompt as a QuickLauncher shortcut

To make a custom prompt appear in the QuickLauncher right-click menu (`Ctrl+Q`), set `category: QuickLauncher` in the YAML frontmatter:

```yaml
---
name: Explain selected term
description: Explains the selected term in translation context
category: QuickLauncher
quicklauncher_label: Explain term
---

Your prompt content here...
```

| Field                     | Description                                                              |
| ------------------------- | ------------------------------------------------------------------------ |
| `category: QuickLauncher` | Marks this prompt as a QuickLauncher item                                |
| `quicklauncher_label`     | Optional short label shown in the menu – falls back to `name` if omitted |

You can also organise QuickLauncher prompts by placing them in a folder called `QuickLauncher` inside your `prompt_library` folder. Any prompt in that folder is automatically treated as a QuickLauncher prompt.

{% hint style="info" %}
QuickLauncher prompts are shared with Supervertaler Workbench via the shared prompt library folder.
{% endhint %}

### Keyboard shortcuts for QuickLauncher prompts

You can assign keyboard shortcuts (Ctrl+Alt+1 through Ctrl+Alt+0) to individual QuickLauncher prompts for instant access without opening the Ctrl+Q menu.

1. Open **Settings → Prompts**
2. Select a QuickLauncher prompt in the tree
3. In the detail pane on the right, use the **Shortcut** dropdown to assign a slot
4. Click **OK** to save

Each shortcut can only be assigned to one prompt. If you assign a shortcut that is already in use, it is automatically cleared from the other prompt.

Assigned shortcuts are shown next to prompt names in the Ctrl+Q menu and in the Trados keyboard shortcuts settings (File → Options → Keyboard Shortcuts → Supervertaler for Trados).

### Reordering prompts

Use the **▲** and **▼** buttons in the toolbar to change the order of prompts within a folder. This is especially useful for QuickLauncher prompts, as the order in the tree determines the order in the Ctrl+Q menu.

The order is saved in each prompt's YAML frontmatter as a `sort_order` field.
