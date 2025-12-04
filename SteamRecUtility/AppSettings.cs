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
        public string VideoEncoder { get; set; } = "libx265"; // libx265, hevc_nvenc, av1_nvenc
        public bool MoveProcessedFiles { get; set; } = true;

        // libx265 (CPU) encoder settings
        public int X265CRF { get; set; } = 23;
        public string X265Preset { get; set; } = "medium";
        public string X265Tune { get; set; } = ""; // empty = none
        public int X265BFrames { get; set; } = 3; // 0-16
        public int X265Lookahead { get; set; } = 0; // 0-250, 0 = disabled
        public int X265BitDepth { get; set; } = 8; // 8 or 10

        // hevc_nvenc (GPU HEVC) encoder settings
        public int NvencCQ { get; set; } = 21;
        public string NvencPreset { get; set; } = "p5"; // p1-p7 (modern naming)
        public string NvencRateControl { get; set; } = "constqp";
        public bool NvencSpatialAQ { get; set; } = true;
        public bool NvencTemporalAQ { get; set; } = true;
        public int NvencBFrames { get; set; } = 0; // 0-4, 0 = disabled
        public int NvencLookahead { get; set; } = 0; // 0-32, 0 = disabled
        public string NvencMultipass { get; set; } = "disabled"; // disabled, qres, fullres
        public int NvencBitDepth { get; set; } = 8; // 8 or 10

        // av1_nvenc (GPU AV1) encoder settings - RTX 40/50 series
        public int Av1CQ { get; set; } = 23; // 0-51, lower = better quality
        public string Av1Preset { get; set; } = "p5"; // p1-p7
        public string Av1RateControl { get; set; } = "constqp";
        public string Av1Multipass { get; set; } = "disabled"; // disabled, qres, fullres
        public int Av1Lookahead { get; set; } = 0; // 0-32, 0 = disabled

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
