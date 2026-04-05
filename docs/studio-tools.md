# Studio Tools

{% hint style="info" %}
You are viewing help for **Supervertaler for Trados** – the Trados Studio plugin. Looking for help with the standalone app? Visit [Supervertaler Workbench help](https://help.supervertaler.com).
{% endhint %}

Studio Tools lets you query your Trados Studio installation using natural language in the Supervertaler Assistant chat. Instead of navigating through menus and dialogs, you can simply ask the assistant about your projects, translation memories, termbases, and project templates -- and it will look up the answer for you.

{% hint style="success" %}
Studio Tools requires **Claude** as your AI provider. Other providers (OpenAI, Gemini, etc.) do not support this feature and will work as before -- plain chat without Trados queries.
{% endhint %}

## How It Works

When you send a message in the Supervertaler Assistant, Claude automatically decides whether it needs to query Trados Studio to answer your question. If it does, it calls the appropriate tool behind the scenes, reads the result, and presents the information in a clear format.

You do not need to use any special syntax or commands. Just ask your question naturally.

While a tool is running, the thinking indicator shows what is happening -- for example, "Checking Trados projects..." or "Searching translation memory...".

## Available Tools

| Tool | What It Does |
| ---- | ------------ |
| **List Projects** | Lists all projects registered in Trados Studio with their name, status, and creation date. Supports filtering by status (in progress, completed, archived) |
| **Get Project Details** | Shows detailed information about a specific project, including source and target languages, files, and folder path |
| **Project Statistics** | Shows word count and analysis statistics for a project, broken down by match category (perfect, context, exact, fuzzy, new, repetitions) |
| **File Status** | Shows the confirmation status of all files in a project -- how many segments are not started, draft, translated, approved, or signed off |
| **Project Termbases** | Lists termbases attached to a project, with their file paths, enabled/disabled state, and language index mappings |
| **TM Info** | Shows detailed information about a specific translation memory, including language pair, segment count, file size, and creation date |
| **Search TM** | Searches a translation memory for segments containing specific text. Returns matching source/target pairs so you can see how something was translated before |
| **List TMs** | Lists all translation memories found in the Trados Studio TM folders |
| **List Project Templates** | Lists all project templates available in Trados Studio |

## Example Questions

Here are some things you can try asking in the Assistant chat:

### Projects

* "What projects do I have in Trados Studio?"
* "Show me all my in-progress projects"
* "How many projects do I have?"
* "Tell me about the Client Alpha project"
* "What languages does the Client Alpha project use?"
* "What files are in my latest project?"
* "Do I have any completed projects?"
* "Which project was created most recently?"

### Project Statistics and Progress

* "What are the word counts for the Client Alpha project?"
* "Show me the analysis statistics for my project"
* "How many new words are in the Client Alpha project?"
* "What is the fuzzy match breakdown for this project?"
* "What is the translation status of the files in Client Alpha?"
* "How many segments are translated vs. not started?"
* "Which files still need work?"
* "Are any files fully approved?"

### Termbases

* "What termbases are attached to the Client Alpha project?"
* "Show me the terminology resources for this project"
* "Which termbases are enabled?"

### Translation Memories

* "What translation memories do I have?"
* "List my TMs"
* "Tell me about the English-Dutch TM"
* "How many segments are in my main TM?"
* "How big is my TM?"

### TM Search

* "Search my English-Dutch TM for 'compliance'"
* "How was 'annual report' translated before?"
* "Find segments containing 'data protection' in the TM"
* "Look up 'user interface' in my TM"

### Combined Questions

You can combine Studio Tools queries with the assistant's regular translation capabilities:

* "What projects am I working on? And can you also translate this segment?"
* "List my projects, then explain the terminology in the current segment"
* "Search the TM for 'privacy policy' and suggest a translation for the current segment"

The assistant handles the tool calls first, then continues with the rest of your question seamlessly.

## Technical Details

Studio Tools reads data directly from your local Trados Studio installation. Specifically:

* **Projects** are read from the `projects.xml` file in your Documents folder (e.g., `Documents\Studio 2024\Projects\projects.xml`). Project details, statistics, and file status are read from the individual `.sdlproj` files.
* **Termbases** are read from the termbase configuration stored in each `.sdlproj` file.
* **Translation memories** are found by scanning the `Translation Memories` folder and project folders. TM metadata and search use direct SQLite read-only access to the `.sdltm` files.
* **Project templates** are found in the `Project Templates` folder.

No data is sent to external services other than the AI provider. The tool results are passed to Claude as part of the conversation so it can format and present them to you.

{% hint style="info" %}
Studio Tools currently provides **read-only** access to your Trados data. It cannot create, modify, or delete projects, TMs, or templates.
{% endhint %}

## See Also

* [Supervertaler Assistant](ai-assistant.md) -- The chat interface where Studio Tools is available
* [AI Settings](settings/ai-settings.md) -- Configure your AI provider (must be set to Claude for tool use)
