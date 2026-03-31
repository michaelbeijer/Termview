using System.Collections.Generic;

namespace Supervertaler.Trados.Models
{
    /// <summary>
    /// Represents a prompt template loaded from the shared prompt library.
    /// Stored as .md files (Markdown + YAML frontmatter). Legacy .svprompt files also supported.
    /// </summary>
    public class PromptTemplate
    {
        /// <summary>Display name (from YAML 'name:' field or filename).</summary>
        public string Name { get; set; } = "";

        /// <summary>One-line description (from YAML 'description:' field).</summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Category (from YAML 'category:' field or folder name on disk).
        /// Common values: "Translate", "Proofread", "QuickLauncher", "QuickLauncher/Default".
        /// Legacy values "quickmenu_prompts" / "domain:" are normalised on load.
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>The actual prompt text (everything after the YAML frontmatter).</summary>
        public string Content { get; set; } = "";

        /// <summary>Full filesystem path to the prompt file (.md or legacy .svprompt).</summary>
        public string FilePath { get; set; } = "";

        /// <summary>
        /// Document type (from YAML 'type:' field). Default "prompt".
        /// Used to identify Supervertaler document types in plain .md files.
        /// </summary>
        public string Type { get; set; } = "prompt";

        /// <summary>
        /// Relative path from the prompts root directory.
        /// Used as the stable identifier for settings persistence.
        /// </summary>
        public string RelativePath { get; set; } = "";

        /// <summary>True if this prompt was shipped with the plugin (can be restored if deleted).</summary>
        public bool IsDefault { get; set; }

        /// <summary>True if this prompt is read-only (e.g. from a shared folder).</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// True when the prompt should appear in the QuickLauncher right-click menu.
        /// Set by YAML 'category: QuickLauncher', or by placing the file in a folder named 'QuickLauncher'.
        /// </summary>
        public bool IsQuickLauncher { get; set; }

        /// <summary>
        /// Optional short label shown in the QuickLauncher menu (from YAML 'quicklauncher_label:').
        /// Falls back to Name if empty.
        /// </summary>
        public string QuickLauncherLabel { get; set; } = "";

        /// <summary>
        /// Target application for this prompt (from YAML 'app:' field).
        /// "workbench" = Supervertaler Workbench only, "trados" = Trados plugin only,
        /// "both" = shared between both (default).
        /// </summary>
        public string App { get; set; } = "both";

        /// <summary>
        /// Sort order within a folder (from YAML 'sort_order:' field).
        /// Lower values appear first. Default 100 for unset (sorts after explicit values).
        /// </summary>
        public int SortOrder { get; set; } = 100;

        /// <summary>
        /// When true, the prompt is hidden from the QuickLauncher right-click menu
        /// but still visible in the Prompt Manager tree (shown with a "(hidden)" suffix).
        /// From YAML 'hidden:' field.
        /// </summary>
        public bool HiddenFromMenu { get; set; }

        /// <summary>The label to display in the QuickLauncher menu (QuickLauncherLabel if set, else Name).</summary>
        public string MenuLabel => string.IsNullOrWhiteSpace(QuickLauncherLabel) ? Name : QuickLauncherLabel;

        /// <summary>
        /// True when this template is a local text transform (type: transform)
        /// rather than an AI prompt. Transforms apply find/replace rules directly
        /// to the target segment without calling an AI provider.
        /// </summary>
        public bool IsTransform => "transform".Equals(Type, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Find/replace pairs for text transforms (type: transform).
        /// Each entry has a Find string and a Replace string.
        /// Parsed from YAML frontmatter 'replacements:' block.
        /// </summary>
        public List<TextReplacement> Replacements { get; set; } = new List<TextReplacement>();

        public override string ToString() => Name;
    }

    /// <summary>
    /// A single find/replace rule used by text transform prompts.
    /// </summary>
    public class TextReplacement
    {
        public string Find { get; set; } = "";
        public string Replace { get; set; } = "";
    }
}
