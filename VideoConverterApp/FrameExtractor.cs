using System.Diagnostics;
using System.Globalization;

namespace VideoConverterApp
{
    public class FrameExtractor
    {
        private static readonly string TempFolder = Path.Combine(Path.GetTempPath(), "VideoConverter");

        static FrameExtractor()
        {
            // Ensure temp folder exists
            if (!Directory.Exists(TempFolder))
            {
                Directory.CreateDirectory(TempFolder);
            }
        }

        /// <summary>
        /// Gets the duration of a video in seconds using ffprobe
        /// </summary>
        private static double? GetVideoDuration(string videoPath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null)
                        return null;

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
                    {
                        return duration;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Extracts frames at specific percentages from a video
        /// </summary>
        public static async Task<(Image? frame40, Image? frame60)> ExtractFramesAsync(string videoPath)
        {
            try
            {
                var frame40Task = ExtractFrameAtPercentageAsync(videoPath, 40);
                var frame60Task = ExtractFrameAtPercentageAsync(videoPath, 60);

                await Task.WhenAll(frame40Task, frame60Task);

                return (await frame40Task, await frame60Task);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting frames: {ex.Message}", "Frame Extraction Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (null, null);
            }
        }

        /// <summary>
        /// Extracts a single frame at a percentage through the video
        /// </summary>
        private static async Task<Image?> ExtractFrameAtPercentageAsync(string videoPath, int percentage)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Get video duration first
                    double? duration = GetVideoDuration(videoPath);
                    if (duration == null || duration <= 0)
                        return null;

                    // Calculate the time position
                    double timePosition = duration.Value * (percentage / 100.0);
                    string timeStr = timePosition.ToString("0.00", CultureInfo.InvariantCulture);

                    string outputPath = Path.Combine(TempFolder, $"{Guid.NewGuid()}.jpg");

                    // Use -ss with actual time to seek to specific point
                    // -vframes 1 to extract just one frame
                    string args = $"-ss {timeStr} -i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process? process = Process.Start(psi))
                    {
                        if (process == null)
                            return null;

                        string stderr = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0 || !File.Exists(outputPath))
                        {
                            // Log error for debugging
                            System.Diagnostics.Debug.WriteLine($"FFmpeg frame extraction failed: {stderr}");
                            return null;
                        }
                    }

                    // Load the image and delete the temp file
                    Image frame = Image.FromFile(outputPath);

                    // Delete temp file after a short delay (file might be locked)
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        try
                        {
                            if (File.Exists(outputPath))
                                File.Delete(outputPath);
                        }
                        catch { /* Ignore cleanup errors */ }
                    });

                    return frame;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Frame extraction exception: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Generates a preview frame with filters applied
        /// </summary>
        public static async Task<Image?> GenerateFilteredPreviewAsync(
            string videoPath,
            int percentage,
            double brightness,
            double contrast,
            double saturation)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Get video duration first
                    double? duration = GetVideoDuration(videoPath);
                    if (duration == null || duration <= 0)
                        return null;

                    // Calculate the time position
                    double timePosition = duration.Value * (percentage / 100.0);
                    string timeStr = timePosition.ToString("0.00", CultureInfo.InvariantCulture);

                    string outputPath = Path.Combine(TempFolder, $"{Guid.NewGuid()}.jpg");

                    // Build filter string with invariant culture for decimal formatting
                    string brightnessStr = brightness.ToString("0.00", CultureInfo.InvariantCulture);
                    string contrastStr = contrast.ToString("0.00", CultureInfo.InvariantCulture);
                    string saturationStr = saturation.ToString("0.00", CultureInfo.InvariantCulture);

                    string filter = $"eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}";

                    string args = $"-ss {timeStr} -i \"{videoPath}\" -vf \"{filter}\" -vframes 1 -q:v 2 \"{outputPath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process? process = Process.Start(psi))
                    {
                        if (process == null)
                            return null;

                        string stderr = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0 || !File.Exists(outputPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"FFmpeg filtered frame extraction failed: {stderr}");
                            return null;
                        }
                    }

                    // Load the image
                    Image frame = Image.FromFile(outputPath);

                    // Delete temp file
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        try
                        {
                            if (File.Exists(outputPath))
                                File.Delete(outputPath);
                        }
                        catch { /* Ignore */ }
                    });

                    return frame;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Filtered frame extraction exception: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Cleans up old temp files
        /// </summary>
        public static void CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(TempFolder))
                {
                    foreach (var file in Directory.GetFiles(TempFolder, "*.jpg"))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { /* Ignore */ }
                    }
                }
            }
            catch { /* Ignore */ }
        }
    }
}
