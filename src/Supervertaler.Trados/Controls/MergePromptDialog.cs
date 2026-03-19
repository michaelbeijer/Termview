using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Supervertaler.Trados.Core;

namespace Supervertaler.Trados.Controls
{
    /// <summary>
    /// Dialog that prompts the user when adding a term whose source or target
    /// already exists in the termbase. Offers to merge as a synonym (quick or
    /// via the term entry editor), keep both as separate entries, or cancel.
    ///
    /// DialogResult.Yes    = Add as Synonym (quick — insert and done)
    /// DialogResult.Retry  = Add as Synonym &amp; open Term Entry Editor for review
    /// DialogResult.No     = Keep Both (create a separate entry)
    /// DialogResult.Cancel = Cancel (abort the add operation)
    /// </summary>
    public class MergePromptDialog : Form
    {
        private readonly List<MergeMatch> _matches;
        private readonly string _newSource;
        private readonly string _newTarget;
        private readonly bool _isInverted;

        /// <summary>
        /// The list of merge matches the user is responding to.
        /// </summary>
        public List<MergeMatch> Matches => _matches;

        /// <summary>
        /// Creates the merge prompt dialog.
        /// </summary>
        /// <param name="matches">Merge candidates (source/target in DB direction).</param>
        /// <param name="newSource">The new term's source (in DB direction).</param>
        /// <param name="newTarget">The new term's target (in DB direction).</param>
        /// <param name="isInverted">
        /// True when the project's language direction is the inverse of the
        /// termbase's language direction (e.g. NL→EN project using an EN→NL
        /// termbase). When true, "source" and "target" labels are swapped in
        /// the UI so the dialog matches the translator's perspective.
        /// </param>
        public MergePromptDialog(
            List<MergeMatch> matches, string newSource, string newTarget,
            bool isInverted = false)
        {
            _matches = matches ?? new List<MergeMatch>();
            _newSource = newSource ?? "";
            _newTarget = newTarget ?? "";
            _isInverted = isInverted;

            BuildUI();
        }

        private void BuildUI()
        {
            Text = "Similar Term Found";
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            // All controls are placed directly on the form with absolute
            // positioning — no Dock panels, which misbehave inside Trados's
            // WPF-hosted plugin environment.

            int margin = 20;
            int contentWidth = 460;
            int y = 16;

            // When the termbase is inverted, swap source/target for display
            // so the dialog matches the translator's project direction.
            var displayNewSource = _isInverted ? _newTarget : _newSource;
            var displayNewTarget = _isInverted ? _newSource : _newTarget;

            // --- "You are adding:" label ---
            var addingLabel = new Label
            {
                Text = "You are adding:",
                AutoSize = true,
                Location = new Point(margin, y)
            };
            Controls.Add(addingLabel);
            y += 20;

            // --- New term (bold) ---
            var newTermLabel = new Label
            {
                Text = $"  {displayNewSource}  \u2192  {displayNewTarget}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(margin, y),
                MaximumSize = new Size(contentWidth, 0)
            };
            Controls.Add(newTermLabel);
            y += Math.Max(newTermLabel.PreferredHeight, 20) + 6;

            // --- Separator ---
            var sep1 = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 1,
                Location = new Point(margin, y),
                Width = contentWidth
            };
            Controls.Add(sep1);
            y += 10;

            // --- Match description ---
            var match = _matches[0];
            string matchDescription;
            string synonymAction;

            // Display the existing match in project direction
            var displayMatchSource = _isInverted ? match.TargetTerm : match.SourceTerm;
            var displayMatchTarget = _isInverted ? match.SourceTerm : match.TargetTerm;

            // When inverted, the DB match types are reversed from the project perspective:
            // a DB "source" match means the project-target matched, and vice versa.
            var effectiveMatchType = _isInverted
                ? (match.MatchType == "source" ? "target" : "source")
                : match.MatchType;

            if (effectiveMatchType == "source")
            {
                matchDescription = $"The source term \u201c{displayMatchSource}\u201d already exists " +
                    $"with target \u201c{displayMatchTarget}\u201d";
                synonymAction = $"Add \u201c{displayNewTarget}\u201d as a target synonym " +
                    $"to the existing entry?";
            }
            else
            {
                matchDescription = $"The target term \u201c{displayMatchTarget}\u201d already exists " +
                    $"with source \u201c{displayMatchSource}\u201d";
                synonymAction = $"Add \u201c{displayNewSource}\u201d as a source synonym " +
                    $"to the existing entry?";
            }

            // Termbase name
            matchDescription += $"\nin termbase \u201c{match.TermbaseName}\u201d.";

            // If there are matches in other termbases too, add a note
            int additionalCount = _matches.Count - 1;
            if (additionalCount > 0)
            {
                matchDescription += $"\n(and {additionalCount} more " +
                    $"{(additionalCount == 1 ? "match" : "matches")} in other termbases)";
            }

            var matchLabel = new Label
            {
                Text = matchDescription,
                Location = new Point(margin, y),
                Size = new Size(contentWidth, 60)
            };
            Controls.Add(matchLabel);
            y += 68;

            // --- Action question ---
            var actionLabel = new Label
            {
                Text = synonymAction,
                Location = new Point(margin, y),
                Size = new Size(contentWidth, 36),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            Controls.Add(actionLabel);
            y += 44;

            // --- Separator ---
            var sep2 = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 1,
                Location = new Point(0, y),
                Width = margin + contentWidth + margin
            };
            Controls.Add(sep2);
            y += 12;

            // --- Buttons: right-aligned ---
            int btnH = 30;
            int btnY = y;
            int rightEdge = margin + contentWidth;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(80, btnH),
                DialogResult = DialogResult.Cancel,
                Location = new Point(rightEdge - 80, btnY)
            };

            var btnKeepBoth = new Button
            {
                Text = "Keep Both",
                Size = new Size(90, btnH),
                DialogResult = DialogResult.No,
                Location = new Point(rightEdge - 80 - 8 - 90, btnY)
            };

            var btnEditReview = new Button
            {
                Text = "Add && Edit\u2026",
                Size = new Size(115, btnH),
                Location = new Point(rightEdge - 80 - 8 - 90 - 8 - 115, btnY)
            };
            btnEditReview.Click += (s, e) =>
            {
                DialogResult = DialogResult.Retry;
                Close();
            };

            var btnMerge = new Button
            {
                Text = "Add as Synonym",
                Size = new Size(140, btnH),
                DialogResult = DialogResult.Yes,
                Location = new Point(rightEdge - 80 - 8 - 90 - 8 - 115 - 8 - 140, btnY)
            };

            var tips = new ToolTip();
            tips.SetToolTip(btnMerge, "Quickly add the term as a synonym to the existing entry");
            tips.SetToolTip(btnEditReview, "Add as synonym and open the Term Entry Editor for review");
            tips.SetToolTip(btnKeepBoth, "Create a separate termbase entry instead of merging");
            tips.SetToolTip(btnCancel, "Cancel without adding the term");

            Controls.Add(btnMerge);
            Controls.Add(btnEditReview);
            Controls.Add(btnKeepBoth);
            Controls.Add(btnCancel);

            y += btnH + 12;

            // Size the form to exactly fit the content.
            ClientSize = new Size(margin + contentWidth + margin, y);

            AcceptButton = btnMerge;
            CancelButton = btnCancel;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
