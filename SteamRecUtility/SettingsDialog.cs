using System;
using System.Windows.Forms;

namespace SteamRecUtility
{
    public class SettingsDialog : Form
    {
        private AppSettings settings;

        // Default adjustment controls
        private TrackBar trackDefaultBrightness = null!;
        private TrackBar trackDefaultContrast = null!;
        private TrackBar trackDefaultSaturation = null!;
        private Label lblDefaultBrightnessValue = null!;
        private Label lblDefaultContrastValue = null!;
        private Label lblDefaultSaturationValue = null!;

        // Resolution
        private ComboBox cmbDefaultResolution = null!;
        private NumericUpDown numCustomWidth = null!;
        private NumericUpDown numCustomHeight = null!;

        // Encoder selection
        private ComboBox cmbEncoder = null!;

        // libx265 controls
        private Panel pnlX265Settings = null!;
        private NumericUpDown numX265CRF = null!;
        private ComboBox cmbX265Preset = null!;
        private ComboBox cmbX265Tune = null!;
        private NumericUpDown numX265BFrames = null!;
        private NumericUpDown numX265Lookahead = null!;
        private ComboBox cmbX265BitDepth = null!;

        // hevc_nvenc controls
        private Panel pnlNvencSettings = null!;
        private NumericUpDown numNvencCQ = null!;
        private ComboBox cmbNvencPreset = null!;
        private ComboBox cmbNvencRateControl = null!;
        private CheckBox chkNvencSpatialAQ = null!;
        private CheckBox chkNvencTemporalAQ = null!;
        private NumericUpDown numNvencBFrames = null!;
        private NumericUpDown numNvencLookahead = null!;
        private ComboBox cmbNvencMultipass = null!;
        private ComboBox cmbNvencBitDepth = null!;

        // av1_nvenc controls
        private Panel pnlAv1Settings = null!;
        private NumericUpDown numAv1CQ = null!;
        private ComboBox cmbAv1Preset = null!;
        private ComboBox cmbAv1RateControl = null!;
        private ComboBox cmbAv1Multipass = null!;
        private NumericUpDown numAv1Lookahead = null!;

        // Other settings
        private CheckBox chkMoveProcessed = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Button btnResetDefaults = null!;

        public SettingsDialog(AppSettings settings)
        {
            this.settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(500, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(0, 0, 0, 10);
            this.AutoScroll = true;

            int y = 15;
            int labelWidth = 120;
            int controlLeft = 140;

            // === Default Adjustments Group ===
            var grpAdjustments = new GroupBox
            {
                Text = "Default Video Adjustments",
                Location = new Point(10, y),
                Size = new Size(465, 170)
            };
            this.Controls.Add(grpAdjustments);

            int gy = 25;
            int trackWidth = 180;
            int valueWidth = 50;

            // Brightness
            var lblBrightness = new Label { Text = "Brightness:", Location = new Point(15, gy + 3), Width = labelWidth };
            trackDefaultBrightness = new TrackBar
            {
                Location = new Point(controlLeft, gy),
                Width = trackWidth,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 20
            };
            trackDefaultBrightness.ValueChanged += TrackDefaultBrightness_ValueChanged;

            lblDefaultBrightnessValue = new Label
            {
                Location = new Point(controlLeft + trackWidth + 5, gy + 3),
                Width = valueWidth,
                Text = "0.00"
            };
            grpAdjustments.Controls.AddRange(new Control[] { lblBrightness, trackDefaultBrightness, lblDefaultBrightnessValue });
            gy += 45;

            // Contrast
            var lblContrast = new Label { Text = "Contrast:", Location = new Point(15, gy + 3), Width = labelWidth };
            trackDefaultContrast = new TrackBar
            {
                Location = new Point(controlLeft, gy),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 400,
                Value = 100,
                TickFrequency = 50
            };
            trackDefaultContrast.ValueChanged += TrackDefaultContrast_ValueChanged;

            lblDefaultContrastValue = new Label
            {
                Location = new Point(controlLeft + trackWidth + 5, gy + 3),
                Width = valueWidth,
                Text = "1.00"
            };
            grpAdjustments.Controls.AddRange(new Control[] { lblContrast, trackDefaultContrast, lblDefaultContrastValue });
            gy += 45;

            // Saturation
            var lblSaturation = new Label { Text = "Saturation:", Location = new Point(15, gy + 3), Width = labelWidth };
            trackDefaultSaturation = new TrackBar
            {
                Location = new Point(controlLeft, gy),
                Width = trackWidth,
                Minimum = 0,
                Maximum = 300,
                Value = 120,
                TickFrequency = 30
            };
            trackDefaultSaturation.ValueChanged += TrackDefaultSaturation_ValueChanged;

            lblDefaultSaturationValue = new Label
            {
                Location = new Point(controlLeft + trackWidth + 5, gy + 3),
                Width = valueWidth,
                Text = "1.20"
            };
            grpAdjustments.Controls.AddRange(new Control[] { lblSaturation, trackDefaultSaturation, lblDefaultSaturationValue });

            y += grpAdjustments.Height + 15;

            // === Output Settings Group ===
            var grpOutput = new GroupBox
            {
                Text = "Default Output Settings",
                Location = new Point(10, y),
                Size = new Size(465, 420)
            };
            this.Controls.Add(grpOutput);

            gy = 25;

            // Resolution
            var lblResolution = new Label { Text = "Resolution:", Location = new Point(15, gy + 3), Width = labelWidth };
            cmbDefaultResolution = new ComboBox
            {
                Location = new Point(controlLeft, gy),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDefaultResolution.Items.AddRange(new[] { "1920x1080 (Full HD)", "2560x1440 (2K)", "3840x2160 (4K)", "Custom" });
            cmbDefaultResolution.SelectedIndexChanged += CmbDefaultResolution_SelectedIndexChanged;
            grpOutput.Controls.AddRange(new Control[] { lblResolution, cmbDefaultResolution });
            gy += 30;

            // Custom resolution
            var lblCustomRes = new Label { Text = "Custom Size:", Location = new Point(15, gy + 3), Width = labelWidth };
            numCustomWidth = new NumericUpDown
            {
                Location = new Point(controlLeft, gy),
                Width = 70,
                Minimum = 640,
                Maximum = 7680,
                Value = 1920
            };
            var lblX = new Label { Text = "x", Location = new Point(controlLeft + 75, gy + 3), Width = 15 };
            numCustomHeight = new NumericUpDown
            {
                Location = new Point(controlLeft + 95, gy),
                Width = 70,
                Minimum = 360,
                Maximum = 4320,
                Value = 1080
            };
            grpOutput.Controls.AddRange(new Control[] { lblCustomRes, numCustomWidth, lblX, numCustomHeight });
            gy += 35;

            // Encoder selection
            var lblEncoder = new Label { Text = "Video Encoder:", Location = new Point(15, gy + 3), Width = labelWidth };
            cmbEncoder = new ComboBox
            {
                Location = new Point(controlLeft, gy),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEncoder.Items.AddRange(new[] { "libx265 (CPU)", "hevc_nvenc (GPU HEVC)", "av1_nvenc (GPU AV1)" });
            cmbEncoder.SelectedIndexChanged += CmbEncoder_SelectedIndexChanged;
            grpOutput.Controls.AddRange(new Control[] { lblEncoder, cmbEncoder });
            gy += 35;

            // === libx265 Settings Panel ===
            pnlX265Settings = new Panel
            {
                Location = new Point(10, gy),
                Size = new Size(445, 240),
                Visible = false
            };
            grpOutput.Controls.Add(pnlX265Settings);

            int x265y = 0;

            // CRF
            var lblX265CRF = new Label { Text = "CRF (0-51):", Location = new Point(5, x265y + 3), Width = labelWidth };
            numX265CRF = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 23
            };
            var lblX265CRFHelp = new Label
            {
                Text = "Lower = better quality. 18-28 recommended.",
                Location = new Point(controlLeft + 55, x265y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265CRF, numX265CRF, lblX265CRFHelp });
            x265y += 28;

            // Preset
            var lblX265Preset = new Label { Text = "Preset:", Location = new Point(5, x265y + 3), Width = labelWidth };
            cmbX265Preset = new ComboBox
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbX265Preset.Items.AddRange(new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" });
            var lblX265PresetHelp = new Label
            {
                Text = "Slower = better compression.",
                Location = new Point(controlLeft + 115, x265y + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265Preset, cmbX265Preset, lblX265PresetHelp });
            x265y += 28;

            // Tune
            var lblX265Tune = new Label { Text = "Tune:", Location = new Point(5, x265y + 3), Width = labelWidth };
            cmbX265Tune = new ComboBox
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbX265Tune.Items.AddRange(new[] { "(none)", "film", "animation", "grain", "fastdecode", "zerolatency" });
            var lblX265TuneHelp = new Label
            {
                Text = "Content-specific tuning.",
                Location = new Point(controlLeft + 115, x265y + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265Tune, cmbX265Tune, lblX265TuneHelp });
            x265y += 28;

            // B-Frames
            var lblX265BFrames = new Label { Text = "B-Frames (0-16):", Location = new Point(5, x265y + 3), Width = labelWidth };
            numX265BFrames = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 60,
                Minimum = 0,
                Maximum = 16,
                Value = 3
            };
            var lblX265BFramesHelp = new Label
            {
                Text = "More = better compression, slower.",
                Location = new Point(controlLeft + 55, x265y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265BFrames, numX265BFrames, lblX265BFramesHelp });
            x265y += 28;

            // Lookahead
            var lblX265Lookahead = new Label { Text = "Lookahead (0-250):", Location = new Point(5, x265y + 3), Width = labelWidth };
            numX265Lookahead = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 60,
                Minimum = 0,
                Maximum = 250,
                Value = 0
            };
            var lblX265LookaheadHelp = new Label
            {
                Text = "0 = disabled. Higher = better quality.",
                Location = new Point(controlLeft + 55, x265y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265Lookahead, numX265Lookahead, lblX265LookaheadHelp });
            x265y += 28;

            // Bit Depth
            var lblX265BitDepth = new Label { Text = "Bit Depth:", Location = new Point(5, x265y + 3), Width = labelWidth };
            cmbX265BitDepth = new ComboBox
            {
                Location = new Point(controlLeft - 10, x265y),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbX265BitDepth.Items.AddRange(new[] { "8-bit", "10-bit" });
            var lblX265BitDepthHelp = new Label
            {
                Text = "10-bit = better gradients, larger files.",
                Location = new Point(controlLeft + 75, x265y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265BitDepth, cmbX265BitDepth, lblX265BitDepthHelp });

            // === hevc_nvenc Settings Panel ===
            pnlNvencSettings = new Panel
            {
                Location = new Point(10, gy),
                Size = new Size(445, 280),
                Visible = false
            };
            grpOutput.Controls.Add(pnlNvencSettings);

            int nvency = 0;

            // CQ
            var lblNvencCQ = new Label { Text = "CQ Level (0-51):", Location = new Point(5, nvency + 3), Width = labelWidth };
            numNvencCQ = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 21
            };
            var lblNvencCQHelp = new Label
            {
                Text = "Lower = better quality. 19-23 recommended.",
                Location = new Point(controlLeft + 55, nvency + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencCQ, numNvencCQ, lblNvencCQHelp });
            nvency += 28;

            // Preset (modern p1-p7 naming)
            var lblNvencPreset = new Label { Text = "Preset:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencPreset = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencPreset.Items.AddRange(new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            var lblNvencPresetHelp = new Label
            {
                Text = "p1=fastest, p7=best quality. p5 recommended.",
                Location = new Point(controlLeft + 75, nvency + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencPreset, cmbNvencPreset, lblNvencPresetHelp });
            nvency += 28;

            // Rate Control
            var lblNvencRC = new Label { Text = "Rate Control:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencRateControl = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencRateControl.Items.AddRange(new[] { "constqp", "vbr", "cbr" });
            var lblNvencRCHelp = new Label
            {
                Text = "constqp = constant quality.",
                Location = new Point(controlLeft + 95, nvency + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencRC, cmbNvencRateControl, lblNvencRCHelp });
            nvency += 28;

            // B-Frames
            var lblNvencBFrames = new Label { Text = "B-Frames (0-4):", Location = new Point(5, nvency + 3), Width = labelWidth };
            numNvencBFrames = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 60,
                Minimum = 0,
                Maximum = 4,
                Value = 0
            };
            var lblNvencBFramesHelp = new Label
            {
                Text = "0 = disabled. Higher = better compression.",
                Location = new Point(controlLeft + 55, nvency + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencBFrames, numNvencBFrames, lblNvencBFramesHelp });
            nvency += 28;

            // Lookahead
            var lblNvencLookahead = new Label { Text = "Lookahead (0-32):", Location = new Point(5, nvency + 3), Width = labelWidth };
            numNvencLookahead = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 60,
                Minimum = 0,
                Maximum = 32,
                Value = 0
            };
            var lblNvencLookaheadHelp = new Label
            {
                Text = "0 = disabled. Higher = better quality.",
                Location = new Point(controlLeft + 55, nvency + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencLookahead, numNvencLookahead, lblNvencLookaheadHelp });
            nvency += 28;

            // Multipass
            var lblNvencMultipass = new Label { Text = "Multipass:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencMultipass = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencMultipass.Items.AddRange(new[] { "disabled", "qres", "fullres" });
            var lblNvencMultipassHelp = new Label
            {
                Text = "fullres = best quality, slower.",
                Location = new Point(controlLeft + 95, nvency + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencMultipass, cmbNvencMultipass, lblNvencMultipassHelp });
            nvency += 28;

            // Bit Depth
            var lblNvencBitDepth = new Label { Text = "Bit Depth:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencBitDepth = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencBitDepth.Items.AddRange(new[] { "8-bit", "10-bit" });
            var lblNvencBitDepthHelp = new Label
            {
                Text = "10-bit = better gradients.",
                Location = new Point(controlLeft + 75, nvency + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencBitDepth, cmbNvencBitDepth, lblNvencBitDepthHelp });
            nvency += 28;

            // Adaptive Quantization checkboxes
            chkNvencSpatialAQ = new CheckBox
            {
                Text = "Spatial AQ (improves quality)",
                Location = new Point(controlLeft - 10, nvency),
                Width = 200,
                Checked = true
            };
            chkNvencTemporalAQ = new CheckBox
            {
                Text = "Temporal AQ (motion)",
                Location = new Point(controlLeft + 195, nvency),
                Width = 180,
                Checked = true
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { chkNvencSpatialAQ, chkNvencTemporalAQ });

            // === av1_nvenc Settings Panel ===
            pnlAv1Settings = new Panel
            {
                Location = new Point(10, gy),
                Size = new Size(445, 170),
                Visible = false
            };
            grpOutput.Controls.Add(pnlAv1Settings);

            int av1y = 0;

            // CQ
            var lblAv1CQ = new Label { Text = "CQ Level (0-51):", Location = new Point(5, av1y + 3), Width = labelWidth };
            numAv1CQ = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, av1y),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 23
            };
            var lblAv1CQHelp = new Label
            {
                Text = "Lower = better quality. 19-26 recommended.",
                Location = new Point(controlLeft + 55, av1y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlAv1Settings.Controls.AddRange(new Control[] { lblAv1CQ, numAv1CQ, lblAv1CQHelp });
            av1y += 28;

            // Preset (p1-p7)
            var lblAv1Preset = new Label { Text = "Preset:", Location = new Point(5, av1y + 3), Width = labelWidth };
            cmbAv1Preset = new ComboBox
            {
                Location = new Point(controlLeft - 10, av1y),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAv1Preset.Items.AddRange(new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7" });
            var lblAv1PresetHelp = new Label
            {
                Text = "p1=fastest, p7=best quality. p5 recommended.",
                Location = new Point(controlLeft + 75, av1y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlAv1Settings.Controls.AddRange(new Control[] { lblAv1Preset, cmbAv1Preset, lblAv1PresetHelp });
            av1y += 28;

            // Rate Control
            var lblAv1RC = new Label { Text = "Rate Control:", Location = new Point(5, av1y + 3), Width = labelWidth };
            cmbAv1RateControl = new ComboBox
            {
                Location = new Point(controlLeft - 10, av1y),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAv1RateControl.Items.AddRange(new[] { "constqp", "vbr", "cbr" });
            var lblAv1RCHelp = new Label
            {
                Text = "constqp = constant quality.",
                Location = new Point(controlLeft + 95, av1y + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlAv1Settings.Controls.AddRange(new Control[] { lblAv1RC, cmbAv1RateControl, lblAv1RCHelp });
            av1y += 28;

            // Multipass
            var lblAv1Multipass = new Label { Text = "Multipass:", Location = new Point(5, av1y + 3), Width = labelWidth };
            cmbAv1Multipass = new ComboBox
            {
                Location = new Point(controlLeft - 10, av1y),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAv1Multipass.Items.AddRange(new[] { "disabled", "qres", "fullres" });
            var lblAv1MultipassHelp = new Label
            {
                Text = "fullres = best quality, slower.",
                Location = new Point(controlLeft + 95, av1y + 3),
                Width = 200,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlAv1Settings.Controls.AddRange(new Control[] { lblAv1Multipass, cmbAv1Multipass, lblAv1MultipassHelp });
            av1y += 28;

            // Lookahead
            var lblAv1Lookahead = new Label { Text = "Lookahead (0-32):", Location = new Point(5, av1y + 3), Width = labelWidth };
            numAv1Lookahead = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, av1y),
                Width = 60,
                Minimum = 0,
                Maximum = 32,
                Value = 0
            };
            var lblAv1LookaheadHelp = new Label
            {
                Text = "0 = disabled. Higher = better quality.",
                Location = new Point(controlLeft + 55, av1y + 3),
                Width = 250,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlAv1Settings.Controls.AddRange(new Control[] { lblAv1Lookahead, numAv1Lookahead, lblAv1LookaheadHelp });

            y += grpOutput.Height + 15;

            // === File Handling ===
            var grpFiles = new GroupBox
            {
                Text = "File Handling",
                Location = new Point(10, y),
                Size = new Size(465, 55)
            };
            this.Controls.Add(grpFiles);

            chkMoveProcessed = new CheckBox
            {
                Text = "Move original files to processed folder after conversion",
                Location = new Point(15, 22),
                Width = 400,
                Checked = true
            };
            grpFiles.Controls.Add(chkMoveProcessed);

            y += grpFiles.Height + 20;

            // Buttons
            btnResetDefaults = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(10, y),
                Width = 110,
                Height = 30
            };
            btnResetDefaults.Click += BtnResetDefaults_Click;

            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(290, y),
                Width = 85,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(385, y),
                Width = 85,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { btnResetDefaults, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadSettings()
        {
            // Adjustments
            trackDefaultBrightness.Value = (int)(settings.Brightness * 100);
            trackDefaultContrast.Value = (int)(settings.Contrast * 100);
            trackDefaultSaturation.Value = (int)(settings.Saturation * 100);
            UpdateAdjustmentLabels();

            // Resolution
            string resString = $"{settings.OutputWidth}x{settings.OutputHeight}";
            int resIndex = resString switch
            {
                "1920x1080" => 0,
                "2560x1440" => 1,
                "3840x2160" => 2,
                _ => 3 // Custom
            };
            cmbDefaultResolution.SelectedIndex = resIndex;
            numCustomWidth.Value = settings.OutputWidth;
            numCustomHeight.Value = settings.OutputHeight;
            UpdateCustomResolutionEnabled();

            // Encoder
            cmbEncoder.SelectedIndex = settings.VideoEncoder switch
            {
                "libx265" => 0,
                "hevc_nvenc" => 1,
                "av1_nvenc" => 2,
                _ => 0
            };

            // libx265 settings
            numX265CRF.Value = settings.X265CRF;
            cmbX265Preset.SelectedItem = settings.X265Preset;
            cmbX265Tune.SelectedItem = string.IsNullOrEmpty(settings.X265Tune) ? "(none)" : settings.X265Tune;
            numX265BFrames.Value = settings.X265BFrames;
            numX265Lookahead.Value = settings.X265Lookahead;
            cmbX265BitDepth.SelectedIndex = settings.X265BitDepth == 10 ? 1 : 0;

            // hevc_nvenc settings
            numNvencCQ.Value = settings.NvencCQ;
            cmbNvencPreset.SelectedItem = settings.NvencPreset;
            cmbNvencRateControl.SelectedItem = settings.NvencRateControl;
            chkNvencSpatialAQ.Checked = settings.NvencSpatialAQ;
            chkNvencTemporalAQ.Checked = settings.NvencTemporalAQ;
            numNvencBFrames.Value = settings.NvencBFrames;
            numNvencLookahead.Value = settings.NvencLookahead;
            cmbNvencMultipass.SelectedItem = settings.NvencMultipass;
            cmbNvencBitDepth.SelectedIndex = settings.NvencBitDepth == 10 ? 1 : 0;

            // av1_nvenc settings
            numAv1CQ.Value = settings.Av1CQ;
            cmbAv1Preset.SelectedItem = settings.Av1Preset;
            cmbAv1RateControl.SelectedItem = settings.Av1RateControl;
            cmbAv1Multipass.SelectedItem = settings.Av1Multipass;
            numAv1Lookahead.Value = settings.Av1Lookahead;

            UpdateEncoderPanels();

            // File handling
            chkMoveProcessed.Checked = settings.MoveProcessedFiles;
        }

        private void UpdateAdjustmentLabels()
        {
            lblDefaultBrightnessValue.Text = (trackDefaultBrightness.Value / 100.0).ToString("0.00");
            lblDefaultContrastValue.Text = (trackDefaultContrast.Value / 100.0).ToString("0.00");
            lblDefaultSaturationValue.Text = (trackDefaultSaturation.Value / 100.0).ToString("0.00");
        }

        private void UpdateCustomResolutionEnabled()
        {
            bool isCustom = cmbDefaultResolution.SelectedIndex == 3;
            numCustomWidth.Enabled = isCustom;
            numCustomHeight.Enabled = isCustom;
        }

        private void UpdateEncoderPanels()
        {
            int selectedIndex = cmbEncoder.SelectedIndex;
            pnlX265Settings.Visible = selectedIndex == 0;
            pnlNvencSettings.Visible = selectedIndex == 1;
            pnlAv1Settings.Visible = selectedIndex == 2;
        }

        private void TrackDefaultBrightness_ValueChanged(object? sender, EventArgs e)
        {
            UpdateAdjustmentLabels();
        }

        private void TrackDefaultContrast_ValueChanged(object? sender, EventArgs e)
        {
            UpdateAdjustmentLabels();
        }

        private void TrackDefaultSaturation_ValueChanged(object? sender, EventArgs e)
        {
            UpdateAdjustmentLabels();
        }

        private void CmbDefaultResolution_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateCustomResolutionEnabled();

            // Update custom values when preset selected
            switch (cmbDefaultResolution.SelectedIndex)
            {
                case 0: // 1920x1080
                    numCustomWidth.Value = 1920;
                    numCustomHeight.Value = 1080;
                    break;
                case 1: // 2560x1440
                    numCustomWidth.Value = 2560;
                    numCustomHeight.Value = 1440;
                    break;
                case 2: // 3840x2160
                    numCustomWidth.Value = 3840;
                    numCustomHeight.Value = 2160;
                    break;
            }
        }

        private void CmbEncoder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateEncoderPanels();
        }

        private void BtnResetDefaults_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Reset all settings to default values?", "Reset Defaults",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                trackDefaultBrightness.Value = 0;
                trackDefaultContrast.Value = 100;
                trackDefaultSaturation.Value = 120;
                cmbDefaultResolution.SelectedIndex = 0;
                numCustomWidth.Value = 1920;
                numCustomHeight.Value = 1080;
                cmbEncoder.SelectedIndex = 0; // libx265

                // libx265 defaults
                numX265CRF.Value = 23;
                cmbX265Preset.SelectedItem = "medium";
                cmbX265Tune.SelectedItem = "(none)";
                numX265BFrames.Value = 3;
                numX265Lookahead.Value = 0;
                cmbX265BitDepth.SelectedIndex = 0; // 8-bit

                // hevc_nvenc defaults
                numNvencCQ.Value = 21;
                cmbNvencPreset.SelectedItem = "p5";
                cmbNvencRateControl.SelectedItem = "constqp";
                chkNvencSpatialAQ.Checked = true;
                chkNvencTemporalAQ.Checked = true;
                numNvencBFrames.Value = 0;
                numNvencLookahead.Value = 0;
                cmbNvencMultipass.SelectedItem = "disabled";
                cmbNvencBitDepth.SelectedIndex = 0; // 8-bit

                // av1_nvenc defaults
                numAv1CQ.Value = 23;
                cmbAv1Preset.SelectedItem = "p5";
                cmbAv1RateControl.SelectedItem = "constqp";
                cmbAv1Multipass.SelectedItem = "disabled";
                numAv1Lookahead.Value = 0;

                chkMoveProcessed.Checked = true;
                UpdateAdjustmentLabels();
                UpdateEncoderPanels();
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Save settings
            settings.Brightness = trackDefaultBrightness.Value / 100.0;
            settings.Contrast = trackDefaultContrast.Value / 100.0;
            settings.Saturation = trackDefaultSaturation.Value / 100.0;
            settings.OutputWidth = (int)numCustomWidth.Value;
            settings.OutputHeight = (int)numCustomHeight.Value;

            // Encoder
            settings.VideoEncoder = cmbEncoder.SelectedIndex switch
            {
                0 => "libx265",
                1 => "hevc_nvenc",
                2 => "av1_nvenc",
                _ => "libx265"
            };

            // libx265 settings
            settings.X265CRF = (int)numX265CRF.Value;
            settings.X265Preset = cmbX265Preset.SelectedItem?.ToString() ?? "medium";
            settings.X265Tune = cmbX265Tune.SelectedItem?.ToString() == "(none)" ? "" : cmbX265Tune.SelectedItem?.ToString() ?? "";
            settings.X265BFrames = (int)numX265BFrames.Value;
            settings.X265Lookahead = (int)numX265Lookahead.Value;
            settings.X265BitDepth = cmbX265BitDepth.SelectedIndex == 1 ? 10 : 8;

            // hevc_nvenc settings
            settings.NvencCQ = (int)numNvencCQ.Value;
            settings.NvencPreset = cmbNvencPreset.SelectedItem?.ToString() ?? "p5";
            settings.NvencRateControl = cmbNvencRateControl.SelectedItem?.ToString() ?? "constqp";
            settings.NvencSpatialAQ = chkNvencSpatialAQ.Checked;
            settings.NvencTemporalAQ = chkNvencTemporalAQ.Checked;
            settings.NvencBFrames = (int)numNvencBFrames.Value;
            settings.NvencLookahead = (int)numNvencLookahead.Value;
            settings.NvencMultipass = cmbNvencMultipass.SelectedItem?.ToString() ?? "disabled";
            settings.NvencBitDepth = cmbNvencBitDepth.SelectedIndex == 1 ? 10 : 8;

            // av1_nvenc settings
            settings.Av1CQ = (int)numAv1CQ.Value;
            settings.Av1Preset = cmbAv1Preset.SelectedItem?.ToString() ?? "p5";
            settings.Av1RateControl = cmbAv1RateControl.SelectedItem?.ToString() ?? "constqp";
            settings.Av1Multipass = cmbAv1Multipass.SelectedItem?.ToString() ?? "disabled";
            settings.Av1Lookahead = (int)numAv1Lookahead.Value;

            settings.MoveProcessedFiles = chkMoveProcessed.Checked;

            settings.Save();
        }
    }
}
