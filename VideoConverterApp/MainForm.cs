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

        // Top controls
        private TextBox txtInputFolder = null!;
        private TextBox txtOutputFolder = null!;
        private Button btnBrowseInput = null!;
        private Button btnBrowseOutput = null!;
        private Button btnLoadVideos = null!;
        private Button btnConvertAll = null!;

        // Left panel - Video list
        private ListBox lstVideos = null!;

        // Right panel - Preview and settings
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

        // YouTube controls
        private CheckBox chkEnableYouTube = null!;
        private TextBox txtYouTubeTitleTemplate = null!;
        private TextBox txtYouTubeDescriptionTemplate = null!;
        private TextBox txtYouTubeTags = null!;
        private ComboBox cmbYouTubePrivacy = null!;
        private ComboBox cmbYouTubeCategory = null!;
        private Button btnYouTubeAuth = null!;
        private Label lblYouTubeStatus = null!;

        // Progress
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
            this.MinimumSize = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;

            int y = 10;

            // Top section - Folder selection and Load Videos
            CreateFolderSelectionSection(ref y);
            CreateVideoListPanel(ref y);
            CreatePreviewPanel();
            CreateBottomSection(ref y);
        }

        private void CreateFolderSelectionSection(ref int y)
        {
            // Input Folder
            var lblInput = new Label { Text = "Input Folder:", Location = new Point(10, y + 3), Width = 80 };
            txtInputFolder = new TextBox { Location = new Point(100, y), Width = 400 };
            btnBrowseInput = new Button { Text = "...", Location = new Point(510, y - 2), Width = 30 };
            btnBrowseInput.Click += BtnBrowseInput_Click;
            this.Controls.AddRange(new Control[] { lblInput, txtInputFolder, btnBrowseInput });

            y += 30;

            // Output Folder
            var lblOutput = new Label { Text = "Output Folder:", Location = new Point(10, y + 3), Width = 80 };
            txtOutputFolder = new TextBox { Location = new Point(100, y), Width = 400 };
            btnBrowseOutput = new Button { Text = "...", Location = new Point(510, y - 2), Width = 30 };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            this.Controls.AddRange(new Control[] { lblOutput, txtOutputFolder, btnBrowseOutput });

            y += 35;

            // Load Videos button
            btnLoadVideos = new Button
            {
                Text = "Load Videos",
                Location = new Point(10, y),
                Width = 120,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            btnLoadVideos.Click += BtnLoadVideos_Click;
            this.Controls.Add(btnLoadVideos);

            y += 40;
        }

        private void CreateVideoListPanel(ref int y)
        {
            var lblVideos = new Label
            {
                Text = "Videos:",
                Location = new Point(10, y),
                Width = 300,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            this.Controls.Add(lblVideos);

            y += 25;

            lstVideos = new ListBox
            {
                Location = new Point(10, y),
                Width = 300,
                Height = 400,
                Font = new Font("Consolas", 9),
                SelectionMode = SelectionMode.One
            };
            lstVideos.SelectedIndexChanged += LstVideos_SelectedIndexChanged;
            this.Controls.Add(lstVideos);
        }

        private void CreatePreviewPanel()
        {
            int panelLeft = 320;
            int panelTop = 115;
            int panelWidth = 1060;

            var grpPreview = new GroupBox
            {
                Text = "Preview & Settings",
                Location = new Point(panelLeft, panelTop),
                Width = panelWidth,
                Height = 520
            };
            this.Controls.Add(grpPreview);

            // Preview section
            int previewTop = 25;
            int imgWidth = 240;
            int imgHeight = 180;

            // 40% labels and images
            lbl40 = new Label
            {
                Text = "40% through video",
                Location = new Point(10, previewTop),
                Width = 500,
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

            // 60% labels and images
            previewTop += imgHeight + 15;

            lbl60 = new Label
            {
                Text = "60% through video",
                Location = new Point(10, previewTop),
                Width = 500,
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

            // Adjustment controls on the right side
            int controlsLeft = 520;
            int controlsTop = 25;
            CreateAdjustmentControls(grpPreview, controlsLeft, controlsTop);
        }

        private void CreateAdjustmentControls(GroupBox parent, int left, int top)
        {
            int y = top;

            // Brightness
            var lblBrightness = new Label { Text = "Brightness (-1.0 to 1.0):", Location = new Point(left, y), Width = 200 };
            parent.Controls.Add(lblBrightness);
            y += 20;

            trackBrightness = new TrackBar
            {
                Location = new Point(left, y),
                Width = 300,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10
            };
            trackBrightness.ValueChanged += TrackAdjustments_ValueChanged;
            parent.Controls.Add(trackBrightness);

            lblBrightnessValue = new Label
            {
                Text = "0.00",
                Location = new Point(left + 310, y + 5),
                Width = 50
            };
            parent.Controls.Add(lblBrightnessValue);

            y += 50;

            // Contrast
            var lblContrast = new Label { Text = "Contrast (0.0 to 4.0):", Location = new Point(left, y), Width = 200 };
            parent.Controls.Add(lblContrast);
            y += 20;

            trackContrast = new TrackBar
            {
                Location = new Point(left, y),
                Width = 300,
                Minimum = 0,
                Maximum = 400,
                Value = 100,
                TickFrequency = 20
            };
            trackContrast.ValueChanged += TrackAdjustments_ValueChanged;
            parent.Controls.Add(trackContrast);

            lblContrastValue = new Label
            {
                Text = "1.00",
                Location = new Point(left + 310, y + 5),
                Width = 50
            };
            parent.Controls.Add(lblContrastValue);

            y += 50;

            // Saturation
            var lblSaturation = new Label { Text = "Saturation (0.0 to 3.0):", Location = new Point(left, y), Width = 200 };
            parent.Controls.Add(lblSaturation);
            y += 20;

            trackSaturation = new TrackBar
            {
                Location = new Point(left, y),
                Width = 300,
                Minimum = 0,
                Maximum = 300,
                Value = 120,
                TickFrequency = 15
            };
            trackSaturation.ValueChanged += TrackAdjustments_ValueChanged;
            parent.Controls.Add(trackSaturation);

            lblSaturationValue = new Label
            {
                Text = "1.20",
                Location = new Point(left + 310, y + 5),
                Width = 50
            };
            parent.Controls.Add(lblSaturationValue);

            y += 60;

            // Refresh Preview button
            btnRefreshPreview = new Button
            {
                Text = "Refresh Preview",
                Location = new Point(left, y),
                Width = 130
            };
            btnRefreshPreview.Click += BtnRefreshPreview_Click;
            parent.Controls.Add(btnRefreshPreview);

            // Apply to All button
            btnApplyToAll = new Button
            {
                Text = "Apply to All Videos",
                Location = new Point(left + 140, y),
                Width = 140
            };
            btnApplyToAll.Click += BtnApplyToAll_Click;
            parent.Controls.Add(btnApplyToAll);

            y += 35;

            // Reset button
            btnReset = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(left, y),
                Width = 130
            };
            btnReset.Click += BtnReset_Click;
            parent.Controls.Add(btnReset);

            y += 45;

            // Resolution
            var lblResolution = new Label { Text = "Output Resolution:", Location = new Point(left, y + 3), Width = 120 };
            parent.Controls.Add(lblResolution);

            cmbResolution = new ComboBox
            {
                Location = new Point(left + 125, y),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbResolution.Items.AddRange(new object[]
            {
                "1920x1080 (Full HD)",
                "2560x1440 (2K)",
                "3840x2160 (4K)"
            });
            cmbResolution.SelectedIndex = 0;
            parent.Controls.Add(cmbResolution);

            y += 35;

            // CRF
            var lblCRF = new Label { Text = "CRF Quality:", Location = new Point(left, y + 3), Width = 80 };
            numCRF = new NumericUpDown
            {
                Location = new Point(left + 85, y),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 18
            };
            var lblCRFHelp = new Label
            {
                Text = "(lower = better, 18-23 recommended)",
                Location = new Point(left + 150, y + 3),
                Width = 250
            };
            parent.Controls.AddRange(new Control[] { lblCRF, numCRF, lblCRFHelp });

            y += 30;

            // Bitrate
            var lblBitrate = new Label { Text = "Bitrate (kbps):", Location = new Point(left, y + 3), Width = 85 };
            numBitrate = new NumericUpDown
            {
                Location = new Point(left + 85, y),
                Width = 80,
                Minimum = 1000,
                Maximum = 100000,
                Increment = 1000,
                Value = 20000
            };
            parent.Controls.AddRange(new Control[] { lblBitrate, numBitrate });

            y += 35;

            // Move processed checkbox
            chkMoveProcessed = new CheckBox
            {
                Text = "Move originals to processed folder",
                Location = new Point(left, y),
                Width = 300
            };
            parent.Controls.Add(chkMoveProcessed);
        }

        private void CreateBottomSection(ref int y)
        {
            y = 640;

            // YouTube section (collapsed by default)
            CreateYouTubeSection(ref y);

            y += 10;

            // Convert All button
            btnConvertAll = new Button
            {
                Text = "Convert All Selected Videos",
                Location = new Point(10, y),
                Width = 200,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                Enabled = false
            };
            btnConvertAll.Click += BtnConvertAll_Click;
            this.Controls.Add(btnConvertAll);

            y += 45;

            // Progress
            lblProgress = new Label
            {
                Text = "Ready",
                Location = new Point(10, y),
                Width = 1360
            };
            this.Controls.Add(lblProgress);

            y += 20;

            progressBar = new ProgressBar
            {
                Location = new Point(10, y),
                Width = 1360
            };
            this.Controls.Add(progressBar);

            y += 30;

            // Log
            var lblLog = new Label
            {
                Text = "Log:",
                Location = new Point(10, y),
                Width = 100,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            this.Controls.Add(lblLog);

            y += 20;

            txtLog = new RichTextBox
            {
                Location = new Point(10, y),
                Width = 1360,
                Height = 150,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(txtLog);
        }

        private void CreateYouTubeSection(ref int y)
        {
            var grpYouTube = new GroupBox
            {
                Text = "YouTube Upload (Optional)",
                Location = new Point(10, y),
                Width = 1360,
                Height = 140
            };
            this.Controls.Add(grpYouTube);

            int innerY = 20;

            // Enable checkbox
            chkEnableYouTube = new CheckBox
            {
                Text = "Enable YouTube upload after conversion",
                Location = new Point(10, innerY),
                Width = 300
            };
            chkEnableYouTube.CheckedChanged += ChkEnableYouTube_CheckedChanged;
            grpYouTube.Controls.Add(chkEnableYouTube);

            innerY += 25;

            // Title template
            var lblTitle = new Label { Text = "Title:", Location = new Point(10, innerY + 3), Width = 80 };
            txtYouTubeTitleTemplate = new TextBox { Location = new Point(95, innerY), Width = 400, Text = "{filename}" };
            grpYouTube.Controls.AddRange(new Control[] { lblTitle, txtYouTubeTitleTemplate });

            // Tags
            var lblTags = new Label { Text = "Tags:", Location = new Point(510, innerY + 3), Width = 40 };
            txtYouTubeTags = new TextBox { Location = new Point(555, innerY), Width = 250, Text = "gaming,gameplay" };
            grpYouTube.Controls.AddRange(new Control[] { lblTags, txtYouTubeTags });

            // Privacy
            var lblPrivacy = new Label { Text = "Privacy:", Location = new Point(820, innerY + 3), Width = 50 };
            cmbYouTubePrivacy = new ComboBox
            {
                Location = new Point(875, innerY),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbYouTubePrivacy.Items.AddRange(new object[] { "private", "unlisted", "public" });
            cmbYouTubePrivacy.SelectedIndex = 0;
            grpYouTube.Controls.AddRange(new Control[] { lblPrivacy, cmbYouTubePrivacy });

            innerY += 30;

            // Description
            var lblDesc = new Label { Text = "Description:", Location = new Point(10, innerY + 3), Width = 80 };
            txtYouTubeDescriptionTemplate = new TextBox
            {
                Location = new Point(95, innerY),
                Width = 400,
                Height = 40,
                Multiline = true,
                Text = "Converted video: {filename}"
            };
            grpYouTube.Controls.AddRange(new Control[] { lblDesc, txtYouTubeDescriptionTemplate });

            // Category
            var lblCategory = new Label { Text = "Category:", Location = new Point(510, innerY + 3), Width = 60 };
            cmbYouTubeCategory = new ComboBox
            {
                Location = new Point(575, innerY),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbYouTubeCategory.Items.AddRange(new object[]
            {
                "Gaming (20)",
                "Entertainment (24)",
                "People & Blogs (22)"
            });
            cmbYouTubeCategory.SelectedIndex = 0;
            grpYouTube.Controls.AddRange(new Control[] { lblCategory, cmbYouTubeCategory });

            // Auth button
            btnYouTubeAuth = new Button
            {
                Text = "Authenticate",
                Location = new Point(820, innerY),
                Width = 120
            };
            btnYouTubeAuth.Click += BtnYouTubeAuth_Click;
            grpYouTube.Controls.Add(btnYouTubeAuth);

            // Status
            lblYouTubeStatus = new Label
            {
                Text = "Not authenticated",
                Location = new Point(950, innerY + 3),
                Width = 300,
                ForeColor = Color.Gray
            };
            grpYouTube.Controls.Add(lblYouTubeStatus);

            UpdateYouTubeControlsEnabled();

            y += 150;
        }

        // Event handlers and core logic to be continued...
    }
}
