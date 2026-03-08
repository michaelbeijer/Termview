using System;
using System.Drawing;
using System.Windows.Forms;

namespace Supervertaler.Trados.Controls
{
    /// <summary>
    /// Dialog for creating a new termbase inside a database.
    /// Collects the termbase name, source language, and target language.
    /// </summary>
    public class NewTermbaseDialog : Form
    {
        private TextBox _txtName;
        private TextBox _txtSourceLang;
        private TextBox _txtTargetLang;
        private Button _btnCreate;

        /// <summary>The termbase name entered by the user.</summary>
        public string TermbaseName => _txtName.Text.Trim();

        /// <summary>The source language code entered by the user.</summary>
        public string SourceLang => _txtSourceLang.Text.Trim();

        /// <summary>The target language code entered by the user.</summary>
        public string TargetLang => _txtTargetLang.Text.Trim();

        public NewTermbaseDialog()
        {
            Text = "New Termbase";
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(400, 200);
            BackColor = Color.White;

            int y = 16;
            int inputWidth = ClientSize.Width - 32;
            int halfWidth = (inputWidth - 8) / 2;

            // Termbase name
            Controls.Add(new Label
            {
                Text = "Termbase name:",
                Location = new Point(16, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            y += 20;

            _txtName = new TextBox
            {
                Location = new Point(16, y),
                Width = inputWidth,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            _txtName.TextChanged += OnFieldChanged;
            Controls.Add(_txtName);
            y += 34;

            // Source language
            Controls.Add(new Label
            {
                Text = "Source language:",
                Location = new Point(16, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            });

            // Target language
            Controls.Add(new Label
            {
                Text = "Target language:",
                Location = new Point(16 + halfWidth + 8, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            y += 20;

            _txtSourceLang = new TextBox
            {
                Location = new Point(16, y),
                Width = halfWidth,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            _txtSourceLang.TextChanged += OnFieldChanged;
            Controls.Add(_txtSourceLang);

            _txtTargetLang = new TextBox
            {
                Location = new Point(16 + halfWidth + 8, y),
                Width = halfWidth,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            _txtTargetLang.TextChanged += OnFieldChanged;
            Controls.Add(_txtTargetLang);

            // Separator
            Controls.Add(new Label
            {
                Location = new Point(16, ClientSize.Height - 50),
                Width = ClientSize.Width - 32,
                Height = 1,
                BorderStyle = BorderStyle.Fixed3D
            });

            // Buttons
            _btnCreate = new Button
            {
                Text = "Create",
                DialogResult = DialogResult.OK,
                Location = new Point(ClientSize.Width - 170, ClientSize.Height - 38),
                Width = 75,
                FlatStyle = FlatStyle.System,
                Enabled = false
            };
            Controls.Add(_btnCreate);

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(ClientSize.Width - 88, ClientSize.Height - 38),
                Width = 75,
                FlatStyle = FlatStyle.System
            };
            Controls.Add(btnCancel);

            AcceptButton = _btnCreate;
            CancelButton = btnCancel;
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            _btnCreate.Enabled =
                !string.IsNullOrWhiteSpace(_txtName.Text) &&
                !string.IsNullOrWhiteSpace(_txtSourceLang.Text) &&
                !string.IsNullOrWhiteSpace(_txtTargetLang.Text);
        }
    }
}
