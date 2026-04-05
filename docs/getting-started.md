{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

This page walks you through the first-time setup so you can start using TermLens terminology and AI translation inside Trados Studio.

{% hint style="success" %}
**Prefer to watch?** The [Getting Started screencast](https://www.youtube.com/watch?v=bOIwMAoP7xc) (16 min) covers everything on this page and more – TermLens, prompt generation, AI translation, the Chat window, and purchasing.
{% endhint %}

## First-Time Setup

### 1. Open Settings

Click the **gear icon** in the TermLens panel header to open the settings dialogue.

### 2. Configure Termbases (TermLens tab)

On the **TermLens** tab:

1. Click **Browse** to select an existing Supervertaler termbase (`.db` file)
2. Or click **New** to create a new empty termbase

You can add multiple termbases. Designate one as the **Project termbase** to give its terms higher priority (shown in pink).

{% hint style="info" %}
Supervertaler for Trados uses the same `.db` termbase format as Supervertaler Workbench. Any termbase created in either tool works in both. On Windows, both tools can point to the same `.db` file in a shared data folder. On a Mac running Trados via Parallels, the two products use separate filesystems – see [Running on a Mac](installation.md#running-on-a-mac-parallels) for details.
{% endhint %}

### 3. Configure AI (AI Settings tab)

On the **AI Settings** tab:

1. Select a **provider** (OpenAI, Anthropic, Google, OpenRouter, Ollama, or others)
2. Enter your **API key** for the selected provider
3. Choose a **model**

{% hint style="info" %}
**Don't have an API key yet?** You can skip this step entirely and use **[Clipboard Mode](clipboard-mode.md)** instead. Clipboard Mode lets you translate and proofread using any web-based AI you already have access to – ChatGPT, Claude, Gemini, or any other LLM chat interface. No API key required. It is the fastest way to start using AI translation in Supervertaler for Trados.
{% endhint %}

### 4. Click OK

Settings are saved and applied immediately.

## Try It Out

### TermLens

1. Open a project in the Trados **Editor** view
2. Navigate to any segment – TermLens automatically displays term matches for the source text
3. Click a term translation to insert it into the target, or press **Alt+1** through **Alt+9**

### Clipboard Mode (no API key needed)

1. Open the **Supervertaler Assistant** panel and switch to the **Batch Operations** tab
2. Tick the **Clipboard Mode** checkbox
3. Click **Copy to Clipboard** – a ready-to-use prompt with your segments, terminology, and instructions is copied
4. Paste it into any web-based AI (ChatGPT, Claude, Gemini, etc.) and send it
5. Copy the AI's response and click **Paste from Clipboard** – the translations are written back into Trados

See [Clipboard Mode](clipboard-mode.md) for the full walkthrough.

### AI Translate (API key required)

1. Place the cursor in a segment
2. Press **Ctrl+T** to translate the active segment with AI
3. The AI translation appears in the target cell

### Supervertaler Assistant

1. Open the **Supervertaler Assistant** panel (View > Supervertaler Assistant)
2. Switch to the **Chat** tab
3. Type a question about the current segment and press **Enter**
4. The assistant responds with context from your terminology and TM matches

## Quick Links

| Feature | Page |
|---------|------|
| TermLens terminology display | [TermLens](termlens.md) |
| AI via clipboard (no API key) | [Clipboard Mode](clipboard-mode.md) |
| AI chat interface | [Supervertaler Assistant](ai-assistant.md) |
| Bulk AI translation | [Batch Translate](batch-translate.md) |
| All shortcuts | [Keyboard Shortcuts](keyboard-shortcuts.md) |

---

## See Also

- [Installation](installation.md)
- [Clipboard Mode](clipboard-mode.md)
- [TermLens](termlens.md)
- [Supervertaler Assistant](ai-assistant.md)
