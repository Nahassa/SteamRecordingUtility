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

        // Split containers for resizable layout
        private SplitContainer splitMain = null!;        // Vertical: Videos | (Preview+Log)
        private SplitContainer splitRight = null!;       // Horizontal: Preview | Log

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

        // Progress and log
        private ProgressBar progressBar = null!;
        private Label lblProgress = null!;
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
            this.Size = new Size(1400, 1000);
            this.MinimumSize = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;

            // Create top panel (docked, fixed height)
            CreateTopPanel();

            // Create main split container layout (resizable)
            CreateMainLayout();
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
            // Main horizontal split: Videos (left) | Preview+Log (right)
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300,
                BorderStyle = BorderStyle.Fixed3D,
                FixedPanel = FixedPanel.Panel1  // Keep video list fixed width when resizing
            };
            this.Controls.Add(splitMain);

            // Left panel: Video list
            CreateVideoListPanel();

            // Right side: Vertical split (Preview | Log)
            splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 550,
                BorderStyle = BorderStyle.None
            };
            splitMain.Panel2.Controls.Add(splitRight);

            // Top of right: Preview and settings
            CreatePreviewPanel();

            // Bottom of right: Progress and log (resizable!)
            CreateLogPanel();
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
            splitRight.Panel1.Controls.Add(pnlPreview);

            var grpPreview = new GroupBox
            {
                Text = "Preview & Settings",
                Location = new Point(5, 5),
                Width = 1040,
                Height = 490,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlPreview.Controls.Add(grpPreview);

            // Preview images layout
            int previewTop = 25;
            int imgWidth = 220;
            int imgHeight = 165;

            // 40% through video
            lbl40 = new Label
            {
                Text = "40% through video",
                Location = new Point(10, previewTop),
                Width = 450,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            grpPreview.Controls.Add(lbl40);

            previewTop += 20;

            var lbl40Before = new Label { Text = "Before", Location = new Point(10, previewTop), Width = imgWidth };
            var lbl40After = new Label { Text = "After", Location = new Point(10 + imgWidth + 10, previewTop), Width = imgWidth };
            grpPreview.Controls.AddRange(new Control[] { lbl40Before, lbl40After });

            previewTop += 20;

            pic40Before = new PictureBox
            {
                Location = new Point(10, previewTop),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            pic40After = new PictureBox
            {
                Location = new Point(10 + imgWidth + 10, previewTop),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            grpPreview.Controls.AddRange(new Control[] { pic40Before, pic40After });

            // 60% through video
            previewTop += imgHeight + 15;

            lbl60 = new Label
            {
                Text = "60% through video",
                Location = new Point(10, previewTop),
                Width = 450,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            grpPreview.Controls.Add(lbl60);

            previewTop += 20;

            var lbl60Before = new Label { Text = "Before", Location = new Point(10, previewTop), Width = imgWidth };
            var lbl60After = new Label { Text = "After", Location = new Point(10 + imgWidth + 10, previewTop), Width = imgWidth };
            grpPreview.Controls.AddRange(new Control[] { lbl60Before, lbl60After });

            previewTop += 20;

            pic60Before = new PictureBox
            {
                Location = new Point(10, previewTop),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            pic60After = new PictureBox
            {
                Location = new Point(10 + imgWidth + 10, previewTop),
                Size = new Size(imgWidth, imgHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            grpPreview.Controls.AddRange(new Control[] { pic60Before, pic60After });

            // Controls on the right side
            int controlsLeft = 470;
            int controlsTop = 25;
            CreateAdjustmentControls(grpPreview, controlsLeft, controlsTop);
        }

        private void CreateAdjustmentControls(GroupBox parent, int left, int top)
        {
            int y = top;
            int labelWidth = 80;
            int trackWidth = 200;

            // Brightness
            var lblBrightness = new Label { Text = "Brightness:", Location = new Point(left, y + 3), Width = labelWidth };
            trackBrightness = new TrackBar
            {
                Location = new Point(left + labelWidth, y),
                Width = trackWidth,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 20
            };
            trackBrightness.ValueChanged += TrackAdjustments_ValueChanged;

            lblBrightnessValue = new Label
            {
                Location = new Point(left + labelWidth + trackWidth + 5, y + 3),
                Width = 50,
                Text = "0.00"
            };

            parent.Controls.AddRange(new Control[] { lblBrightness, trackBrightness, lblBrightnessValue });
            y += 50;

            // Contrast
            var lblContrast = new Label { Text = "Contrast:", Location = new Point(left, y + 3), Width = labelWidth };
            trackContrast = new TrackBar
            {
                Location = new Point(left + labelWidth, y),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 400,
                Value = 100,
                TickFrequency = 50
            };
            trackContrast.ValueChanged += TrackAdjustments_ValueChanged;

            lblContrastValue = new Label
            {
                Location = new Point(left + labelWidth + trackWidth + 5, y + 3),
                Width = 50,
                Text = "1.00"
            };

            parent.Controls.AddRange(new Control[] { lblContrast, trackContrast, lblContrastValue });
            y += 50;

            // Saturation
            var lblSaturation = new Label { Text = "Saturation:", Location = new Point(left, y + 3), Width = labelWidth };
            trackSaturation = new TrackBar
            {
                Location = new Point(left + labelWidth, y),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 300,
                Value = 120,
                TickFrequency = 30
            };
            trackSaturation.ValueChanged += TrackAdjustments_ValueChanged;

            lblSaturationValue = new Label
            {
                Location = new Point(left + labelWidth + trackWidth + 5, y + 3),
                Width = 50,
                Text = "1.20"
            };

            parent.Controls.AddRange(new Control[] { lblSaturation, trackSaturation, lblSaturationValue });
            y += 50;

            // Buttons
            btnRefreshPreview = new Button
            {
                Text = "Refresh Preview",
                Location = new Point(left, y),
                Width = 120,
                Height = 25
            };
            btnRefreshPreview.Click += BtnRefreshPreview_Click;

            btnApplyToAll = new Button
            {
                Text = "Apply to All",
                Location = new Point(left + 130, y),
                Width = 100,
                Height = 25
            };
            btnApplyToAll.Click += BtnApplyToAll_Click;

            btnReset = new Button
            {
                Text = "Reset",
                Location = new Point(left + 240, y),
                Width = 80,
                Height = 25
            };
            btnReset.Click += BtnReset_Click;

            parent.Controls.AddRange(new Control[] { btnRefreshPreview, btnApplyToAll, btnReset });
            y += 35;

            // Resolution and quality settings
            var lblResolution = new Label { Text = "Resolution:", Location = new Point(left, y + 3), Width = labelWidth };
            cmbResolution = new ComboBox
            {
                Location = new Point(left + labelWidth, y),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbResolution.Items.AddRange(new[] { "1920x1080 (Full HD)", "2560x1440 (2K)", "3840x2160 (4K)", "Custom" });
            cmbResolution.SelectedIndex = 0;

            parent.Controls.AddRange(new Control[] { lblResolution, cmbResolution });
            y += 35;

            var lblCRF = new Label { Text = "CRF:", Location = new Point(left, y + 3), Width = labelWidth };
            numCRF = new NumericUpDown
            {
                Location = new Point(left + labelWidth, y),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 18
            };

            var lblBitrate = new Label { Text = "Bitrate (kbps):", Location = new Point(left + 160, y + 3), Width = 90 };
            numBitrate = new NumericUpDown
            {
                Location = new Point(left + 250, y),
                Width = 80,
                Minimum = 1000,
                Maximum = 100000,
                Value = 20000,
                Increment = 1000
            };

            parent.Controls.AddRange(new Control[] { lblCRF, numCRF, lblBitrate, numBitrate });
            y += 35;

            chkMoveProcessed = new CheckBox
            {
                Text = "Move original files to processed folder",
                Location = new Point(left, y),
                Width = 300,
                Checked = true
            };

            parent.Controls.Add(chkMoveProcessed);
        }

        private void CreateLogPanel()
        {
            var pnlLog = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            splitRight.Panel2.Controls.Add(pnlLog);

            var lblLog = new Label
            {
                Text = "Log (Drag divider above to resize)",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            pnlLog.Controls.Add(lblLog);

            lblProgress = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = "Ready",
                Padding = new Padding(0, 2, 0, 0)
            };
            pnlLog.Controls.Add(lblProgress);

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };
            pnlLog.Controls.Add(progressBar);

            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLog.Controls.Add(txtLog);
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
