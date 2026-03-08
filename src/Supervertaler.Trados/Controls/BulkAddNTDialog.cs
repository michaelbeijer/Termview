using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Supervertaler.Trados.Controls
{
    /// <summary>
    /// Simple dialog for pasting multiple non-translatable terms at once (one per line).
    /// Each non-empty line becomes an NT entry with source = target.
    /// </summary>
    public class BulkAddNTDialog : Form
    {
        private TextBox _txtTerms;
        private Label _lblCount;
        private Button _btnInsert;

        /// <summary>
        /// The parsed list of non-empty, trimmed term strings after OK is clicked.
        /// </summary>
        public List<string> Terms { get; private set; } = new List<string>();

        public BulkAddNTDialog()
        {
            Text = "Bulk Add Non-Translatable Terms";
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 360);
            BackColor = Color.White;

            int y = 16;

            // Instruction label
            Controls.Add(new Label
            {
                Text = "Paste terms below (one per line).\n" +
                       "Each term will be added as non-translatable (source = target).",
                Location = new Point(16, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            y += 40;

            // Multiline text box
            _txtTerms = new TextBox
            {
                Location = new Point(16, y),
                Width = ClientSize.Width - 32,
                Height = 200,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            _txtTerms.TextChanged += OnTextChanged;
            Controls.Add(_txtTerms);
            y += 210;

            // Count label
            _lblCount = new Label
            {
                Text = "0 terms to add",
                Location = new Point(16, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Controls.Add(_lblCount);

            // Separator
            Controls.Add(new Label
            {
                Location = new Point(16, ClientSize.Height - 50),
                Width = ClientSize.Width - 32,
                Height = 1,
                BorderStyle = BorderStyle.Fixed3D
            });

            // Insert button
            _btnInsert = new Button
            {
                Text = "Insert",
                DialogResult = DialogResult.None,
                Location = new Point(ClientSize.Width - 170, ClientSize.Height - 38),
                Width = 75,
                FlatStyle = FlatStyle.System,
                Enabled = false
            };
            _btnInsert.Click += OnInsertClick;
            Controls.Add(_btnInsert);

            // Cancel button
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(ClientSize.Width - 88, ClientSize.Height - 38),
                Width = 75,
                FlatStyle = FlatStyle.System
            };
            Controls.Add(btnCancel);

            // No AcceptButton — Enter creates newlines in the textbox
            CancelButton = btnCancel;
        }

        private List<string> ParseTerms()
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(_txtTerms.Text)) return result;

            foreach (var line in _txtTerms.Text.Split('\n'))
            {
                var trimmed = line.Trim().TrimEnd('\r');
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result;
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            var terms = ParseTerms();
            _lblCount.Text = terms.Count == 1 ? "1 term to add" : $"{terms.Count} terms to add";
            _btnInsert.Enabled = terms.Count > 0;
        }

        private void OnInsertClick(object sender, EventArgs e)
        {
            Terms = ParseTerms();
            if (Terms.Count == 0) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
