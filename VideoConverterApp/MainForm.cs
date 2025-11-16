using System.Diagnostics;

namespace VideoConverterApp
{
    public class MainForm : Form
    {
        private AppSettings settings;

        // UI Controls - Initialized in InitializeComponent()
        private TextBox txtInputFolder = null!;
        private TextBox txtOutputFolder = null!;
        private Button btnBrowseInput = null!;
        private Button btnBrowseOutput = null!;
        private ComboBox cmbResolution = null!;
        private TextBox txtCustomWidth = null!;
        private TextBox txtCustomHeight = null!;
        private NumericUpDown numSaturation = null!;
        private NumericUpDown numCRF = null!;
        private NumericUpDown numBitrate = null!;
        private CheckBox chkMoveProcessed = null!;
        private Button btnConvert = null!;
        private RichTextBox txtLog = null!;
        private ProgressBar progressBar = null!;
        private Label lblProgress = null!;

        public MainForm()
        {
            settings = AppSettings.Load();
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Steam Recording Video Converter";
            this.Size = new Size(800, 700);
            this.MinimumSize = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;
            int labelWidth = 120;
            int controlLeft = labelWidth + 30;

            // Input Folder
            AddLabel("Input Folder:", 20, y);
            txtInputFolder = new TextBox { Location = new Point(controlLeft, y), Width = 460 };
            btnBrowseInput = new Button { Text = "...", Location = new Point(controlLeft + 470, y - 2), Width = 30 };
            btnBrowseInput.Click += BtnBrowseInput_Click;
            this.Controls.Add(txtInputFolder);
            this.Controls.Add(btnBrowseInput);

            y += 35;

            // Output Folder
            AddLabel("Output Folder:", 20, y);
            txtOutputFolder = new TextBox { Location = new Point(controlLeft, y), Width = 460 };
            btnBrowseOutput = new Button { Text = "...", Location = new Point(controlLeft + 470, y - 2), Width = 30 };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            this.Controls.Add(txtOutputFolder);
            this.Controls.Add(btnBrowseOutput);

            y += 40;

            // Resolution
            AddLabel("Resolution:", 20, y);
            cmbResolution = new ComboBox
            {
                Location = new Point(controlLeft, y),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbResolution.Items.AddRange(new object[]
            {
                "1920x1080 (Full HD)",
                "2560x1440 (2K)",
                "3840x2160 (4K)",
                "Custom"
            });
            cmbResolution.SelectedIndexChanged += CmbResolution_SelectedIndexChanged;
            this.Controls.Add(cmbResolution);

            // Custom resolution fields
            Label lblX = new Label { Text = "x", Location = new Point(controlLeft + 240, y + 3), Width = 15 };
            txtCustomWidth = new TextBox
            {
                Location = new Point(controlLeft + 160, y),
                Width = 70,
                Visible = false
            };
            txtCustomHeight = new TextBox
            {
                Location = new Point(controlLeft + 260, y),
                Width = 70,
                Visible = false
            };
            this.Controls.Add(lblX);
            this.Controls.Add(txtCustomWidth);
            this.Controls.Add(txtCustomHeight);

            y += 35;

            // Saturation
            AddLabel("Saturation:", 20, y);
            numSaturation = new NumericUpDown
            {
                Location = new Point(controlLeft, y),
                Width = 100,
                Minimum = 0,
                Maximum = 3,
                DecimalPlaces = 2,
                Increment = 0.1M
            };
            AddLabel("(1.0 = default, higher = more saturated)", controlLeft + 110, y + 3, 300);
            this.Controls.Add(numSaturation);

            y += 35;

            // CRF
            AddLabel("CRF Quality:", 20, y);
            numCRF = new NumericUpDown
            {
                Location = new Point(controlLeft, y),
                Width = 100,
                Minimum = 0,
                Maximum = 51,
                Value = 18
            };
            AddLabel("(lower = better quality, 18-23 recommended)", controlLeft + 110, y + 3, 350);
            this.Controls.Add(numCRF);

            y += 35;

            // Bitrate
            AddLabel("Bitrate (kbps):", 20, y);
            numBitrate = new NumericUpDown
            {
                Location = new Point(controlLeft, y),
                Width = 100,
                Minimum = 1000,
                Maximum = 100000,
                Increment = 1000
            };
            this.Controls.Add(numBitrate);

            y += 35;

            // Move processed files checkbox
            chkMoveProcessed = new CheckBox
            {
                Text = "Move original files to 'processed' subfolder after conversion",
                Location = new Point(20, y),
                Width = 500
            };
            this.Controls.Add(chkMoveProcessed);

            y += 40;

            // Convert button
            btnConvert = new Button
            {
                Text = "Start Conversion",
                Location = new Point(20, y),
                Width = 150,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            btnConvert.Click += BtnConvert_Click;
            this.Controls.Add(btnConvert);

            y += 50;

            // Progress
            lblProgress = new Label
            {
                Text = "Ready",
                Location = new Point(20, y),
                Width = 760
            };
            this.Controls.Add(lblProgress);

            y += 25;

            progressBar = new ProgressBar
            {
                Location = new Point(20, y),
                Width = 760
            };
            this.Controls.Add(progressBar);

            y += 35;

            // Log
            AddLabel("Log:", 20, y);
            y += 20;
            txtLog = new RichTextBox
            {
                Location = new Point(20, y),
                Width = 760,
                Height = 200,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(txtLog);

            this.FormClosing += MainForm_FormClosing;
        }

        private void AddLabel(string text, int x, int y, int width = 120)
        {
            Label label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Width = width
            };
            this.Controls.Add(label);
        }

        private void LoadSettings()
        {
            txtInputFolder.Text = settings.InputFolder;
            txtOutputFolder.Text = settings.OutputFolder;

            // Set resolution
            if (settings.OutputWidth == 1920 && settings.OutputHeight == 1080)
                cmbResolution.SelectedIndex = 0;
            else if (settings.OutputWidth == 2560 && settings.OutputHeight == 1440)
                cmbResolution.SelectedIndex = 1;
            else if (settings.OutputWidth == 3840 && settings.OutputHeight == 2160)
                cmbResolution.SelectedIndex = 2;
            else
            {
                cmbResolution.SelectedIndex = 3;
                txtCustomWidth.Text = settings.OutputWidth.ToString();
                txtCustomHeight.Text = settings.OutputHeight.ToString();
            }

            numSaturation.Value = (decimal)settings.Saturation;
            numCRF.Value = settings.CRF;
            numBitrate.Value = settings.Bitrate;
            chkMoveProcessed.Checked = settings.MoveProcessedFiles;
        }

        private void SaveSettings()
        {
            settings.InputFolder = txtInputFolder.Text;
            settings.OutputFolder = txtOutputFolder.Text;
            settings.Saturation = (double)numSaturation.Value;
            settings.CRF = (int)numCRF.Value;
            settings.Bitrate = (int)numBitrate.Value;
            settings.MoveProcessedFiles = chkMoveProcessed.Checked;

            // Get resolution
            if (cmbResolution.SelectedIndex == 3) // Custom
            {
                if (int.TryParse(txtCustomWidth.Text, out int width))
                    settings.OutputWidth = width;
                if (int.TryParse(txtCustomHeight.Text, out int height))
                    settings.OutputHeight = height;
            }
            else
            {
                string resolution = cmbResolution.Text.Split(' ')[0];
                string[] parts = resolution.Split('x');
                settings.OutputWidth = int.Parse(parts[0]);
                settings.OutputHeight = int.Parse(parts[1]);
            }

            settings.Save();
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtInputFolder.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtInputFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtOutputFolder.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputFolder.Text = dialog.SelectedPath;
            }
        }

        private void CmbResolution_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool isCustom = cmbResolution.SelectedIndex == 3;
            txtCustomWidth.Visible = isCustom;
            txtCustomHeight.Visible = isCustom;
        }

        private async void BtnConvert_Click(object? sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtInputFolder.Text))
            {
                MessageBox.Show("Please select an input folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                MessageBox.Show("Please select an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(txtInputFolder.Text))
            {
                MessageBox.Show("Input folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveSettings();

            btnConvert.Enabled = false;
            txtLog.Clear();
            progressBar.Value = 0;

            try
            {
                await ConvertVideosAsync();
            }
            catch (Exception ex)
            {
                LogError($"Error: {ex.Message}");
            }
            finally
            {
                btnConvert.Enabled = true;
                lblProgress.Text = "Ready";
            }
        }

        private async Task ConvertVideosAsync()
        {
            string inputFolder = txtInputFolder.Text;
            string outputFolder = txtOutputFolder.Text;
            string processedFolder = Path.Combine(inputFolder, "processed");

            // Create directories
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                LogInfo("Created output folder");
            }

            if (chkMoveProcessed.Checked && !Directory.Exists(processedFolder))
            {
                Directory.CreateDirectory(processedFolder);
                LogInfo("Created processed folder");
            }

            // Get video files
            var videoFiles = Directory.GetFiles(inputFolder, "*.mp4", SearchOption.TopDirectoryOnly);

            if (videoFiles.Length == 0)
            {
                LogWarning("No video files found in input folder.");
                return;
            }

            LogInfo($"Found {videoFiles.Length} video file(s) to process");
            LogInfo($"Settings: {settings.OutputWidth}x{settings.OutputHeight}, Saturation: {settings.Saturation}, CRF: {settings.CRF}");
            LogInfo("");

            progressBar.Maximum = videoFiles.Length;
            progressBar.Value = 0;

            for (int i = 0; i < videoFiles.Length; i++)
            {
                string inputPath = videoFiles[i];
                string fileName = Path.GetFileName(inputPath);
                string outputPath = Path.Combine(outputFolder, fileName);
                string processedPath = Path.Combine(processedFolder, fileName);

                lblProgress.Text = $"Processing {i + 1}/{videoFiles.Length}: {fileName}";
                LogInfo($"[{i + 1}/{videoFiles.Length}] Processing: {fileName}");

                // Build ffmpeg command
                string vf = $"setdar=16/9,eq=saturation={settings.Saturation}";
                string args = $"-i \"{inputPath}\" -vf \"{vf}\" -c:v libx265 -pix_fmt yuv420p -crf {settings.CRF} -b:v {settings.Bitrate}k -s {settings.OutputWidth}x{settings.OutputHeight} \"{outputPath}\"";

                bool success = await RunFFmpegAsync(args);

                if (success)
                {
                    LogSuccess($"Successfully converted: {fileName}");

                    if (chkMoveProcessed.Checked)
                    {
                        File.Move(inputPath, processedPath, true);
                        LogInfo("  Moved original to processed folder");
                    }
                }
                else
                {
                    LogError($"Error converting: {fileName} - original kept in place");
                }

                progressBar.Value = i + 1;
                LogInfo("");
            }

            LogSuccess($"Conversion complete! Processed {videoFiles.Length} file(s).");
        }

        private Task<bool> RunFFmpegAsync(string arguments)
        {
            return Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using Process? process = Process.Start(psi);
                    if (process == null)
                        return false;

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Invoke(() => LogError($"FFmpeg error: {ex.Message}"));
                    return false;
                }
            });
        }

        private void LogInfo(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogInfo(message));
                return;
            }

            txtLog.SelectionColor = Color.Black;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void LogSuccess(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogSuccess(message));
                return;
            }

            txtLog.SelectionColor = Color.Green;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void LogWarning(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogWarning(message));
                return;
            }

            txtLog.SelectionColor = Color.Orange;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void LogError(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogError(message));
                return;
            }

            txtLog.SelectionColor = Color.Red;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
    }
}
