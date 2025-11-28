using System.Diagnostics;
using System.Globalization;

namespace SteamRecUtility
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

                lblProgress.Text = $"Processing {i + 1}/{videos.Count}";
                lblCurrentTask.Text = fileName;
                LogInfo($"[{i + 1}/{videos.Count}] Processing: {fileName}");

                bool success;

                // Check if video conversion is enabled
                if (settings.EnableVideoConversion)
                {
                    // Build dynamic filter chain based on enabled options
                    var filters = new List<string>();

                    // Scaling filter (if enabled)
                    if (settings.EnableScaling)
                    {
                        filters.Add($"scale={video.OutputWidth}:{video.OutputHeight}:flags=lanczos");
                        filters.Add("setdar=16/9");
                        LogInfo($"  Scaling: {video.OutputWidth}x{video.OutputHeight}");
                    }

                    // Color adjustment filter (if enabled)
                    if (settings.EnableColorAdjustments)
                    {
                        string brightnessStr = video.Brightness.ToString("0.00", CultureInfo.InvariantCulture);
                        string contrastStr = video.Contrast.ToString("0.00", CultureInfo.InvariantCulture);
                        string saturationStr = video.Saturation.ToString("0.00", CultureInfo.InvariantCulture);
                        filters.Add($"eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}");
                        LogInfo($"  Color: Brightness={video.Brightness:0.00}, Contrast={video.Contrast:0.00}, Saturation={video.Saturation:0.00}");
                    }

                    // Get effective encoder (with NVENC fallback if needed)
                    string encoder = GetEffectiveEncoder();
                    string encoderArgs = GetEncoderArguments(encoder, (int)numCRF.Value, (int)numBitrate.Value);
                    LogInfo($"  Encoder: {encoder}");

                    // Build FFmpeg command
                    string args;
                    if (filters.Count > 0)
                    {
                        string vf = string.Join(",", filters);
                        args = $"-y -i \"{inputPath}\" -vf \"{vf}\" {encoderArgs} \"{outputPath}\"";
                    }
                    else
                    {
                        // No filters, just re-encode
                        LogInfo("  Re-encoding only (no scaling or color adjustments)");
                        args = $"-y -i \"{inputPath}\" {encoderArgs} \"{outputPath}\"";
                    }

                    success = await RunFFmpegAsync(args);
                }
                else
                {
                    // No conversion - just copy file to output (for YouTube upload only workflow)
                    LogInfo("  Copying file (conversion disabled)");
                    File.Copy(inputPath, outputPath, true);
                    success = true;
                }

                if (success)
                {
                    LogSuccess($"Successfully processed: {fileName}");

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

                        string title = youtubeUploader.ProcessTemplate(settings.YouTubeTitleTemplate, outputPath, settings.YouTubeRemoveDateFromFilename);
                        string description = youtubeUploader.ProcessTemplate(settings.YouTubeDescriptionTemplate, outputPath, settings.YouTubeRemoveDateFromFilename);
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

                    // Read stderr character by character to handle FFmpeg's \r-based progress updates
                    var errorLines = new List<string>();
                    var lineBuilder = new System.Text.StringBuilder();
                    string lastLoggedTime = "";
                    int ch;

                    while ((ch = process.StandardError.Read()) != -1)
                    {
                        if (ch == '\r' || ch == '\n')
                        {
                            if (lineBuilder.Length > 0)
                            {
                                string trimmedLine = lineBuilder.ToString().Trim();
                                lineBuilder.Clear();

                                if (!string.IsNullOrWhiteSpace(trimmedLine))
                                {
                                    errorLines.Add(trimmedLine);
                                    ProcessFFmpegLine(trimmedLine, ref lastLoggedTime);
                                }
                            }
                        }
                        else
                        {
                            lineBuilder.Append((char)ch);
                        }
                    }

                    // Process any remaining content
                    if (lineBuilder.Length > 0)
                    {
                        string trimmedLine = lineBuilder.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            errorLines.Add(trimmedLine);
                            ProcessFFmpegLine(trimmedLine, ref lastLoggedTime);
                        }
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        // Log the last few error lines
                        foreach (var errorLine in errorLines.TakeLast(5))
                        {
                            Invoke(() => LogError($"  {errorLine}"));
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

        private void ProcessFFmpegLine(string trimmedLine, ref string lastLoggedTime)
        {
            // Parse progress info (FFmpeg outputs lines like: frame=123 fps=30 ... time=00:01:23.45 ...)
            if (trimmedLine.Contains("time=") && trimmedLine.StartsWith("frame="))
            {
                string? timeInfo = ParseFFmpegTime(trimmedLine);
                if (timeInfo != null)
                {
                    Invoke(() => lblCurrentTask.Text = $"Encoding: {timeInfo}");

                    // Log progress every ~5 seconds of video time to avoid flooding
                    string timePrefix = timeInfo.Length >= 7 ? timeInfo.Substring(0, 7) : timeInfo; // HH:MM:S
                    if (timePrefix != lastLoggedTime)
                    {
                        lastLoggedTime = timePrefix;
                        Invoke(() => LogInfo($"  Progress: {trimmedLine}"));
                    }
                }
            }
            // Log error lines
            else if (trimmedLine.Contains("Error") || trimmedLine.Contains("error"))
            {
                Invoke(() => LogInfo($"  {trimmedLine}"));
            }
        }

        private static string? ParseFFmpegTime(string line)
        {
            // Parse time from FFmpeg progress output: "time=00:01:23.45"
            int timeIndex = line.IndexOf("time=");
            if (timeIndex >= 0)
            {
                int start = timeIndex + 5;
                int end = line.IndexOf(' ', start);
                if (end < 0) end = line.Length;

                string timeStr = line.Substring(start, end - start);
                // Clean up the time string (remove any trailing characters)
                if (timeStr.Contains("bitrate"))
                {
                    timeStr = timeStr.Split("bitrate")[0].Trim();
                }
                return timeStr;
            }
            return null;
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

        private bool? _nvencAvailable = null;

        private bool IsNvencAvailable()
        {
            // Cache the result to avoid repeated checks
            if (_nvencAvailable.HasValue)
                return _nvencAvailable.Value;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-encoders",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using Process? process = Process.Start(psi);
                if (process == null)
                {
                    _nvencAvailable = false;
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                _nvencAvailable = output.Contains("hevc_nvenc");
                return _nvencAvailable.Value;
            }
            catch
            {
                _nvencAvailable = false;
                return false;
            }
        }

        private string GetEncoderArguments(string encoder, int crf, int bitrate)
        {
            return encoder switch
            {
                "hevc_nvenc" => $"-c:v hevc_nvenc -preset p4 -cq {crf} -pix_fmt yuv420p -b:v {bitrate}k",
                "hevc_nvenc_hq" => $"-c:v hevc_nvenc -preset p7 -tune hq -rc vbr -cq {crf} -spatial-aq 1 -temporal-aq 1 -rc-lookahead 32 -b_ref_mode middle -pix_fmt yuv420p -b:v {bitrate}k",
                _ => $"-c:v libx265 -preset slow -pix_fmt yuv420p -crf {crf} -b:v {bitrate}k" // libx265 (default)
            };
        }

        private string GetEffectiveEncoder()
        {
            string requestedEncoder = settings.VideoEncoder;

            // If NVENC requested but not available, fall back to libx265
            if ((requestedEncoder == "hevc_nvenc" || requestedEncoder == "hevc_nvenc_hq") && !IsNvencAvailable())
            {
                LogWarning("NVENC encoder not available. Falling back to CPU encoder (libx265).");
                return "libx265";
            }

            return requestedEncoder;
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
