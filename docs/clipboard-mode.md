{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

Clipboard Mode lets you translate or proofread segments using **any web-based AI** – ChatGPT, Claude, Gemini, DeepSeek, or any other LLM with a chat interface – without needing an API key. Instead of sending segments to an AI provider via API, Supervertaler builds a ready-to-use prompt and copies it to your clipboard. You paste it into the AI of your choice, copy the response, and paste it back.

{% hint style="success" %}
**No API key? No problem.** Clipboard Mode is the fastest way to start using AI translation in Supervertaler for Trados. If you already have access to a web-based AI chat – and most people do these days – you can start translating immediately after installing the plugin. No API keys, no provider configuration, no per-token billing. Just tick Clipboard Mode, copy, paste, and translate.
{% endhint %}

Clipboard Mode is also ideal if you want to use a model that is not available via API, if you prefer a pay-as-you-go chat subscription, or if you want to try different AI models before committing to a specific provider's API.

## How It Works

Clipboard Mode is available in both **Translate** and **Proofread** modes on the Batch Operations tab.

### Translating with Clipboard Mode

1. Open the **Supervertaler Assistant** panel and switch to the **Batch Operations** tab
2. Set the mode to **Translate**
3. Tick the **Clipboard Mode** checkbox
4. Choose a **scope** (Empty Segments Only, All Segments, etc.)
5. Optionally select a **prompt** to customise the translation instructions
6. Click **Copy to Clipboard**
7. Open your preferred web-based AI (ChatGPT, Claude, Gemini, etc.)
8. Paste the prompt into the chat and send it
9. Copy the AI's full response
10. Switch back to Trados and click **Paste from Clipboard**

The translations are written into the target segments automatically, with full tag reconstruction and validation.

### Proofreading with Clipboard Mode

1. Set the mode to **Proofread** and tick **Clipboard Mode**
2. Click **Copy to Clipboard** – the prompt includes both source and target text for each segment
3. Paste into your AI, copy the response, and click **Paste from Clipboard**

## What Gets Copied

When you click **Copy to Clipboard**, Supervertaler builds a comprehensive prompt that includes:

* **System instructions** – the same translation or proofreading instructions used by the API-based batch modes
* **Custom prompt** – your selected prompt from the Prompt Manager, if any
* **Terminology** – terms from your enabled termbases, including definitions and domains (when term metadata is enabled in AI Settings)
* **Document context** – source segments from the document (when enabled in AI Settings), so the AI understands the document type and domain
* **Numbered bilingual segments** – each segment is numbered and formatted with status annotations

This is not just a list of segments – it is a fully self-contained prompt ready to paste into any LLM chat window.

### Segment Format

Each segment is formatted as a numbered bilingual block:

```
Segment 1 [new]:
Dutch: Polyvision breidt de mogelijkheden uit.
English:

Segment 2 [fuzzy, 85%]:
Dutch: Nieuwe toepassingen in onderwijs.
English: New applications in education.

Segment 3 [translated, 100%]:
Dutch: Doelstelling lange termijn.
English: Long-term objective.
```

The per-segment labels use short language names (e.g. "Dutch", "English") to save tokens. The full language names with regional variants (e.g. "Dutch (Netherlands)", "English (United Kingdom)") are stated once in the system prompt at the top.

### Status Annotations

Each segment header includes a status annotation in square brackets:

| Status | Meaning |
|--------|---------|
| **[new]** | No target text – needs translation |
| **[fuzzy, N%]** | TM fuzzy match at N% – may need revision |
| **[translated, 100%]** | 100% TM match – likely correct |
| **[translated]** | Human-edited translation |
| **[machine translated]** | Machine translation output |
| **[draft]** | Has target text but origin is unclear |

These annotations help the AI understand the state of each segment and respond appropriately – for example, a fuzzy match may only need minor adjustments rather than a full retranslation.

## Tag Handling

Inline tags (bold, italic, hyperlinks, field codes, etc.) are serialised as numbered placeholders before being sent to the AI:

| Tag type | Placeholder |
|----------|-------------|
| Opening tag | `<t1>`, `<t2>`, etc. |
| Closing tag | `</t1>`, `</t2>`, etc. |
| Self-closing tag | `<t1/>`, `<t2/>`, etc. |

For example, a segment like "Click **here** for details" becomes:

```
Click <t1>here</t1> for details
```

The AI is instructed to preserve these placeholders exactly as they appear. When you paste the response back, Supervertaler reconstructs the original Trados tags from the placeholders – the same tag reconstruction pipeline used by API-based Batch Translate.

{% hint style="warning" %}
If a tag is missing or malformed in the AI's response, Supervertaler reports a warning but still writes the translation. Check the log for any tag validation messages.
{% endhint %}

## Choosing an AI Model

Any web-based LLM with a chat interface works with Clipboard Mode. Some recommendations:

* **Claude** (claude.ai) – excellent at following the bilingual format precisely and preserving tags
* **ChatGPT** (chatgpt.com) – widely available, works well with the structured format
* **Gemini** (gemini.google.com) – large context window, good for bigger batches
* **DeepSeek** (chat.deepseek.com) – strong multilingual capabilities

For best results, use the most capable model available in your subscription (e.g., Claude Opus, GPT-4o, Gemini Pro).

{% hint style="info" %}
Most web-based AI chat interfaces have a context limit that determines how many segments you can process at once. If you have a large number of segments, consider using a smaller scope (e.g., Filtered Segments) or processing in multiple rounds.
{% endhint %}

## Clipboard Mode vs API Mode

| | Clipboard Mode | API Mode |
|---|---|---|
| **API key required** | No | Yes |
| **Setup time** | None – works immediately | Requires provider account and API key |
| **Cost** | Included in your AI chat subscription | Pay-per-token via API |
| **Automation** | Manual copy/paste | Fully automatic |
| **Model choice** | Any web-based LLM | OpenAI, Anthropic, Google, Ollama |
| **Best for** | Getting started, quick jobs, trying new models | Large projects, automation, batch processing |

Both modes use the same prompts, terminology, document context, and tag handling – the only difference is how the text gets to and from the AI.

{% hint style="info" %}
Many users start with Clipboard Mode to explore AI translation with zero setup, then move to API Mode later for larger projects where full automation is more efficient. The two modes complement each other – you can switch between them at any time by ticking or unticking the Clipboard Mode checkbox.
{% endhint %}

## Tips

### Use the Best Model Available

Since you are not paying per token in Clipboard Mode, there is no cost difference between models. Use the most capable model your subscription offers.

### Check the Response Format

Before clicking Paste from Clipboard, glance at the AI's response to make sure it followed the numbered bilingual format. Most modern LLMs handle this correctly, but if the format is off, you can ask the AI to reformat its response.

### Combine with Terminology

Clipboard Mode includes your termbase terms in the prompt, just like API mode. Make sure your termbases are set up and enabled in AI Settings for the best results.

### Name Prompts After Your Projects

If you save a custom prompt with the same name as your Trados project (e.g. "HAYNESPRO" for a project called HAYNESPRO), the prompt dropdown will auto-select it whenever you open that project. This works for both Translate and Proofread modes and saves you from having to reselect the correct prompt each time.

### Process in Batches

For large documents, use the scope dropdown to work through segments in manageable batches – for example, use display filters to select a section at a time, then use the **Filtered Segments** scope.

---

## See Also

* [Batch Translate](batch-translate.md)
* [AI Proofreader](ai-proofreader.md)
* [Batch Operations](batch-operations.md)
* [AI Settings](settings/ai-settings.md)
* [Prompts](settings/prompts.md)
