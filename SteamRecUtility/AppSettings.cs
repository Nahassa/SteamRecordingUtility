using Newtonsoft.Json;

namespace SteamRecUtility
{
    public class AppSettings
    {
        public string InputFolder { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
        public int OutputWidth { get; set; } = 1920;
        public int OutputHeight { get; set; } = 1080;
        public double Brightness { get; set; } = 0.0;  // -1.0 to 1.0 (default 0.0 = no change)
        public double Contrast { get; set; } = 1.0;    // 0.0 to 4.0 (default 1.0 = no change)
        public double Saturation { get; set; } = 1.2;
        public string VideoEncoder { get; set; } = "hevc_nvenc"; // libx265, hevc_nvenc, av1_nvenc
        public bool MoveProcessedFiles { get; set; } = true;

        // Scaling mode: "scale" = resample pixels (traditional), "sar" = set sample aspect ratio (preserve pixels)
        public string ScalingMode { get; set; } = "sar";

        // libx265 (CPU) encoder settings
        public int X265CRF { get; set; } = 23;
        public string X265Preset { get; set; } = "medium";
        public string X265Tune { get; set; } = ""; // empty = none

        // NVENC (GPU) encoder settings - modern SDK presets (RTX 5080 / Ada+)
        public int NvencCQ { get; set; } = 21;
        public string NvencPreset { get; set; } = "p7";        // p1 (fastest) to p7 (best quality)
        public string NvencTune { get; set; } = "hq";          // hq, ll, ull, lossless
        public string NvencRateControl { get; set; } = "constqp";
        public string NvencMultipass { get; set; } = "fullres"; // disabled, qres, fullres
        public int NvencBFrames { get; set; } = 3;             // 0-4, B-frames for better compression
        public bool NvencSpatialAQ { get; set; } = true;
        public bool NvencTemporalAQ { get; set; } = true;

        // Processing Options
        public bool EnableVideoConversion { get; set; } = true;
        public bool EnableScaling { get; set; } = true;
        public bool EnableColorAdjustments { get; set; } = true;

        // YouTube Settings
        public bool EnableYouTubeUpload { get; set; } = false;
        public string YouTubeTitleTemplate { get; set; } = "{filename}";
        public string YouTubeDescriptionTemplate { get; set; } = "Converted video: {filename}";
        public string YouTubeTags { get; set; } = "gaming,gameplay";
        public string YouTubePrivacyStatus { get; set; } = "private"; // private, unlisted, public
        public string YouTubeCategoryId { get; set; } = "20"; // 20 = Gaming
        public bool YouTubeMadeForKids { get; set; } = false;
        public bool YouTubeAgeRestricted { get; set; } = false;
        public bool YouTubeRemoveDateFromFilename { get; set; } = false;
        public string YouTubeRemoveTextPatterns { get; set; } = string.Empty; // Comma-separated text to remove

        private static readonly string SettingsFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}\nUsing defaults.",
                    "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
