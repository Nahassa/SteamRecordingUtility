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

        // Resolution and scaling
        private ComboBox cmbDefaultResolution = null!;
        private NumericUpDown numCustomWidth = null!;
        private NumericUpDown numCustomHeight = null!;
        private ComboBox cmbScalingMode = null!;

        // Encoder selection
        private ComboBox cmbEncoder = null!;

        // libx265 controls
        private Panel pnlX265Settings = null!;
        private NumericUpDown numX265CRF = null!;
        private ComboBox cmbX265Preset = null!;
        private ComboBox cmbX265Tune = null!;

        // NVENC controls (hevc_nvenc / av1_nvenc)
        private Panel pnlNvencSettings = null!;
        private NumericUpDown numNvencCQ = null!;
        private ComboBox cmbNvencPreset = null!;
        private ComboBox cmbNvencTune = null!;
        private ComboBox cmbNvencRateControl = null!;
        private ComboBox cmbNvencMultipass = null!;
        private NumericUpDown numNvencBFrames = null!;
        private CheckBox chkNvencSpatialAQ = null!;
        private CheckBox chkNvencTemporalAQ = null!;

        // UHQ mode controls
        private CheckBox chkNvencUHQ = null!;
        private NumericUpDown numUHQBitrate = null!;
        private NumericUpDown numUHQMaxrate = null!;
        private NumericUpDown numUHQRcLookahead = null!;
        private Panel pnlUHQSettings = null!;

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
            this.Size = new Size(500, 850);
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
                Size = new Size(465, 530)
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

            // Scaling mode
            var lblScalingMode = new Label { Text = "Scaling Mode:", Location = new Point(15, gy + 3), Width = labelWidth };
            cmbScalingMode = new ComboBox
            {
                Location = new Point(controlLeft, gy),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbScalingMode.Items.AddRange(new[] { "SAR (preserve pixels, tag for 16:9)", "Scale (resample pixels to target)" });
            var lblScalingModeHelp = new Label
            {
                Text = "SAR preserves original quality. Scale resamples to exact resolution.",
                Location = new Point(controlLeft, gy + 25),
                Width = 300,
                Height = 16,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            grpOutput.Controls.AddRange(new Control[] { lblScalingMode, cmbScalingMode, lblScalingModeHelp });
            gy += 48;

            // Encoder selection
            var lblEncoder = new Label { Text = "Video Encoder:", Location = new Point(15, gy + 3), Width = labelWidth };
            cmbEncoder = new ComboBox
            {
                Location = new Point(controlLeft, gy),
                Width = 200,
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
                Size = new Size(445, 170),
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
                Text = "Lower = better quality, larger file. 18-28 recommended.",
                Location = new Point(controlLeft - 10, x265y + 25),
                Width = 300,
                Height = 30,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265CRF, numX265CRF, lblX265CRFHelp });
            x265y += 55;

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
                Text = "Slower = better compression. medium recommended.",
                Location = new Point(controlLeft - 10, x265y + 25),
                Width = 300,
                Height = 30,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265Preset, cmbX265Preset, lblX265PresetHelp });
            x265y += 55;

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
                Text = "Optional tuning for specific content types.",
                Location = new Point(controlLeft - 10, x265y + 25),
                Width = 300,
                Height = 30,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlX265Settings.Controls.AddRange(new Control[] { lblX265Tune, cmbX265Tune, lblX265TuneHelp });

            // === NVENC Settings Panel (shared by hevc_nvenc and av1_nvenc) ===
            pnlNvencSettings = new Panel
            {
                Location = new Point(10, gy),
                Size = new Size(445, 400),
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
                Location = new Point(controlLeft - 10, nvency + 25),
                Width = 300,
                Height = 16,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(this.Font.FontFamily, 7.5f)
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencCQ, numNvencCQ, lblNvencCQHelp });
            nvency += 45;

            // Preset (modern p1-p7)
            var lblNvencPreset = new Label { Text = "Preset:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencPreset = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencPreset.Items.AddRange(new[] { "p1 (Fastest)", "p2", "p3", "p4", "p5", "p6", "p7 (Best Quality)" });
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencPreset, cmbNvencPreset });
            nvency += 30;

            // Tune
            var lblNvencTune = new Label { Text = "Tune:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencTune = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencTune.Items.AddRange(new[] { "hq (High Quality)", "ll (Low Latency)", "ull (Ultra Low Latency)", "lossless" });
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencTune, cmbNvencTune });
            nvency += 30;

            // Rate Control
            var lblNvencRC = new Label { Text = "Rate Control:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencRateControl = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencRateControl.Items.AddRange(new[] { "constqp", "vbr", "cbr" });
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencRC, cmbNvencRateControl });
            nvency += 30;

            // Multipass
            var lblNvencMultipass = new Label { Text = "Multipass:", Location = new Point(5, nvency + 3), Width = labelWidth };
            cmbNvencMultipass = new ComboBox
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbNvencMultipass.Items.AddRange(new[] { "disabled", "qres (Quarter Res)", "fullres (Full Res)" });
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencMultipass, cmbNvencMultipass });
            nvency += 30;

            // B-Frames
            var lblNvencBFrames = new Label { Text = "B-Frames (0-4):", Location = new Point(5, nvency + 3), Width = labelWidth };
            numNvencBFrames = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, nvency),
                Width = 60,
                Minimum = 0,
                Maximum = 4,
                Value = 3
            };
            pnlNvencSettings.Controls.AddRange(new Control[] { lblNvencBFrames, numNvencBFrames });
            nvency += 30;

            // Adaptive Quantization checkboxes
            chkNvencSpatialAQ = new CheckBox
            {
                Text = "Spatial AQ (improves quality in flat areas)",
                Location = new Point(controlLeft - 10, nvency),
                Width = 280,
                Checked = true
            };
            pnlNvencSettings.Controls.Add(chkNvencSpatialAQ);
            nvency += 25;

            chkNvencTemporalAQ = new CheckBox
            {
                Text = "Temporal AQ (improves motion quality)",
                Location = new Point(controlLeft - 10, nvency),
                Width = 280,
                Checked = true
            };
            pnlNvencSettings.Controls.Add(chkNvencTemporalAQ);
            nvency += 30;

            // === Ultra High Quality Mode ===
            chkNvencUHQ = new CheckBox
            {
                Text = "Ultra High Quality (av1_nvenc UHQ)",
                Location = new Point(5, nvency),
                Width = 300,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold),
                Checked = false
            };
            chkNvencUHQ.CheckedChanged += ChkNvencUHQ_CheckedChanged;
            pnlNvencSettings.Controls.Add(chkNvencUHQ);
            nvency += 25;

            pnlUHQSettings = new Panel
            {
                Location = new Point(0, nvency),
                Size = new Size(445, 75),
                Visible = false
            };
            pnlNvencSettings.Controls.Add(pnlUHQSettings);

            int uhqy = 0;
            var lblUHQBitrate = new Label { Text = "Bitrate (Mbps):", Location = new Point(5, uhqy + 3), Width = labelWidth };
            numUHQBitrate = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, uhqy),
                Width = 60,
                Minimum = 1,
                Maximum = 100,
                Value = 15
            };
            pnlUHQSettings.Controls.AddRange(new Control[] { lblUHQBitrate, numUHQBitrate });
            uhqy += 28;

            var lblUHQMaxrate = new Label { Text = "Max Rate (Mbps):", Location = new Point(5, uhqy + 3), Width = labelWidth };
            numUHQMaxrate = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, uhqy),
                Width = 60,
                Minimum = 1,
                Maximum = 200,
                Value = 30
            };
            pnlUHQSettings.Controls.AddRange(new Control[] { lblUHQMaxrate, numUHQMaxrate });
            uhqy += 28;

            var lblUHQLookahead = new Label { Text = "RC Lookahead:", Location = new Point(5, uhqy + 3), Width = labelWidth };
            numUHQRcLookahead = new NumericUpDown
            {
                Location = new Point(controlLeft - 10, uhqy),
                Width = 60,
                Minimum = 0,
                Maximum = 64,
                Value = 32
            };
            pnlUHQSettings.Controls.AddRange(new Control[] { lblUHQLookahead, numUHQRcLookahead });

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

            // Scaling mode
            cmbScalingMode.SelectedIndex = settings.ScalingMode == "sar" ? 0 : 1;

            // Encoder
            cmbEncoder.SelectedIndex = settings.VideoEncoder switch
            {
                "libx265" => 0,
                "hevc_nvenc" => 1,
                "av1_nvenc" => 2,
                _ => 1
            };

            // libx265 settings
            numX265CRF.Value = settings.X265CRF;
            cmbX265Preset.SelectedItem = settings.X265Preset;
            cmbX265Tune.SelectedItem = string.IsNullOrEmpty(settings.X265Tune) ? "(none)" : settings.X265Tune;

            // NVENC settings
            numNvencCQ.Value = settings.NvencCQ;
            SelectNvencPreset(settings.NvencPreset);
            SelectNvencTune(settings.NvencTune);
            cmbNvencRateControl.SelectedItem = settings.NvencRateControl;
            SelectNvencMultipass(settings.NvencMultipass);
            numNvencBFrames.Value = settings.NvencBFrames;
            chkNvencSpatialAQ.Checked = settings.NvencSpatialAQ;
            chkNvencTemporalAQ.Checked = settings.NvencTemporalAQ;

            // UHQ mode
            chkNvencUHQ.Checked = settings.NvencUHQMode;
            numUHQBitrate.Value = settings.NvencUHQBitrate;
            numUHQMaxrate.Value = settings.NvencUHQMaxrate;
            numUHQRcLookahead.Value = settings.NvencUHQRcLookahead;
            pnlUHQSettings.Visible = settings.NvencUHQMode;

            UpdateEncoderPanels();

            // File handling
            chkMoveProcessed.Checked = settings.MoveProcessedFiles;
        }

        /// <summary>
        /// Select the NVENC preset dropdown item by the raw preset value (e.g. "p7")
        /// </summary>
        private void SelectNvencPreset(string preset)
        {
            for (int i = 0; i < cmbNvencPreset.Items.Count; i++)
            {
                string item = cmbNvencPreset.Items[i]?.ToString() ?? "";
                if (item.StartsWith(preset, StringComparison.OrdinalIgnoreCase))
                {
                    cmbNvencPreset.SelectedIndex = i;
                    return;
                }
            }
            cmbNvencPreset.SelectedIndex = 6; // Default to p7
        }

        /// <summary>
        /// Select the NVENC tune dropdown item by the raw tune value (e.g. "hq")
        /// </summary>
        private void SelectNvencTune(string tune)
        {
            for (int i = 0; i < cmbNvencTune.Items.Count; i++)
            {
                string item = cmbNvencTune.Items[i]?.ToString() ?? "";
                if (item.StartsWith(tune, StringComparison.OrdinalIgnoreCase))
                {
                    cmbNvencTune.SelectedIndex = i;
                    return;
                }
            }
            cmbNvencTune.SelectedIndex = 0; // Default to hq
        }

        /// <summary>
        /// Select the NVENC multipass dropdown item by the raw value (e.g. "fullres")
        /// </summary>
        private void SelectNvencMultipass(string multipass)
        {
            for (int i = 0; i < cmbNvencMultipass.Items.Count; i++)
            {
                string item = cmbNvencMultipass.Items[i]?.ToString() ?? "";
                if (item.StartsWith(multipass, StringComparison.OrdinalIgnoreCase))
                {
                    cmbNvencMultipass.SelectedIndex = i;
                    return;
                }
            }
            cmbNvencMultipass.SelectedIndex = 2; // Default to fullres
        }

        /// <summary>
        /// Extract the raw value from a dropdown item like "p7 (Best Quality)" → "p7"
        /// </summary>
        private static string ExtractDropdownValue(string? item, string fallback)
        {
            if (string.IsNullOrEmpty(item)) return fallback;
            // Take everything before the first space (or parenthesis)
            int spaceIdx = item.IndexOf(' ');
            return spaceIdx > 0 ? item.Substring(0, spaceIdx) : item;
        }

        private void ChkNvencUHQ_CheckedChanged(object? sender, EventArgs e)
        {
            pnlUHQSettings.Visible = chkNvencUHQ.Checked;
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
            bool isX265 = cmbEncoder.SelectedIndex == 0;
            pnlX265Settings.Visible = isX265;
            pnlNvencSettings.Visible = !isX265;
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
                cmbScalingMode.SelectedIndex = 0; // SAR
                cmbEncoder.SelectedIndex = 1; // hevc_nvenc

                // libx265 defaults
                numX265CRF.Value = 23;
                cmbX265Preset.SelectedItem = "medium";
                cmbX265Tune.SelectedItem = "(none)";

                // NVENC defaults
                numNvencCQ.Value = 21;
                SelectNvencPreset("p7");
                SelectNvencTune("hq");
                cmbNvencRateControl.SelectedItem = "constqp";
                SelectNvencMultipass("fullres");
                numNvencBFrames.Value = 3;
                chkNvencSpatialAQ.Checked = true;
                chkNvencTemporalAQ.Checked = true;

                // UHQ defaults
                chkNvencUHQ.Checked = false;
                numUHQBitrate.Value = 15;
                numUHQMaxrate.Value = 30;
                numUHQRcLookahead.Value = 32;
                pnlUHQSettings.Visible = false;

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

            // Scaling mode
            settings.ScalingMode = cmbScalingMode.SelectedIndex == 0 ? "sar" : "scale";

            // Encoder
            settings.VideoEncoder = cmbEncoder.SelectedIndex switch
            {
                0 => "libx265",
                1 => "hevc_nvenc",
                2 => "av1_nvenc",
                _ => "hevc_nvenc"
            };

            // libx265 settings
            settings.X265CRF = (int)numX265CRF.Value;
            settings.X265Preset = cmbX265Preset.SelectedItem?.ToString() ?? "medium";
            settings.X265Tune = cmbX265Tune.SelectedItem?.ToString() == "(none)" ? "" : cmbX265Tune.SelectedItem?.ToString() ?? "";

            // NVENC settings
            settings.NvencCQ = (int)numNvencCQ.Value;
            settings.NvencPreset = ExtractDropdownValue(cmbNvencPreset.SelectedItem?.ToString(), "p7");
            settings.NvencTune = ExtractDropdownValue(cmbNvencTune.SelectedItem?.ToString(), "hq");
            settings.NvencRateControl = cmbNvencRateControl.SelectedItem?.ToString() ?? "constqp";
            settings.NvencMultipass = ExtractDropdownValue(cmbNvencMultipass.SelectedItem?.ToString(), "fullres");
            settings.NvencBFrames = (int)numNvencBFrames.Value;
            settings.NvencSpatialAQ = chkNvencSpatialAQ.Checked;
            settings.NvencTemporalAQ = chkNvencTemporalAQ.Checked;

            // UHQ mode
            settings.NvencUHQMode = chkNvencUHQ.Checked;
            settings.NvencUHQBitrate = (int)numUHQBitrate.Value;
            settings.NvencUHQMaxrate = (int)numUHQMaxrate.Value;
            settings.NvencUHQRcLookahead = (int)numUHQRcLookahead.Value;

            settings.MoveProcessedFiles = chkMoveProcessed.Checked;

            settings.Save();
        }
    }
}
