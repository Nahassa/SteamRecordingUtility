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

        // Resolution and quality
        private ComboBox cmbDefaultResolution = null!;
        private NumericUpDown numCustomWidth = null!;
        private NumericUpDown numCustomHeight = null!;
        private NumericUpDown numDefaultCRF = null!;
        private NumericUpDown numDefaultBitrate = null!;

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
            this.Size = new Size(500, 510);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(0, 0, 0, 10);

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
                Size = new Size(465, 130)
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
            gy += 30;

            // CRF and Bitrate
            var lblCRF = new Label { Text = "CRF (0-51):", Location = new Point(15, gy + 3), Width = labelWidth };
            numDefaultCRF = new NumericUpDown
            {
                Location = new Point(controlLeft, gy),
                Width = 60,
                Minimum = 0,
                Maximum = 51,
                Value = 18
            };

            var lblBitrate = new Label { Text = "Bitrate:", Location = new Point(230, gy + 3), Width = 50 };
            numDefaultBitrate = new NumericUpDown
            {
                Location = new Point(285, gy),
                Width = 80,
                Minimum = 1000,
                Maximum = 100000,
                Value = 20000,
                Increment = 1000
            };
            var lblKbps = new Label { Text = "kbps", Location = new Point(370, gy + 3), Width = 40 };
            grpOutput.Controls.AddRange(new Control[] { lblCRF, numDefaultCRF, lblBitrate, numDefaultBitrate, lblKbps });

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

            // Quality
            numDefaultCRF.Value = settings.CRF;
            numDefaultBitrate.Value = settings.Bitrate;

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
                numDefaultCRF.Value = 18;
                numDefaultBitrate.Value = 20000;
                chkMoveProcessed.Checked = true;
                UpdateAdjustmentLabels();
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
            settings.CRF = (int)numDefaultCRF.Value;
            settings.Bitrate = (int)numDefaultBitrate.Value;
            settings.MoveProcessedFiles = chkMoveProcessed.Checked;

            settings.Save();
        }
    }
}
