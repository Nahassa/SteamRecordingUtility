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

            // Validate CUDA availability once before processing any videos
            bool cudaValidated = false;
            bool cudaAvailable = false;
            if (settings.UseGpuScaling)
            {
                cudaAvailable = CheckCudaDeviceAvailable();
                cudaValidated = true;
                if (!cudaAvailable)
                    LogWarning("CUDA device not available — GPU scaling will be disabled for all videos");
            }

            LogInfo($"Starting conversion of {videos.Count} video(s)");
            if (settings.NvencUHQMode)
            {
                LogInfo("Mode: Ultra High Quality (av1_nvenc UHQ)");
                LogInfo($"  Bitrate: {settings.NvencUHQBitrate}M, Max: {settings.NvencUHQMaxrate}M, RC-Lookahead: {settings.NvencUHQRcLookahead}");
            }
            else
            {
                LogInfo($"Encoder: {settings.VideoEncoder}");
            }
            string scalingDesc = settings.ScalingMode == "sar" ? "SAR (preserve pixels)" :
                settings.UseGpuScaling ? "GPU (scale_cuda)" : "CPU (scale + lanczos)";
            LogInfo($"Scaling: {scalingDesc}");
            if (!settings.NvencUHQMode)
            {
                if (settings.VideoEncoder == "libx265")
                {
                    LogInfo($"  libx265 settings - CRF: {settings.X265CRF}, Preset: {settings.X265Preset}, Tune: {(string.IsNullOrEmpty(settings.X265Tune) ? "(none)" : settings.X265Tune)}");
                }
                else
                {
                    LogInfo($"  NVENC settings - CQ: {settings.NvencCQ}, Preset: {settings.NvencPreset}, Tune: {settings.NvencTune}, RC: {settings.NvencRateControl}, Multipass: {settings.NvencMultipass}, B-Frames: {settings.NvencBFrames}, Spatial AQ: {settings.NvencSpatialAQ}, Temporal AQ: {settings.NvencTemporalAQ}");
                }
            }
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

                    // Resolve encoder first (needed to decide GPU vs CPU scaling)
                    string encoder = GetEffectiveEncoder();
                    bool isNvenc = encoder == "hevc_nvenc" || encoder == "av1_nvenc";
                    bool gpuScale = settings.EnableScaling && settings.ScalingMode != "sar"
                                    && settings.UseGpuScaling && isNvenc
                                    && (!cudaValidated || cudaAvailable);
                    string encoderArgs = GetEncoderArguments(encoder, gpuPipeline: gpuScale);

                    // Build filter chain: color first (CPU), then scaling (GPU or CPU)
                    bool useGpuUpload = false;

                    // Color adjustment filter first (CPU — must run before hwupload)
                    if (settings.EnableColorAdjustments)
                    {
                        string brightnessStr = video.Brightness.ToString("0.00", CultureInfo.InvariantCulture);
                        string contrastStr = video.Contrast.ToString("0.00", CultureInfo.InvariantCulture);
                        string saturationStr = video.Saturation.ToString("0.00", CultureInfo.InvariantCulture);
                        filters.Add($"eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}");
                        LogInfo($"  Color: Brightness={video.Brightness:0.00}, Contrast={video.Contrast:0.00}, Saturation={video.Saturation:0.00}");
                    }

                    // Scaling filter (if enabled)
                    if (settings.EnableScaling)
                    {
                        if (settings.ScalingMode == "sar")
                        {
                            var (sarNum, sarDen) = ComputeSarFor16by9(video.OutputWidth, video.OutputHeight);
                            if (sarNum != sarDen)
                            {
                                filters.Add($"setsar={sarNum}/{sarDen}");
                                LogInfo($"  SAR: {sarNum}/{sarDen} (preserving {video.OutputWidth}x{video.OutputHeight} pixels, displays as 16:9)");
                            }
                            else
                            {
                                LogInfo($"  Already 16:9 ({video.OutputWidth}x{video.OutputHeight}), no SAR needed");
                            }
                        }
                        else if (gpuScale)
                        {
                            if (settings.EnableColorAdjustments)
                            {
                                // CPU color already added above — convert to NV12, upload to GPU, then scale
                                filters.Add("format=nv12");
                                filters.Add("hwupload_cuda");
                                useGpuUpload = true;
                            }
                            else
                            {
                                // No color — decode on CPU, convert to NV12, upload to GPU, then scale
                                filters.Add("format=nv12");
                                filters.Add("hwupload_cuda");
                                useGpuUpload = true;
                            }
                            filters.Add($"scale_cuda={video.OutputWidth}:{video.OutputHeight}:interp_algo=lanczos");
                            LogInfo($"  GPU Scaling (scale_cuda): {video.OutputWidth}x{video.OutputHeight}");
                        }
                        else
                        {
                            filters.Add($"scale={video.OutputWidth}:{video.OutputHeight}:flags=lanczos");
                            filters.Add("setdar=16/9");
                            LogInfo($"  CPU Scaling: {video.OutputWidth}x{video.OutputHeight}");
                        }
                    }

                    LogInfo($"  Using encoder: {encoder}");

                    // Build FFmpeg command
                    string hwaccelFlags = useGpuUpload ? "-init_hw_device cuda=cu -filter_hw_device cu " : "";
                    string aspectFlag = (useGpuUpload && settings.EnableScaling && settings.ScalingMode != "sar") ? "-aspect 16:9 " : "";
                    string args;
                    if (filters.Count > 0)
                    {
                        string vf = string.Join(",", filters);
                        args = $"-y {hwaccelFlags}-i \"{inputPath}\" -vf \"{vf}\" {aspectFlag}{encoderArgs} \"{outputPath}\"";
                    }
                    else
                    {
                        LogInfo("  Re-encoding only (no scaling or color adjustments)");
                        args = $"-y -i \"{inputPath}\" {encoderArgs} \"{outputPath}\"";
                    }

                    success = await RunFFmpegAsync(args);

                    // Fallback: if GPU scaling failed, retry with CPU scaling
                    if (!success && useGpuUpload)
                    {
                        LogWarning("  GPU scaling (scale_cuda) failed — falling back to CPU scaling");
                        string cpuEncoderArgs = GetEncoderArguments(encoder, gpuPipeline: false);
                        var cpuFilters = new List<string>();
                        if (settings.EnableColorAdjustments)
                        {
                            string brightnessStr2 = video.Brightness.ToString("0.00", CultureInfo.InvariantCulture);
                            string contrastStr2 = video.Contrast.ToString("0.00", CultureInfo.InvariantCulture);
                            string saturationStr2 = video.Saturation.ToString("0.00", CultureInfo.InvariantCulture);
                            cpuFilters.Add($"eq=brightness={brightnessStr2}:contrast={contrastStr2}:saturation={saturationStr2}");
                        }
                        cpuFilters.Add($"scale={video.OutputWidth}:{video.OutputHeight}:flags=lanczos");
                        cpuFilters.Add("setdar=16:9");
                        string cpuVf = string.Join(",", cpuFilters);
                        args = $"-y -i \"{inputPath}\" -vf \"{cpuVf}\" {cpuEncoderArgs} \"{outputPath}\"";
                        success = await RunFFmpegAsync(args);
                    }
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

                        string title = youtubeUploader.ProcessTemplate(
                            settings.YouTubeTitleTemplate,
                            outputPath,
                            settings.YouTubeRemoveDateFromFilename,
                            settings.YouTubeRemoveTextPatterns);
                        string description = youtubeUploader.ProcessTemplate(
                            settings.YouTubeDescriptionTemplate,
                            outputPath,
                            settings.YouTubeRemoveDateFromFilename,
                            settings.YouTubeRemoveTextPatterns);
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

                            // Move converted video to uploaded subfolder in output folder
                            string uploadedFolder = Path.Combine(outputFolder, "uploaded");
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

        private bool CheckCudaDeviceAvailable()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-init_hw_device cuda=cu -f lavfi -i color=s=1x1:d=0.001 -t 0.001 -f null -",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using Process? process = Process.Start(psi);
                if (process == null)
                    return false;

                return process.WaitForExit(3000) && process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
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

        private bool? _hevcNvencAvailable = null;
        private bool? _av1NvencAvailable = null;
        private string? _encoderListCache = null;

        private string GetEncoderList()
        {
            if (_encoderListCache != null)
                return _encoderListCache;

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
                    _encoderListCache = "";
                    return "";
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                _encoderListCache = output;
                return output;
            }
            catch
            {
                _encoderListCache = "";
                return "";
            }
        }

        private bool IsNvencAvailable()
        {
            if (_hevcNvencAvailable.HasValue)
                return _hevcNvencAvailable.Value;

            _hevcNvencAvailable = GetEncoderList().Contains("hevc_nvenc");
            return _hevcNvencAvailable.Value;
        }

        private bool IsAv1NvencAvailable()
        {
            if (_av1NvencAvailable.HasValue)
                return _av1NvencAvailable.Value;

            _av1NvencAvailable = GetEncoderList().Contains("av1_nvenc");
            return _av1NvencAvailable.Value;
        }

        private string GetEncoderArguments(string encoder, bool gpuPipeline = false)
        {
            if (settings.NvencUHQMode)
            {
                // Ultra High Quality mode: av1_nvenc with VBR bitrate targeting
                var args = $"-c:v av1_nvenc -preset p7 -tune uhq";
                args += $" -b:v {settings.NvencUHQBitrate}M -maxrate {settings.NvencUHQMaxrate}M -bufsize {settings.NvencUHQMaxrate}M";
                args += $" -rc-lookahead {settings.NvencUHQRcLookahead}";
                args += " -spatial_aq 1 -temporal_aq 1";
                if (!gpuPipeline)
                    args += " -pix_fmt yuv420p";
                return args;
            }

            if (encoder == "hevc_nvenc" || encoder == "av1_nvenc")
            {
                // NVENC (GPU) encoder settings - modern SDK presets (p1-p7)
                var args = $"-c:v {encoder} -preset {settings.NvencPreset} -tune {settings.NvencTune} -rc {settings.NvencRateControl}";

                // CQ level (quality parameter)
                args += $" -cq {settings.NvencCQ}";

                // Multipass encoding
                if (settings.NvencMultipass != "disabled")
                    args += $" -multipass {settings.NvencMultipass}";

                // B-frames for better compression
                if (settings.NvencBFrames > 0)
                    args += $" -bf {settings.NvencBFrames}";

                // Adaptive quantization
                if (settings.NvencSpatialAQ)
                    args += " -spatial-aq 1";
                if (settings.NvencTemporalAQ)
                    args += " -temporal-aq 1";

                if (!gpuPipeline)
                    args += " -pix_fmt yuv420p";
                return args;
            }
            else
            {
                // libx265 (CPU) encoder settings
                var args = $"-c:v libx265 -crf {settings.X265CRF} -preset {settings.X265Preset}";

                // Add tune if specified
                if (!string.IsNullOrEmpty(settings.X265Tune))
                    args += $" -tune {settings.X265Tune}";

                args += " -pix_fmt yuv420p";
                return args;
            }
        }

        private string GetEffectiveEncoder()
        {
            // UHQ mode forces av1_nvenc
            string requestedEncoder = settings.NvencUHQMode ? "av1_nvenc" : settings.VideoEncoder;

            // Fallback chain: av1_nvenc → hevc_nvenc → libx265
            if (requestedEncoder == "av1_nvenc" && !IsAv1NvencAvailable())
            {
                LogWarning("AV1 NVENC encoder not available.");
                if (IsNvencAvailable())
                {
                    LogWarning("Falling back to HEVC NVENC (hevc_nvenc).");
                    return "hevc_nvenc";
                }
                LogWarning("Falling back to CPU encoder (libx265).");
                return "libx265";
            }

            if (requestedEncoder == "hevc_nvenc" && !IsNvencAvailable())
            {
                LogWarning("NVENC encoder not available. Falling back to CPU encoder (libx265).");
                return "libx265";
            }

            return requestedEncoder;
        }

        /// <summary>
        /// Computes the SAR (Sample Aspect Ratio) needed to make the given resolution
        /// display at 16:9. Returns (numerator, denominator) simplified by GCD.
        /// </summary>
        private static (int num, int den) ComputeSarFor16by9(int width, int height)
        {
            // DAR = SAR * (width / height) = 16/9
            // SAR = (16 * height) / (9 * width)
            int num = 16 * height;
            int den = 9 * width;
            int gcd = GCD(num, den);
            return (num / gcd, den / gcd);
        }

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
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
