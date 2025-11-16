using Newtonsoft.Json;

namespace VideoConverterApp
{
    public class AppSettings
    {
        public string InputFolder { get; set; } = @"E:\Steam Recordings";
        public string OutputFolder { get; set; } = @"E:\Steam Recordings\stretched";
        public int OutputWidth { get; set; } = 1920;
        public int OutputHeight { get; set; } = 1080;
        public double Saturation { get; set; } = 1.2;
        public int CRF { get; set; } = 18;
        public int Bitrate { get; set; } = 20000;
        public bool MoveProcessedFiles { get; set; } = true;

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
