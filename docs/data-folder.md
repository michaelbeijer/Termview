# User Data Folder

Supervertaler for Trados shares a user data folder with [Supervertaler Workbench](https://supervertaler.com/workbench). This allows both programs to access the same termbases, translation memories, and prompt library without duplicating files.

## Folder Location

By default, the shared data folder is located at:

```
C:\Users\<YourName>\Supervertaler\
```

You can choose a different location during first-run setup. Both programs read the configured path from the same pointer file at `%APPDATA%\Supervertaler\config.json`.

## Folder Structure

```
Supervertaler/
│
├── prompt_library/              Shared
│   ├── domain_expertise/
│   ├── project_prompts/
│   └── style_guides/
│
├── resources/                   Shared
│   ├── supervertaler.db
│   ├── termbases/
│   ├── tms/
│   ├── non_translatables/
│   └── segmentation_rules/
│
├── workbench/                   Supervertaler Workbench only
│   ├── settings/
│   │   ├── settings.json
│   │   ├── themes.json
│   │   ├── shortcuts.json
│   │   └── ...
│   ├── dictionaries/
│   ├── projects/
│   ├── ai_assistant/
│   ├── voice_scripts/
│   └── web_cache/
│
└── trados/                      Supervertaler for Trados only
    ├── settings/
    │   ├── settings.json
    │   ├── license.json
    │   └── chat_history.json
    └── projects/
```

### Shared resources

The **prompt library** and **resources** folders are shared between both programs. Prompts you create or edit in one program are immediately available in the other. The SQLite database (`supervertaler.db`) holds your termbases and translation memories — Workbench has full read-write access, while the Trados plugin reads from it.

### Program-specific folders

Each program stores its own settings, projects, and runtime data in a dedicated subfolder (`workbench/` or `trados/`). This keeps configuration separate so the two programs never interfere with each other.

## Automatic Migration

If you are updating from an older version, both programs will automatically reorganise the folder on their next startup. No manual action is required — your settings, license, and data are preserved.
