{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

The **Batch Operations** tab in the Supervertaler Assistant panel provides two AI-powered modes for processing multiple segments at once:

| Mode | Description |
|------|-------------|
| **[Batch Translate](batch-translate.md)** | Translate segments using AI with customisable prompts |
| **[AI Proofreader](ai-proofreader.md)** | Check translations for errors, inconsistencies, and style issues |

Switch between modes using the **Mode** dropdown at the top of the Batch Operations tab.

Both modes share the same prompt selector, provider/model configuration, and scope options. Prompts are filtered by mode – Translate prompts appear in Translate mode, Proofread prompts appear in Proofread mode. You can click the **provider/model label** to quickly switch AI models via a flyout menu – the same menu available in the Chat tab.

### Clipboard Mode

Both Translate and Proofread modes support **[Clipboard Mode](clipboard-mode.md)** – an alternative workflow that lets you use any web-based AI (ChatGPT, Claude, Gemini, etc.) without an API key. Tick the **Clipboard Mode** checkbox to switch from API-based processing to a manual copy/paste workflow. See [Clipboard Mode](clipboard-mode.md) for full details.

### AutoPrompt

The Batch Operations tab also includes an **[AutoPrompt](generate-prompt.md)** link that uses AI to create a comprehensive, domain-specific translation prompt based on your project's content, terminology, and TM data.

## See Also

* [Clipboard Mode](clipboard-mode.md)
* [AutoPrompt](generate-prompt.md)
* [Prompts](settings/prompts.md)
* [AI Settings](settings/ai-settings.md)
* [Keyboard Shortcuts](keyboard-shortcuts.md)
