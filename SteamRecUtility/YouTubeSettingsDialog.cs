using System;
using System.Windows.Forms;

namespace SteamRecUtility
{
    public class YouTubeSettingsDialog : Form
    {
        private AppSettings settings;
        public YouTubeUploader? YouTubeUploader { get; private set; }

        private CheckBox chkEnableYouTube = null!;
        private TextBox txtTitleTemplate = null!;
        private TextBox txtDescriptionTemplate = null!;
        private TextBox txtTags = null!;
        private ComboBox cmbPrivacy = null!;
        private ComboBox cmbCategory = null!;
        private CheckBox chkMadeForKids = null!;
        private CheckBox chkAgeRestricted = null!;
        private CheckBox chkRemoveDateFromFilename = null!;
        private TextBox txtRemoveTextPatterns = null!;
        private Button btnAuth = null!;
        private Label lblStatus = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        public YouTubeSettingsDialog(AppSettings settings, YouTubeUploader? uploader)
        {
            this.settings = settings;
            this.YouTubeUploader = uploader;
            InitializeComponent();
            LoadSettings();

            // Try to restore authentication if we don't have an uploader yet
            if (this.YouTubeUploader == null)
            {
                TryRestoreAuthAsync();
            }
        }

        private async void TryRestoreAuthAsync()
        {
            var uploader = new YouTubeUploader();
            if (await uploader.TryRestoreAuthenticationAsync())
            {
                YouTubeUploader = uploader;
                lblStatus.Text = "Authenticated (restored)";
                lblStatus.ForeColor = Color.Green;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "YouTube Upload Settings";
            this.Size = new Size(600, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 15;
            int labelWidth = 120;
            int controlWidth = 430;

            // Enable YouTube
            chkEnableYouTube = new CheckBox
            {
                Text = "Enable YouTube Upload",
                Location = new Point(15, y),
                Width = 300,
                Checked = settings.EnableYouTubeUpload
            };
            chkEnableYouTube.CheckedChanged += ChkEnableYouTube_CheckedChanged;
            this.Controls.Add(chkEnableYouTube);

            y += 35;

            // Title Template
            var lblTitle = new Label { Text = "Title Template:", Location = new Point(15, y + 3), Width = labelWidth };
            txtTitleTemplate = new TextBox
            {
                Location = new Point(145, y),
                Width = controlWidth,
                Text = settings.YouTubeTitleTemplate
            };
            this.Controls.AddRange(new Control[] { lblTitle, txtTitleTemplate });

            y += 30;

            var lblTitleHelp = new Label
            {
                Text = "Use {filename}, {date} (current date), {datetime}, {year}, {month}, {day}",
                Location = new Point(145, y),
                Width = controlWidth,
                ForeColor = Color.Gray,
                Font = new Font(this.Font.FontFamily, 8)
            };
            this.Controls.Add(lblTitleHelp);

            y += 25;

            // Description Template
            var lblDesc = new Label { Text = "Description:", Location = new Point(15, y + 3), Width = labelWidth };
            txtDescriptionTemplate = new TextBox
            {
                Location = new Point(145, y),
                Width = controlWidth,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = settings.YouTubeDescriptionTemplate
            };
            this.Controls.AddRange(new Control[] { lblDesc, txtDescriptionTemplate });

            y += 90;

            // Tags
            var lblTags = new Label { Text = "Tags (comma-separated):", Location = new Point(15, y + 3), Width = labelWidth };
            txtTags = new TextBox
            {
                Location = new Point(145, y),
                Width = controlWidth,
                Text = settings.YouTubeTags
            };
            this.Controls.AddRange(new Control[] { lblTags, txtTags });

            y += 35;

            // Privacy
            var lblPrivacy = new Label { Text = "Privacy:", Location = new Point(15, y + 3), Width = labelWidth };
            cmbPrivacy = new ComboBox
            {
                Location = new Point(145, y),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPrivacy.Items.AddRange(new[] { "private", "unlisted", "public" });
            cmbPrivacy.Text = settings.YouTubePrivacyStatus;
            this.Controls.AddRange(new Control[] { lblPrivacy, cmbPrivacy });

            y += 35;

            // Category
            var lblCategory = new Label { Text = "Category:", Location = new Point(15, y + 3), Width = labelWidth };
            cmbCategory = new ComboBox
            {
                Location = new Point(145, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.AddRange(new[] { "Gaming (20)", "Entertainment (24)", "People & Blogs (22)" });

            string categoryDisplay = settings.YouTubeCategoryId switch
            {
                "20" => "Gaming (20)",
                "24" => "Entertainment (24)",
                "22" => "People & Blogs (22)",
                _ => "Gaming (20)"
            };
            cmbCategory.Text = categoryDisplay;

            this.Controls.AddRange(new Control[] { lblCategory, cmbCategory });

            y += 35;

            // Audience settings
            var lblAudience = new Label { Text = "Audience:", Location = new Point(15, y + 3), Width = labelWidth };
            chkMadeForKids = new CheckBox
            {
                Text = "Made for kids",
                Location = new Point(145, y),
                Width = 130,
                Checked = settings.YouTubeMadeForKids
            };
            chkAgeRestricted = new CheckBox
            {
                Text = "Age restricted (18+)",
                Location = new Point(280, y),
                Width = 150,
                Checked = settings.YouTubeAgeRestricted
            };
            this.Controls.AddRange(new Control[] { lblAudience, chkMadeForKids, chkAgeRestricted });

            y += 35;

            // Filename options
            var lblFilenameOptions = new Label { Text = "Filename:", Location = new Point(15, y + 3), Width = labelWidth };
            chkRemoveDateFromFilename = new CheckBox
            {
                Text = "Remove dates from video title (e.g., 2024-01-15)",
                Location = new Point(145, y),
                Width = 350,
                Checked = settings.YouTubeRemoveDateFromFilename
            };
            this.Controls.AddRange(new Control[] { lblFilenameOptions, chkRemoveDateFromFilename });

            y += 30;

            // Remove text patterns
            var lblRemoveText = new Label { Text = "", Location = new Point(15, y + 3), Width = labelWidth };
            txtRemoveTextPatterns = new TextBox
            {
                Location = new Point(145, y),
                Width = controlWidth,
                Text = settings.YouTubeRemoveTextPatterns,
                PlaceholderText = "e.g., recording, test, 2024-"
            };
            this.Controls.AddRange(new Control[] { lblRemoveText, txtRemoveTextPatterns });

            y += 25;

            var lblRemoveTextHelp = new Label
            {
                Text = "Remove specific text (comma-separated). Applied after date removal.",
                Location = new Point(145, y),
                Width = controlWidth,
                ForeColor = Color.Gray,
                Font = new Font(this.Font.FontFamily, 8)
            };
            this.Controls.Add(lblRemoveTextHelp);

            y += 35;

            // Authentication
            var lblAuth = new Label { Text = "Authentication:", Location = new Point(15, y + 3), Width = labelWidth };
            btnAuth = new Button
            {
                Text = "Authenticate with YouTube",
                Location = new Point(145, y),
                Width = 200,
                Height = 30
            };
            btnAuth.Click += BtnAuth_Click;

            lblStatus = new Label
            {
                Location = new Point(355, y + 7),
                Width = 220,
                ForeColor = YouTubeUploader != null ? Color.Green : Color.Gray,
                Text = YouTubeUploader != null ? "Authenticated" : "Not authenticated"
            };

            this.Controls.AddRange(new Control[] { lblAuth, btnAuth, lblStatus });

            y += 45;

            var lblAuthHelp = new Label
            {
                Text = "Click to open browser for Google OAuth authentication",
                Location = new Point(145, y),
                Width = controlWidth,
                ForeColor = Color.Gray,
                Font = new Font(this.Font.FontFamily, 8)
            };
            this.Controls.Add(lblAuthHelp);

            y += 35;

            // OK and Cancel buttons
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(380, y),
                Width = 90,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(480, y),
                Width = 90,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            UpdateControlsEnabled();
        }

        private void LoadSettings()
        {
            // Settings already loaded in constructor
        }

        private void ChkEnableYouTube_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateControlsEnabled();
        }

        private void UpdateControlsEnabled()
        {
            bool enabled = chkEnableYouTube.Checked;
            txtTitleTemplate.Enabled = enabled;
            txtDescriptionTemplate.Enabled = enabled;
            txtTags.Enabled = enabled;
            cmbPrivacy.Enabled = enabled;
            cmbCategory.Enabled = enabled;
            chkMadeForKids.Enabled = enabled;
            chkAgeRestricted.Enabled = enabled;
            chkRemoveDateFromFilename.Enabled = enabled;
            txtRemoveTextPatterns.Enabled = enabled;
            btnAuth.Enabled = enabled;
        }

        private async void BtnAuth_Click(object? sender, EventArgs e)
        {
            btnAuth.Enabled = false;
            lblStatus.Text = "Authenticating...";
            lblStatus.ForeColor = Color.Gray;

            try
            {
                YouTubeUploader = new YouTubeUploader();
                bool success = await YouTubeUploader.AuthenticateAsync();

                if (success)
                {
                    lblStatus.Text = "Authenticated successfully!";
                    lblStatus.ForeColor = Color.Green;
                    MessageBox.Show("YouTube authentication successful!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "Authentication failed";
                    lblStatus.ForeColor = Color.Red;
                    YouTubeUploader = null;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                YouTubeUploader = null;
                MessageBox.Show($"Authentication error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAuth.Enabled = true;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Save settings
            settings.EnableYouTubeUpload = chkEnableYouTube.Checked;
            settings.YouTubeTitleTemplate = txtTitleTemplate.Text;
            settings.YouTubeDescriptionTemplate = txtDescriptionTemplate.Text;
            settings.YouTubeTags = txtTags.Text;
            settings.YouTubePrivacyStatus = cmbPrivacy.Text;

            string categoryText = cmbCategory.Text;
            string categoryId = categoryText.Split('(').Last().TrimEnd(')');
            settings.YouTubeCategoryId = categoryId;

            settings.YouTubeMadeForKids = chkMadeForKids.Checked;
            settings.YouTubeAgeRestricted = chkAgeRestricted.Checked;
            settings.YouTubeRemoveDateFromFilename = chkRemoveDateFromFilename.Checked;
            settings.YouTubeRemoveTextPatterns = txtRemoveTextPatterns.Text;

            settings.Save();
        }
    }
}
