{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

### Anatomy of a prompt

A good prompt has three parts:

1. **Role** – tells the AI who it is
2. **Task** – tells the AI what to do
3. **Constraints** – tells the AI what to avoid or preserve

Here is an annotated example for a Batch Translate prompt:

```
You are an expert {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}} patent translator.  ← Role + language variables

Translate the source segment provided. Return only the translated text –         ← Task
no commentary, no explanations, no repetition of the source.

Preserve all tag placeholders exactly as they appear (e.g. <t1>, <t2/>).        ← Constraints
Preserve numbers, units, and chemical formulas without conversion.
Use formal, technical register throughout.
```

### Example QuickLauncher prompt – explain a selected term

This prompt uses `{{SELECTION}}` to ask the AI to explain a selected term in context:

```
The user is translating a patent from {{SOURCE_LANGUAGE}} to {{TARGET_LANGUAGE}}.

The selected term is: {{SELECTION}}

Please explain what this term means in the context of patent translation,
suggest the standard {{TARGET_LANGUAGE}} equivalent, and note any regional
or register variations the translator should be aware of.
```

When the translator selects "werkwijze" in the source segment and triggers this prompt via QuickLauncher, the AI receives:

```
The user is translating a patent from Dutch (Belgium) to English (United States).

The selected term is: werkwijze

Please explain what this term means...
```

### Example QuickLauncher prompt – assess the current translation

This prompt uses `{{SOURCE_SEGMENT}}` and `{{TARGET_SEGMENT}}` to ask the AI to review the translation of the active segment:

```
Source ({{SOURCE_LANGUAGE}}):
{{SOURCE_SEGMENT}}

My translation ({{TARGET_LANGUAGE}}):
{{TARGET_SEGMENT}}

Assess how I translated the current segment. Point out any inaccuracies,
awkward phrasing, or terminology issues, and suggest improvements.
```

### Example QuickLauncher prompt – translate a selected term in context

```
Source segment ({{SOURCE_LANGUAGE}}):
{{SOURCE_SEGMENT}}

Current translation ({{TARGET_LANGUAGE}}):
{{TARGET_SEGMENT}}

The translator has selected this word or phrase: {{SELECTION}}

Suggest the best {{TARGET_LANGUAGE}} translation for "{{SELECTION}}"
given the full segment context above. Give a short explanation of your reasoning.
```

### Example QuickLauncher prompt – translate a term using surrounding passage

Uses `{{SURROUNDING_SEGMENTS}}` for a wider context window than just the active segment:

```
I am translating a {{SOURCE_LANGUAGE}} patent into {{TARGET_LANGUAGE}}.

The selected term is: {{SELECTION}}

Here is the passage surrounding the active segment:

{{SURROUNDING_SEGMENTS}}

Suggest the best {{TARGET_LANGUAGE}} translation for "{{SELECTION}}" given the
surrounding context. Briefly explain your reasoning.
```

### Example QuickLauncher prompt – full-document term consistency check

Uses `{{PROJECT}}` to give the AI the entire source document. Useful for checking whether a key term is used consistently, or for understanding a term's meaning across all its occurrences. Reserve this for important queries – see the token cost note in [Prompt Variables](prompt-variables.md).

```
I am translating a {{SOURCE_LANGUAGE}} patent ({{DOCUMENT_NAME}}) into {{TARGET_LANGUAGE}}.
Project: {{PROJECT_NAME}}

Here is the complete source text:

{{PROJECT}}

The selected term is: {{SELECTION}}

What is the most accurate and consistent {{TARGET_LANGUAGE}} translation for
"{{SELECTION}}" throughout this document? Note any variation in meaning between
occurrences and recommend which translation to use where.
```

### Example QuickLauncher prompt – check a segment against the full document

After sending `{{PROJECT}}`, the AI knows the segment numbers shown in Trados, so you can ask about specific segments by number in follow-up messages – or ask in the prompt itself:

```
I am translating a {{SOURCE_LANGUAGE}} patent into {{TARGET_LANGUAGE}}.

Here is the source document:

{{PROJECT}}

I am currently working on segment {{SOURCE_SEGMENT}} (shown as [{{SOURCE_SEGMENT}}]
above). My translation is:

{{TARGET_SEGMENT}}

Does this translation accurately reflect the source and maintain consistency with the
terminology used elsewhere in the document? Point out any issues.
```

### Tips for effective prompts

* **Be explicit about output format.** If you only want the translation, say "Return only the translated text." If you want an explanation, describe the expected structure.
* **Use language variables.** Hardcoding "Dutch to English" breaks the prompt when you switch projects. Always use `{{SOURCE_LANGUAGE}}` and `{{TARGET_LANGUAGE}}`.
* **Keep QuickLauncher prompts focused.** A narrow, specific task works better than a broad one – except when you deliberately need the full document context via `{{PROJECT}}`.
* **Use `{{SURROUNDING_SEGMENTS}}` instead of `{{SOURCE_SEGMENT}}` when context matters.** The surrounding passage often gives the AI enough context for a better answer at a fraction of the cost of `{{PROJECT}}`.
* **Use `{{PROJECT}}` sparingly.** It is best suited for high-stakes queries on short-to-medium documents – terminology consistency checks, key term decisions, or reviewing a handful of specific segments. Avoid it in prompts you run on every segment.
* **Segment numbers in `{{PROJECT}}` match the Trados editor.** After sending `{{PROJECT}}`, you can ask the AI about "segment 4" or "segment 12" and it will know exactly which segment you mean – the same number shown in the Trados grid.
* **Use `{{TM_MATCHES}}` to leverage existing translations.** When a segment has a high fuzzy match, the AI can use it as a starting point – especially useful for repetitive or formulaic content like patents and legal texts.
* **Batch Translate prompts receive one segment at a time.** You do not need to handle lists of segments or loop logic.
* **Proofread prompts receive multiple segment pairs.** The built-in proofreading prompt shows the expected input/output format – follow that structure if you write a custom one.
