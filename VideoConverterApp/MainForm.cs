using System.Diagnostics;
using System.Globalization;

namespace VideoConverterApp
{
    public partial class MainForm : Form
    {
        private AppSettings settings;
        private YouTubeUploader? youtubeUploader;
        private List<VideoItem> videoItems = new List<VideoItem>();
        private VideoItem? currentVideo;
        private System.Windows.Forms.Timer? previewRefreshTimer;

        // Top panel controls
        private Panel pnlTop = null!;
        private TextBox txtInputFolder = null!;
        private TextBox txtOutputFolder = null!;
        private Button btnBrowseInput = null!;
        private Button btnBrowseOutput = null!;
        private Button btnSettings = null!;
        private Button btnYouTubeSettings = null!;
        private Button btnLoadVideos = null!;
        private Button btnConvertAll = null!;
        private Button btnShowLog = null!;

        // Split container for resizable layout
        private SplitContainer splitMain = null!;        // Vertical: Videos | Preview

        // Left panel - Video list
        private ListBox lstVideos = null!;

        // Preview panel controls
        private Panel pnlPreview = null!;
        private PictureBox pic40Before = null!;
        private PictureBox pic40After = null!;
        private PictureBox pic60Before = null!;
        private PictureBox pic60After = null!;
        private Label lbl40 = null!;
        private Label lbl60 = null!;

        // Adjustment controls
        private TrackBar trackBrightness = null!;
        private TrackBar trackContrast = null!;
        private TrackBar trackSaturation = null!;
        private Label lblBrightnessValue = null!;
        private Label lblContrastValue = null!;
        private Label lblSaturationValue = null!;
        private Button btnApplyToAll = null!;
        private Button btnReset = null!;
        private Button btnRefreshPreview = null!;

        // Resolution and quality
        private ComboBox cmbResolution = null!;
        private NumericUpDown numCRF = null!;
        private NumericUpDown numBitrate = null!;
        private CheckBox chkMoveProcessed = null!;

        // Progress and status bar (log moved to separate dialog)
        private ProgressBar progressBar = null!;
        private Label lblProgress = null!;
        private Label lblCurrentTask = null!;
        private Panel pnlStatusBar = null!;

        // Log dialog and its RichTextBox
        private LogDialog? logDialog = null;
        private RichTextBox txtLog = null!;

        public MainForm()
        {
            settings = AppSettings.Load();
            InitializeComponent();
            LoadSettings();

            // Setup auto-refresh timer (debounce slider changes)
            previewRefreshTimer = new System.Windows.Forms.Timer();
            previewRefreshTimer.Interval = 800; // 800ms delay after last slider change
            previewRefreshTimer.Tick += PreviewRefreshTimer_Tick;

            // Try to restore YouTube authentication in the background
            TryRestoreYouTubeAuthAsync();
        }

        private async void TryRestoreYouTubeAuthAsync()
        {
            if (settings.EnableYouTubeUpload)
            {
                var uploader = new YouTubeUploader();
                if (await uploader.TryRestoreAuthenticationAsync())
                {
                    youtubeUploader = uploader;
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Steam Recording Video Converter";
            this.Size = new Size(1200, 700);
            // Minimum width to prevent horizontal scrollbars (based on content widths)
            // Video list (250) + Thumbnails (460) + Settings (350) + margins (60) = 1120
            this.MinimumSize = new Size(1120, 0);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
            this.Activated += MainForm_Activated;

            // Create main split container layout FIRST (will dock Fill)
            CreateMainLayout();

            // Create top panel LAST (will dock Top - takes priority in z-order)
            CreateTopPanel();
        }

        private void CreateTopPanel()
        {
            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlTop);

            int y = 10;
            int labelWidth = 90;
            int textBoxWidth = 500;
            int browseWidth = 30;
            int settingsBtnWidth = 110;

            // Row 1: Input Folder + ... + Settings button
            var lblInput = new Label { Text = "Input Folder:", Location = new Point(10, y + 3), Width = labelWidth };
            txtInputFolder = new TextBox
            {
                Location = new Point(10 + labelWidth + 5, y),
                Width = textBoxWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseInput = new Button
            {
                Text = "...",
                Width = browseWidth,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseInput.Click += BtnBrowseInput_Click;

            btnSettings = new Button
            {
                Text = "Settings...",
                Width = settingsBtnWidth,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSettings.Click += BtnSettings_Click;

            pnlTop.Controls.AddRange(new Control[] { lblInput, txtInputFolder, btnBrowseInput, btnSettings });

            y += 28;

            // Row 2: Output Folder + ... + YouTube Settings button
            var lblOutput = new Label { Text = "Output Folder:", Location = new Point(10, y + 3), Width = labelWidth };
            txtOutputFolder = new TextBox
            {
                Location = new Point(10 + labelWidth + 5, y),
                Width = textBoxWidth,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseOutput = new Button
            {
                Text = "...",
                Width = browseWidth,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            btnYouTubeSettings = new Button
            {
                Text = "YouTube...",
                Width = settingsBtnWidth,
                Height = 23,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnYouTubeSettings.Click += BtnYouTubeSettings_Click;

            pnlTop.Controls.AddRange(new Control[] { lblOutput, txtOutputFolder, btnBrowseOutput, btnYouTubeSettings });

            y += 32;

            // Row 3: Load Videos + Convert All (center-ish) | Show Log (right)
            btnLoadVideos = new Button
            {
                Text = "Load Videos",
                Location = new Point(10 + labelWidth + 5, y),
                Width = 110,
                Height = 28,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            btnLoadVideos.Click += BtnLoadVideos_Click;

            btnConvertAll = new Button
            {
                Text = "Convert All",
                Location = new Point(10 + labelWidth + 5 + 115, y),
                Width = 110,
                Height = 28,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                Enabled = false
            };
            btnConvertAll.Click += BtnConvertAll_Click;

            btnShowLog = new Button
            {
                Text = "Show Log...",
                Width = settingsBtnWidth,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnShowLog.Click += BtnShowLog_Click;

            pnlTop.Controls.AddRange(new Control[] { btnLoadVideos, btnConvertAll, btnShowLog });

            // Position anchored buttons after adding to panel
            UpdateTopPanelButtonPositions();
        }

        private void UpdateTopPanelButtonPositions()
        {
            if (pnlTop == null) return;

            int rightMargin = 10;
            int settingsBtnWidth = 110;
            int browseWidth = 30;
            int gap = 5;

            // Calculate right-aligned positions
            int showLogRight = pnlTop.ClientSize.Width - rightMargin;
            int youtubeRight = showLogRight;
            int settingsRight = showLogRight;

            // Row 1: Settings button and browse button positions
            btnSettings.Location = new Point(settingsRight - settingsBtnWidth, 10);
            btnBrowseInput.Location = new Point(btnSettings.Left - gap - browseWidth, 10);
            txtInputFolder.Width = btnBrowseInput.Left - gap - txtInputFolder.Left;

            // Row 2: YouTube button and browse button positions
            btnYouTubeSettings.Location = new Point(youtubeRight - settingsBtnWidth, 38);
            btnBrowseOutput.Location = new Point(btnYouTubeSettings.Left - gap - browseWidth, 38);
            txtOutputFolder.Width = btnBrowseOutput.Left - gap - txtOutputFolder.Left;

            // Row 3: Show Log button position
            btnShowLog.Location = new Point(showLogRight - settingsBtnWidth, 70);
        }

        private void CreateMainLayout()
        {
            // Create status bar at bottom first (will dock Bottom)
            CreateStatusBar();

            // Main horizontal split: Videos (left) | Preview (right)
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 250,
                SplitterWidth = 6,
                BorderStyle = BorderStyle.Fixed3D,
                FixedPanel = FixedPanel.Panel1  // Keep video list fixed width when resizing
            };
            this.Controls.Add(splitMain);

            // Left panel: Video list
            CreateVideoListPanel();

            // Right side: Preview and settings (no log - moved to dialog)
            CreatePreviewPanel();
        }

        private void CreateVideoListPanel()
        {
            var lblVideos = new Label
            {
                Text = "Videos",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lstVideos = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                SelectionMode = SelectionMode.One
            };
            lstVideos.SelectedIndexChanged += LstVideos_SelectedIndexChanged;

            splitMain.Panel1.Controls.AddRange(new Control[] { lstVideos, lblVideos });
        }

        private void CreatePreviewPanel()
        {
            pnlPreview = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(5)
            };
            splitMain.Panel2.Controls.Add(pnlPreview);

            // Create a TableLayoutPanel for responsive layout
            var previewLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Thumbnails
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Settings
            pnlPreview.Controls.Add(previewLayout);

            // Left side: Thumbnail previews (scrollable with fixed-size content)
            var pnlThumbnails = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            previewLayout.Controls.Add(pnlThumbnails, 0, 0);

            CreateThumbnailSection(pnlThumbnails);

            // Right side: Settings controls (scrollable)
            var pnlSettings = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10, 0, 0, 0)
            };
            previewLayout.Controls.Add(pnlSettings, 1, 0);

            CreateSettingsSection(pnlSettings);
        }

        private void CreateThumbnailSection(Panel parent)
        {
            // Fixed-size container for thumbnails (enables scrolling)
            var thumbContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(460, 520),
                Padding = new Padding(5)
            };
            parent.Controls.Add(thumbContainer);

            int y = 5;
            int imgWidth = 200;
            int imgHeight = 150;
            int gap = 10;

            // 40% section header
            lbl40 = new Label
            {
                Text = "40% through video",
                Location = new Point(5, y),
                Size = new Size(450, 20),
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            thumbContainer.Controls.Add(lbl40);
            y += 22;

            // Before/After labels for 40%
            var lbl40Before = new Label { Text = "Before", Location = new Point(5, y), Size = new Size(imgWidth, 18) };
            var lbl40After = new Label { Text = "After", Location = new Point(5 + imgWidth + gap, y), Size = new Size(imgWidth, 18) };
            thumbContainer.Controls.Add(lbl40Before);
            thumbContainer.Controls.Add(lbl40After);
            y += 20;

            // 40% thumbnails (fixed size)
            pic40Before = new PictureBox
            {
                Location = new Point(5, y),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            pic40After = new PictureBox
            {
                Location = new Point(5 + imgWidth + gap, y),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            thumbContainer.Controls.Add(pic40Before);
            thumbContainer.Controls.Add(pic40After);
            y += imgHeight + 15;

            // 60% section header
            lbl60 = new Label
            {
                Text = "60% through video",
                Location = new Point(5, y),
                Size = new Size(450, 20),
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            thumbContainer.Controls.Add(lbl60);
            y += 22;

            // Before/After labels for 60%
            var lbl60Before = new Label { Text = "Before", Location = new Point(5, y), Size = new Size(imgWidth, 18) };
            var lbl60After = new Label { Text = "After", Location = new Point(5 + imgWidth + gap, y), Size = new Size(imgWidth, 18) };
            thumbContainer.Controls.Add(lbl60Before);
            thumbContainer.Controls.Add(lbl60After);
            y += 20;

            // 60% thumbnails (fixed size)
            pic60Before = new PictureBox
            {
                Location = new Point(5, y),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            pic60After = new PictureBox
            {
                Location = new Point(5 + imgWidth + gap, y),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            thumbContainer.Controls.Add(pic60Before);
            thumbContainer.Controls.Add(pic60After);
            y += imgHeight + 10;

            // Set container size to fit content (enables scrolling when parent is smaller)
            thumbContainer.Size = new Size(5 + imgWidth * 2 + gap + 15, y);
        }

        private void CreateSettingsSection(Panel parent)
        {
            var grpSettings = new GroupBox
            {
                Text = "Adjustment Settings",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            parent.Controls.Add(grpSettings);

            var settingsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            grpSettings.Controls.Add(settingsContainer);

            int y = 10;
            int labelWidth = 80;
            int trackWidth = 180;
            int valueWidth = 50;

            // Brightness
            var lblBrightness = new Label { Text = "Brightness:", Location = new Point(10, y + 3), Width = labelWidth };
            trackBrightness = new TrackBar
            {
                Location = new Point(10 + labelWidth, y),
                Width = trackWidth,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 20
            };
            trackBrightness.ValueChanged += TrackAdjustments_ValueChanged;

            lblBrightnessValue = new Label
            {
                Location = new Point(10 + labelWidth + trackWidth + 5, y + 3),
                Width = valueWidth,
                Text = "0.00"
            };

            settingsContainer.Controls.AddRange(new Control[] { lblBrightness, trackBrightness, lblBrightnessValue });
            y += 50;

            // Contrast
            var lblContrast = new Label { Text = "Contrast:", Location = new Point(10, y + 3), Width = labelWidth };
            trackContrast = new TrackBar
            {
                Location = new Point(10 + labelWidth, y),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 400,
                Value = 100,
                TickFrequency = 50
            };
            trackContrast.ValueChanged += TrackAdjustments_ValueChanged;

            lblContrastValue = new Label
            {
                Location = new Point(10 + labelWidth + trackWidth + 5, y + 3),
                Width = valueWidth,
                Text = "1.00"
            };

            settingsContainer.Controls.AddRange(new Control[] { lblContrast, trackContrast, lblContrastValue });
            y += 50;

            // Saturation
            var lblSaturation = new Label { Text = "Saturation:", Location = new Point(10, y + 3), Width = labelWidth };
            trackSaturation = new TrackBar
            {
                Location = new Point(10 + labelWidth, y),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 300,
                Value = 120,
                TickFrequency = 30
            };
            trackSaturation.ValueChanged += TrackAdjustments_ValueChanged;

            lblSaturationValue = new Label
            {
                Location = new Point(10 + labelWidth + trackWidth + 5, y + 3),
                Width = valueWidth,
                Text = "1.20"
            };

            settingsContainer.Controls.AddRange(new Control[] { lblSaturation, trackSaturation, lblSaturationValue });
            y += 50;

            // Buttons
            btnRefreshPreview = new Button
            {
                Text = "Refresh Preview",
                Location = new Point(10, y),
                Width = 110,
                Height = 25
            };
            btnRefreshPreview.Click += BtnRefreshPreview_Click;

            btnApplyToAll = new Button
            {
                Text = "Apply to All",
                Location = new Point(125, y),
                Width = 90,
                Height = 25
            };
            btnApplyToAll.Click += BtnApplyToAll_Click;

            btnReset = new Button
            {
                Text = "Reset",
                Location = new Point(220, y),
                Width = 70,
                Height = 25
            };
            btnReset.Click += BtnReset_Click;

            settingsContainer.Controls.AddRange(new Control[] { btnRefreshPreview, btnApplyToAll, btnReset });
            y += 35;

            // Resolution and quality settings
            var lblResolution = new Label { Text = "Resolution:", Location = new Point(10, y + 3), Width = labelWidth };
            cmbResolution = new ComboBox
            {
                Location = new Point(10 + labelWidth, y),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbResolution.Items.AddRange(new[] { "1920x1080 (Full HD)", "2560x1440 (2K)", "3840x2160 (4K)", "Custom" });
            cmbResolution.SelectedIndex = 0;

            settingsContainer.Controls.AddRange(new Control[] { lblResolution, cmbResolution });
            y += 35;

            var lblCRF = new Label { Text = "CRF:", Location = new Point(10, y + 3), Width = labelWidth };
            numCRF = new NumericUpDown
            {
                Location = new Point(10 + labelWidth, y),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 18
            };

            var lblBitrate = new Label { Text = "Bitrate:", Location = new Point(160, y + 3), Width = 50 };
            numBitrate = new NumericUpDown
            {
                Location = new Point(215, y),
                Width = 75,
                Minimum = 1000,
                Maximum = 100000,
                Value = 20000,
                Increment = 1000
            };
            var lblKbps = new Label { Text = "kbps", Location = new Point(292, y + 3), Width = 35 };

            settingsContainer.Controls.AddRange(new Control[] { lblCRF, numCRF, lblBitrate, numBitrate, lblKbps });
            y += 35;

            chkMoveProcessed = new CheckBox
            {
                Text = "Move original files to processed folder",
                Location = new Point(10, y),
                Width = 280,
                Checked = true
            };

            settingsContainer.Controls.Add(chkMoveProcessed);
        }

        private void CreateStatusBar()
        {
            pnlStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(pnlStatusBar);

            // Progress bar at top of status bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 18,
                Style = ProgressBarStyle.Continuous
            };
            pnlStatusBar.Controls.Add(progressBar);

            // Spacer panel between progress bar and text
            var pnlSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8
            };
            pnlStatusBar.Controls.Add(pnlSpacer);

            // Panel for status text (add AFTER spacer so it appears below)
            var pnlStatusContent = new Panel
            {
                Dock = DockStyle.Fill
            };
            pnlStatusBar.Controls.Add(pnlStatusContent);

            // Status label
            lblProgress = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                Padding = new Padding(0, 0, 0, 0)
            };
            pnlStatusContent.Controls.Add(lblProgress);

            // Current task label (to the right of status)
            lblCurrentTask = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Color.DarkBlue,
                Padding = new Padding(10, 0, 0, 0)
            };
            pnlStatusContent.Controls.Add(lblCurrentTask);

            // Initialize the log dialog (hidden by default)
            InitializeLogDialog();
        }

        private void InitializeLogDialog()
        {
            logDialog = new LogDialog();
            txtLog = logDialog.LogTextBox;
        }

        private void BtnShowLog_Click(object? sender, EventArgs e)
        {
            if (logDialog != null)
            {
                if (logDialog.Visible)
                {
                    logDialog.Focus();
                }
                else
                {
                    logDialog.Show(this);
                }
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Update top panel button positions when resizing
            UpdateTopPanelButtonPositions();
        }

        private void MainForm_Activated(object? sender, EventArgs e)
        {
            // When main form is activated (clicked from taskbar or another app),
            // bring the log dialog to front too if it's visible (best practice for owned windows)
            if (logDialog != null && logDialog.Visible)
            {
                // Bring log dialog to front without stealing focus from main form
                logDialog.BringToFront();
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            // Open Settings dialog
            using var dlg = new SettingsDialog(settings);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // Reload settings that might have changed
                numCRF.Value = settings.CRF;
                numBitrate.Value = settings.Bitrate;
                chkMoveProcessed.Checked = settings.MoveProcessedFiles;

                // Update trackbar defaults for new videos
                trackBrightness.Value = (int)(settings.Brightness * 100);
                trackContrast.Value = (int)(settings.Contrast * 100);
                trackSaturation.Value = (int)(settings.Saturation * 100);
            }
        }

        private void BtnYouTubeSettings_Click(object? sender, EventArgs e)
        {
            // Open YouTube settings in a separate dialog
            using var dlg = new YouTubeSettingsDialog(settings, youtubeUploader);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                youtubeUploader = dlg.YouTubeUploader;
                SaveSettings();
            }
        }

        // Event handlers and core logic continued in partial classes...
    }
}
