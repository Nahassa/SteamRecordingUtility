using Newtonsoft.Json;

namespace VideoConverterApp
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
        public int CRF { get; set; } = 18;
        public int Bitrate { get; set; } = 20000;
        public string VideoEncoder { get; set; } = "libx265"; // libx265, hevc_nvenc, hevc_nvenc_hq
        public bool MoveProcessedFiles { get; set; } = true;

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
