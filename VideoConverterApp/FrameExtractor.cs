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
                    string outputPath = Path.Combine(TempFolder, $"{Guid.NewGuid()}.jpg");

                    // Use -ss with percentage to seek to specific point
                    // -vframes 1 to extract just one frame
                    string args = $"-ss {percentage}% -i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";

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

                        process.WaitForExit();

                        if (process.ExitCode != 0 || !File.Exists(outputPath))
                            return null;
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
                catch
                {
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
                    string outputPath = Path.Combine(TempFolder, $"{Guid.NewGuid()}.jpg");

                    // Build filter string with invariant culture for decimal formatting
                    string brightnessStr = brightness.ToString("0.00", CultureInfo.InvariantCulture);
                    string contrastStr = contrast.ToString("0.00", CultureInfo.InvariantCulture);
                    string saturationStr = saturation.ToString("0.00", CultureInfo.InvariantCulture);

                    string filter = $"eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}";

                    string args = $"-ss {percentage}% -i \"{videoPath}\" -vf \"{filter}\" -vframes 1 -q:v 2 \"{outputPath}\"";

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

                        process.WaitForExit();

                        if (process.ExitCode != 0 || !File.Exists(outputPath))
                            return null;
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
                catch
                {
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
