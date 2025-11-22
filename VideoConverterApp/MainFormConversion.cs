using System.Diagnostics;
using System.Globalization;

namespace VideoConverterApp
{
    public partial class MainForm
    {
        private async void BtnConvertAll_Click(object? sender, EventArgs e)
        {
            // Validate
            if (videoItems.Count == 0)
            {
                MessageBox.Show("No videos loaded. Click 'Load Videos' first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                MessageBox.Show("Please select an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsFFmpegAvailable())
            {
                MessageBox.Show(
                    "FFmpeg not found!\n\nPlease ensure ffmpeg.exe is either:\n1. In your system PATH, or\n2. In the same folder as this application\n\nDownload FFmpeg from: https://www.gyan.dev/ffmpeg/builds/",
                    "FFmpeg Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (settings.EnableYouTubeUpload && youtubeUploader == null)
            {
                MessageBox.Show(
                    "YouTube upload is enabled but you haven't authenticated yet.\n\nPlease open YouTube Settings and click 'Authenticate' first, or disable YouTube upload.",
                    "YouTube Authentication Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SaveSettings();

            // Get selected videos
            var selectedVideos = videoItems.Where(v => v.Selected).ToList();
            if (selectedVideos.Count == 0)
            {
                MessageBox.Show("No videos selected for conversion.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnConvertAll.Enabled = false;
            btnLoadVideos.Enabled = false;
            txtLog.Clear();
            progressBar.Value = 0;
            progressBar.Maximum = selectedVideos.Count;

            try
            {
                await ConvertVideosAsync(selectedVideos);
            }
            catch (Exception ex)
            {
                LogError($"Error during conversion: {ex.Message}");
            }
            finally
            {
                btnConvertAll.Enabled = true;
                btnLoadVideos.Enabled = true;
                lblProgress.Text = "Ready";
                lblCurrentTask.Text = "";
            }
        }

        private async Task ConvertVideosAsync(List<VideoItem> videos)
        {
            string outputFolder = txtOutputFolder.Text;
            string inputFolder = txtInputFolder.Text;
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

            LogInfo($"Starting conversion of {videos.Count} video(s)");
            LogInfo($"Global settings: CRF: {numCRF.Value}, Bitrate: {numBitrate.Value}k");
            LogInfo("");

            for (int i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                string fileName = video.FileName;
                string inputPath = video.FilePath;
                string outputPath = Path.Combine(outputFolder, fileName);
                string processedPath = Path.Combine(processedFolder, fileName);

                lblProgress.Text = $"Converting {i + 1}/{videos.Count}";
                lblCurrentTask.Text = fileName;
                LogInfo($"[{i + 1}/{videos.Count}] Processing: {fileName}");
                LogInfo($"  Settings: Brightness={video.Brightness:0.00}, Contrast={video.Contrast:0.00}, Saturation={video.Saturation:0.00}");

                // Build ffmpeg command with optimal quality settings
                string brightnessStr = video.Brightness.ToString("0.00", CultureInfo.InvariantCulture);
                string contrastStr = video.Contrast.ToString("0.00", CultureInfo.InvariantCulture);
                string saturationStr = video.Saturation.ToString("0.00", CultureInfo.InvariantCulture);

                // Important: Do scaling in the filter chain (not with -s) to avoid double resampling
                // Use lanczos for high-quality scaling, then apply other filters
                string vf = $"scale={video.OutputWidth}:{video.OutputHeight}:flags=lanczos,setdar=16/9,eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}";

                // Use -preset slow for better quality/compression (trades encoding time for quality)
                string args = $"-i \"{inputPath}\" -vf \"{vf}\" -c:v libx265 -preset slow -pix_fmt yuv420p -crf {numCRF.Value} -b:v {numBitrate.Value}k \"{outputPath}\"";

                bool success = await RunFFmpegAsync(args);

                if (success)
                {
                    LogSuccess($"Successfully converted: {fileName}");

                    // Move original to processed folder
                    if (chkMoveProcessed.Checked)
                    {
                        File.Move(inputPath, processedPath, true);
                        LogInfo("  Moved original to processed folder");
                    }

                    // Upload to YouTube if enabled
                    if (settings.EnableYouTubeUpload && youtubeUploader != null)
                    {
                        lblProgress.Text = $"Uploading {i + 1}/{videos.Count} to YouTube";
                        lblCurrentTask.Text = fileName;
                        LogInfo("  Uploading to YouTube...");

                        string title = youtubeUploader.ProcessTemplate(settings.YouTubeTitleTemplate, outputPath);
                        string description = youtubeUploader.ProcessTemplate(settings.YouTubeDescriptionTemplate, outputPath);
                        string[] tags = settings.YouTubeTags.Split(',').Select(t => t.Trim()).ToArray();

                        var uploadProgress = new Progress<int>(percent =>
                        {
                            if (percent >= 0)
                            {
                                Invoke(() => LogInfo($"  Upload progress: {percent}%"));
                            }
                        });

                        var (uploadSuccess, videoId, videoUrl) = await youtubeUploader.UploadVideoAsync(
                            outputPath,
                            title,
                            description,
                            tags,
                            settings.YouTubePrivacyStatus,
                            settings.YouTubeCategoryId,
                            settings.YouTubeMadeForKids,
                            settings.YouTubeAgeRestricted,
                            uploadProgress);

                        if (uploadSuccess && videoUrl != null)
                        {
                            LogSuccess($"  Uploaded to YouTube: {videoUrl}");

                            // Move converted video to uploaded subfolder
                            string uploadedFolder = Path.Combine(processedFolder, "uploaded");
                            if (!Directory.Exists(uploadedFolder))
                            {
                                Directory.CreateDirectory(uploadedFolder);
                                LogInfo("  Created uploaded folder");
                            }

                            string uploadedPath = Path.Combine(uploadedFolder, fileName);
                            File.Move(outputPath, uploadedPath, true);
                            LogInfo("  Moved converted video to uploaded folder");
                        }
                        else
                        {
                            LogError("  YouTube upload failed - converted video kept in output folder");
                        }
                    }
                }
                else
                {
                    LogError($"Error converting: {fileName} - original kept in place");
                }

                progressBar.Value = i + 1;
                LogInfo("");
            }

            LogSuccess($"Conversion complete! Processed {videos.Count} video(s).");

            // Clear all preview caches after conversion to free memory
            foreach (var video in videoItems)
            {
                video.ClearPreviewCache();
            }

            // Clear PictureBox references
            pic40Before.Image = null;
            pic60Before.Image = null;
            pic40After.Image = null;
            pic60After.Image = null;

            LogInfo("Cleaned up preview cache");
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
                        RedirectStandardOutput = false, // Don't redirect stdout - prevents deadlock
                        RedirectStandardError = true    // FFmpeg outputs progress/errors to stderr
                    };

                    using Process? process = Process.Start(psi);
                    if (process == null)
                    {
                        Invoke(() => LogError("Failed to start FFmpeg process"));
                        return false;
                    }

                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        var errorLines = stderr.Split('\n')
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .TakeLast(5);

                        foreach (var line in errorLines)
                        {
                            Invoke(() => LogError($"  {line.Trim()}"));
                        }
                    }

                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Invoke(() => LogError($"FFmpeg error: {ex.Message}"));
                    if (ex is System.ComponentModel.Win32Exception)
                    {
                        Invoke(() => LogError("FFmpeg not found. Please ensure ffmpeg.exe is in PATH or in the same folder as this application."));
                    }
                    return false;
                }
            });
        }

        private bool IsFFmpegAvailable()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
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
            catch
            {
                return false;
            }
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

            // Clean up preview images
            foreach (var video in videoItems)
            {
                video.ClearPreviewCache();
            }

            // Clean up temp files
            FrameExtractor.CleanupTempFiles();
        }
    }
}
