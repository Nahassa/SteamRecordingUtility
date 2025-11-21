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
        private Button btnLoadVideos = null!;
        private Button btnConvertAll = null!;
        private Button btnYouTubeSettings = null!;

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
        private Button btnShowLog = null!;
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
        }

        private void InitializeComponent()
        {
            this.Text = "Steam Recording Video Converter";
            this.Size = new Size(1280, 720);
            // Minimum 16:9 aspect ratio at 1280x720
            this.MinimumSize = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;

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

            // Input Folder
            var lblInput = new Label { Text = "Input Folder:", Location = new Point(10, y + 3), Width = 90 };
            txtInputFolder = new TextBox
            {
                Location = new Point(105, y),
                Width = 500,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseInput = new Button
            {
                Text = "...",
                Width = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseInput.Location = new Point(txtInputFolder.Right + 5, y - 2);
            btnBrowseInput.Click += BtnBrowseInput_Click;
            pnlTop.Controls.AddRange(new Control[] { lblInput, txtInputFolder, btnBrowseInput });

            y += 30;

            // Output Folder
            var lblOutput = new Label { Text = "Output Folder:", Location = new Point(10, y + 3), Width = 90 };
            txtOutputFolder = new TextBox
            {
                Location = new Point(105, y),
                Width = 500,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseOutput = new Button
            {
                Text = "...",
                Width = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseOutput.Location = new Point(txtOutputFolder.Right + 5, y - 2);
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            pnlTop.Controls.AddRange(new Control[] { lblOutput, txtOutputFolder, btnBrowseOutput });

            y += 35;

            // Buttons
            btnLoadVideos = new Button
            {
                Text = "Load Videos",
                Location = new Point(10, y),
                Width = 120,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            btnLoadVideos.Click += BtnLoadVideos_Click;

            btnConvertAll = new Button
            {
                Text = "Convert All",
                Location = new Point(140, y),
                Width = 120,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                Enabled = false
            };
            btnConvertAll.Click += BtnConvertAll_Click;

            btnYouTubeSettings = new Button
            {
                Text = "YouTube Settings...",
                Location = new Point(270, y),
                Width = 140,
                Height = 30
            };
            btnYouTubeSettings.Click += BtnYouTubeSettings_Click;

            pnlTop.Controls.AddRange(new Control[] { btnLoadVideos, btnConvertAll, btnYouTubeSettings });
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

            // Left side: Thumbnail previews
            var pnlThumbnails = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            previewLayout.Controls.Add(pnlThumbnails, 0, 0);

            CreateThumbnailSection(pnlThumbnails);

            // Right side: Settings controls
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
            var grpThumbnails = new GroupBox
            {
                Text = "Preview Thumbnails",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            parent.Controls.Add(grpThumbnails);

            // Use TableLayoutPanel for thumbnail grid
            var thumbLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(5)
            };
            thumbLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            thumbLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            thumbLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F)); // Label row
            thumbLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F)); // Before/After labels
            thumbLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Images
            thumbLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F)); // Before/After labels
            thumbLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Images
            grpThumbnails.Controls.Add(thumbLayout);

            // 40% section header
            lbl40 = new Label
            {
                Text = "40% through video",
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            thumbLayout.Controls.Add(lbl40, 0, 0);
            thumbLayout.SetColumnSpan(lbl40, 2);

            // Before/After labels for 40%
            var lbl40Before = new Label { Text = "Before", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            var lbl40After = new Label { Text = "After", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            thumbLayout.Controls.Add(lbl40Before, 0, 1);
            thumbLayout.Controls.Add(lbl40After, 1, 1);

            // 40% thumbnails
            pic40Before = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            pic40After = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            thumbLayout.Controls.Add(pic40Before, 0, 2);
            thumbLayout.Controls.Add(pic40After, 1, 2);

            // Before/After labels for 60%
            var lbl60Before = new Label { Text = "Before", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            var lbl60After = new Label { Text = "After", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            thumbLayout.Controls.Add(lbl60Before, 0, 3);
            thumbLayout.Controls.Add(lbl60After, 1, 3);

            // 60% section header (placed as tooltip/combined with label)
            lbl60 = new Label
            {
                Text = "60% through video",
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 8, FontStyle.Italic),
                TextAlign = ContentAlignment.BottomRight,
                ForeColor = Color.Gray
            };
            // We'll place this differently - combine with the label row

            // 60% thumbnails
            pic60Before = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            pic60After = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            thumbLayout.Controls.Add(pic60Before, 0, 4);
            thumbLayout.Controls.Add(pic60After, 1, 4);
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
                Height = 60,
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

            // Panel for status text and button
            var pnlStatusContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0)
            };
            pnlStatusBar.Controls.Add(pnlStatusContent);

            // Status label (left side)
            lblProgress = new Label
            {
                Text = "Ready",
                Location = new Point(0, 5),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            pnlStatusContent.Controls.Add(lblProgress);

            // Current task label
            lblCurrentTask = new Label
            {
                Text = "",
                Location = new Point(0, 22),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            pnlStatusContent.Controls.Add(lblCurrentTask);

            // Show Log button (right side)
            btnShowLog = new Button
            {
                Text = "Show Log...",
                Width = 90,
                Height = 25,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnShowLog.Location = new Point(pnlStatusContent.Width - btnShowLog.Width - 5, 5);
            btnShowLog.Click += BtnShowLog_Click;
            pnlStatusContent.Controls.Add(btnShowLog);

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
            // Update the Show Log button position when resizing
            if (btnShowLog != null && pnlStatusBar != null)
            {
                btnShowLog.Location = new Point(pnlStatusBar.Width - btnShowLog.Width - 25, 5);
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
