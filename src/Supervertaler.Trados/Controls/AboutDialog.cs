using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Supervertaler.Trados.Controls
{
    /// <summary>
    /// About/Help dialog showing plugin version, keyboard shortcuts, and links.
    /// Opened from the "?" button in the TermLens panel header.
    /// </summary>
    public class AboutDialog : Form
    {
        public AboutDialog()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = $"{version.Major}.{version.Minor}.{version.Build}";

            Text = "About Supervertaler for Trados";
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 500);
            BackColor = Color.White;

            int y = 16;
            int leftPad = 20;
            int contentWidth = ClientSize.Width - 40;

            // Plugin name
            Controls.Add(new Label
            {
                Text = "Supervertaler for Trados",
                Location = new Point(leftPad, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            });
            y += 34;

            // Version
            Controls.Add(new Label
            {
                Text = $"Version {versionStr}",
                Location = new Point(leftPad, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 100, 100)
            });
            y += 22;

            // Copyright
            // Author
            Controls.Add(new Label
            {
                Text = "by Michael Beijer",
                Location = new Point(leftPad, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            y += 20;

            // Email
            var emailLink = new LinkLabel
            {
                Text = "info@michaelbeijer.co.uk",
                Location = new Point(leftPad, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(40, 100, 180),
                ActiveLinkColor = Color.FromArgb(30, 80, 160)
            };
            emailLink.LinkClicked += (s, e) =>
            {
                try
                {
                    Clipboard.SetText("info@michaelbeijer.co.uk");
                    var original = emailLink.Text;
                    emailLink.Text = "Copied!";
                    emailLink.LinkColor = Color.FromArgb(60, 160, 60);
                    var timer = new Timer { Interval = 1500 };
                    timer.Tick += (t, _) =>
                    {
                        emailLink.Text = original;
                        emailLink.LinkColor = Color.FromArgb(40, 100, 180);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
                catch { }
            };
            Controls.Add(emailLink);
            y += 22;

            // Copyright
            Controls.Add(new Label
            {
                Text = $"\u00a9 {DateTime.Now.Year} Supervertaler. All rights reserved.",
                Location = new Point(leftPad, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(120, 120, 120)
            });
            y += 28;

            // Separator
            Controls.Add(new Label
            {
                Location = new Point(leftPad, y),
                Width = contentWidth,
                Height = 1,
                BorderStyle = BorderStyle.Fixed3D
            });
            y += 12;

            // Keyboard shortcuts header
            Controls.Add(new Label
            {
                Text = "Keyboard shortcuts",
                Location = new Point(leftPad, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            });
            y += 26;

            // Shortcuts table
            var shortcuts = new[]
            {
                ("Alt+Down",      "Quick add term to write termbases"),
                ("Alt+Up",        "Quick add term to project termbase"),
                ("Ctrl+Alt+T",    "Add term (dialog)"),
                ("Ctrl+Alt+N",    "Quick add non-translatable"),
                ("Ctrl+Alt+A",    "AI translate segment"),
                ("Ctrl+Shift+G",  "Term Picker"),
                ("Alt+1\u20269",  "Insert term from TermLens panel"),
            };

            var keyFont = new Font("Consolas", 8.5f);
            var descFont = new Font("Segoe UI", 8.5f);
            var keyColor = Color.FromArgb(50, 50, 50);
            var descColor = Color.FromArgb(80, 80, 80);

            foreach (var (key, desc) in shortcuts)
            {
                Controls.Add(new Label
                {
                    Text = key,
                    Location = new Point(leftPad + 4, y),
                    AutoSize = true,
                    Font = keyFont,
                    ForeColor = keyColor
                });
                Controls.Add(new Label
                {
                    Text = desc,
                    Location = new Point(leftPad + 130, y),
                    AutoSize = true,
                    Font = descFont,
                    ForeColor = descColor
                });
                y += 20;
            }

            y += 10;

            // Separator
            Controls.Add(new Label
            {
                Location = new Point(leftPad, y),
                Width = contentWidth,
                Height = 1,
                BorderStyle = BorderStyle.Fixed3D
            });
            y += 12;

            // Links
            AddLink("Website", "https://supervertaler.com", leftPad, ref y);
            AddLink("Documentation", "https://supervertaler.gitbook.io/superdocs", leftPad, ref y);
            AddLink("Support", "https://github.com/michaelbeijer/Supervertaler/issues", leftPad, ref y);

            // Close button
            var btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Width = 75,
                FlatStyle = FlatStyle.System,
                Location = new Point(ClientSize.Width - 95, ClientSize.Height - 44)
            };
            Controls.Add(btnClose);
            CancelButton = btnClose;
        }

        private void AddLink(string text, string url, int x, ref int y)
        {
            var link = new LinkLabel
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                LinkColor = Color.FromArgb(40, 100, 180),
                ActiveLinkColor = Color.FromArgb(30, 80, 160)
            };
            link.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { /* No default browser configured */ }
            };
            Controls.Add(link);
            y += 22;
        }
    }
}
