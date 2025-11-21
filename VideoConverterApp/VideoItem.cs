namespace VideoConverterApp
{
    public class VideoItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Selected { get; set; } = true;

        // Per-video conversion settings
        public double Brightness { get; set; } = 0.0;  // -1.0 to 1.0
        public double Contrast { get; set; } = 1.0;    // 0.0 to 4.0
        public double Saturation { get; set; } = 1.2;  // 0.0 to 3.0
        public int OutputWidth { get; set; } = 1920;
        public int OutputHeight { get; set; } = 1080;

        // Preview frames (cached)
        public Image? PreviewFrame40 { get; set; }
        public Image? PreviewFrame60 { get; set; }
        public Image? PreviewFrame40Filtered { get; set; }
        public Image? PreviewFrame60Filtered { get; set; }

        public VideoItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public void ClearPreviewCache()
        {
            PreviewFrame40?.Dispose();
            PreviewFrame60?.Dispose();
            PreviewFrame40Filtered?.Dispose();
            PreviewFrame60Filtered?.Dispose();
            PreviewFrame40 = null;
            PreviewFrame60 = null;
            PreviewFrame40Filtered = null;
            PreviewFrame60Filtered = null;
        }
    }
}
