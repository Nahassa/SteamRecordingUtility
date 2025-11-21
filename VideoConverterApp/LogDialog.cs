using System;
using System.Windows.Forms;

namespace VideoConverterApp
{
    public class LogDialog : Form
    {
        private RichTextBox txtLog = null!;
        private Button btnClear = null!;
        private Button btnCopy = null!;
        private Button btnClose = null!;

        public RichTextBox LogTextBox => txtLog;

        public LogDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Conversion Log";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new Size(400, 300);
            this.ShowInTaskbar = false;

            // Prevent closing, just hide instead
            this.FormClosing += LogDialog_FormClosing;

            // Button panel at bottom
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };
            this.Controls.Add(pnlButtons);

            // Buttons (right-aligned)
            btnClose = new Button
            {
                Text = "Close",
                Width = 80,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Location = new Point(pnlButtons.Width - btnClose.Width - 10, 5);
            btnClose.Click += BtnClose_Click;
            pnlButtons.Controls.Add(btnClose);

            btnCopy = new Button
            {
                Text = "Copy All",
                Width = 80,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCopy.Location = new Point(pnlButtons.Width - btnClose.Width - btnCopy.Width - 20, 5);
            btnCopy.Click += BtnCopy_Click;
            pnlButtons.Controls.Add(btnCopy);

            btnClear = new Button
            {
                Text = "Clear",
                Width = 80,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClear.Location = new Point(pnlButtons.Width - btnClose.Width - btnCopy.Width - btnClear.Width - 30, 5);
            btnClear.Click += BtnClear_Click;
            pnlButtons.Controls.Add(btnClear);

            // Log text box
            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = false
            };
            this.Controls.Add(txtLog);

            // Update button positions on resize
            this.Resize += LogDialog_Resize;
        }

        private void LogDialog_Resize(object? sender, EventArgs e)
        {
            if (btnClose != null && btnCopy != null && btnClear != null)
            {
                var parent = btnClose.Parent;
                if (parent != null)
                {
                    btnClose.Location = new Point(parent.Width - btnClose.Width - 10, 5);
                    btnCopy.Location = new Point(parent.Width - btnClose.Width - btnCopy.Width - 20, 5);
                    btnClear.Location = new Point(parent.Width - btnClose.Width - btnCopy.Width - btnClear.Width - 30, 5);
                }
            }
        }

        private void LogDialog_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Don't actually close, just hide
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Hide();
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLog.Text))
            {
                Clipboard.SetText(txtLog.Text);
                MessageBox.Show("Log copied to clipboard.", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Clear all log entries?", "Clear Log",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                txtLog.Clear();
            }
        }

        public void AppendLog(string message, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendLog(message, color));
                return;
            }

            txtLog.SelectionColor = color;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }
    }
}
