using System.Diagnostics;
using System.Globalization;

namespace VideoConverterApp
{
    public partial class MainForm
    {
        private void LoadSettings()
        {
            txtInputFolder.Text = settings.InputFolder;
            txtOutputFolder.Text = settings.OutputFolder;
            numCRF.Value = settings.CRF;
            numBitrate.Value = settings.Bitrate;
            chkMoveProcessed.Checked = settings.MoveProcessedFiles;

            // Load YouTube settings
            chkEnableYouTube.Checked = settings.EnableYouTubeUpload;
            txtYouTubeTitleTemplate.Text = settings.YouTubeTitleTemplate;
            txtYouTubeDescriptionTemplate.Text = settings.YouTubeDescriptionTemplate;
            txtYouTubeTags.Text = settings.YouTubeTags;
            cmbYouTubePrivacy.Text = settings.YouTubePrivacyStatus;

            string categoryDisplay = settings.YouTubeCategoryId switch
            {
                "20" => "Gaming (20)",
                "24" => "Entertainment (24)",
                "22" => "People & Blogs (22)",
                _ => "Gaming (20)"
            };
            cmbYouTubeCategory.Text = categoryDisplay;
        }

        private void SaveSettings()
        {
            settings.InputFolder = txtInputFolder.Text;
            settings.OutputFolder = txtOutputFolder.Text;
            settings.CRF = (int)numCRF.Value;
            settings.Bitrate = (int)numBitrate.Value;
            settings.MoveProcessedFiles = chkMoveProcessed.Checked;

            // YouTube settings
            settings.EnableYouTubeUpload = chkEnableYouTube.Checked;
            settings.YouTubeTitleTemplate = txtYouTubeTitleTemplate.Text;
            settings.YouTubeDescriptionTemplate = txtYouTubeDescriptionTemplate.Text;
            settings.YouTubeTags = txtYouTubeTags.Text;
            settings.YouTubePrivacyStatus = cmbYouTubePrivacy.Text;

            string categoryText = cmbYouTubeCategory.Text;
            string categoryId = categoryText.Split('(').Last().TrimEnd(')');
            settings.YouTubeCategoryId = categoryId;

            settings.Save();
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtInputFolder.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtInputFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtOutputFolder.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnLoadVideos_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInputFolder.Text) || !Directory.Exists(txtInputFolder.Text))
            {
                MessageBox.Show("Please select a valid input folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnLoadVideos.Enabled = false;
            lblProgress.Text = "Loading videos...";
            lstVideos.Items.Clear();

            // Clear preview images from PictureBoxes to release references
            pic40Before.Image = null;
            pic60Before.Image = null;
            pic40After.Image = null;
            pic60After.Image = null;

            // Dispose all cached preview images before clearing the list
            foreach (var video in videoItems)
            {
                video.ClearPreviewCache();
            }

            videoItems.Clear();

            try
            {
                var files = Directory.GetFiles(txtInputFolder.Text, "*.mp4", SearchOption.TopDirectoryOnly);

                if (files.Length == 0)
                {
                    MessageBox.Show("No MP4 files found in the input folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                foreach (var file in files)
                {
                    var item = new VideoItem(file)
                    {
                        Brightness = settings.Brightness,
                        Contrast = settings.Contrast,
                        Saturation = settings.Saturation,
                        OutputWidth = settings.OutputWidth,
                        OutputHeight = settings.OutputHeight
                    };
                    videoItems.Add(item);
                    lstVideos.Items.Add($"☑ {item.FileName}");
                }

                lblProgress.Text = $"Loaded {videoItems.Count} video(s)";
                LogInfo($"Loaded {videoItems.Count} video(s) from {txtInputFolder.Text}");

                if (videoItems.Count > 0)
                {
                    lstVideos.SelectedIndex = 0;
                    btnConvertAll.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading videos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError($"Error loading videos: {ex.Message}");
            }
            finally
            {
                btnLoadVideos.Enabled = true;
            }
        }

        private async void LstVideos_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstVideos.SelectedIndex < 0 || lstVideos.SelectedIndex >= videoItems.Count)
                return;

            currentVideo = videoItems[lstVideos.SelectedIndex];

            // Update trackbars to show current video's settings
            trackBrightness.ValueChanged -= TrackAdjustments_ValueChanged;
            trackContrast.ValueChanged -= TrackAdjustments_ValueChanged;
            trackSaturation.ValueChanged -= TrackAdjustments_ValueChanged;

            trackBrightness.Value = (int)(currentVideo.Brightness * 100);
            trackContrast.Value = (int)(currentVideo.Contrast * 100);
            trackSaturation.Value = (int)(currentVideo.Saturation * 100);

            trackBrightness.ValueChanged += TrackAdjustments_ValueChanged;
            trackContrast.ValueChanged += TrackAdjustments_ValueChanged;
            trackSaturation.ValueChanged += TrackAdjustments_ValueChanged;

            UpdateValueLabels();

            // Load and show previews
            await LoadPreviewsAsync();
        }

        private async Task LoadPreviewsAsync()
        {
            if (currentVideo == null) return;

            lblProgress.Text = "Loading previews...";
            btnRefreshPreview.Enabled = false;

            try
            {
                // Check if we need to extract original frames
                if (currentVideo.PreviewFrame40 == null || currentVideo.PreviewFrame60 == null)
                {
                    var (frame40, frame60) = await FrameExtractor.ExtractFramesAsync(currentVideo.FilePath);
                    currentVideo.PreviewFrame40 = frame40;
                    currentVideo.PreviewFrame60 = frame60;
                }

                // Show original frames
                pic40Before.Image = currentVideo.PreviewFrame40;
                pic60Before.Image = currentVideo.PreviewFrame60;

                // Generate filtered previews
                await RegenerateFilteredPreviewsAsync();

                lblProgress.Text = "Ready";
            }
            catch (Exception ex)
            {
                LogError($"Error loading previews: {ex.Message}");
                lblProgress.Text = "Error loading previews";
            }
            finally
            {
                btnRefreshPreview.Enabled = true;
            }
        }

        private async Task RegenerateFilteredPreviewsAsync()
        {
            if (currentVideo == null) return;

            try
            {
                // Clear old filtered previews
                currentVideo.PreviewFrame40Filtered?.Dispose();
                currentVideo.PreviewFrame60Filtered?.Dispose();

                // Generate new filtered previews
                var task40 = FrameExtractor.GenerateFilteredPreviewAsync(
                    currentVideo.FilePath,
                    40,
                    currentVideo.Brightness,
                    currentVideo.Contrast,
                    currentVideo.Saturation);

                var task60 = FrameExtractor.GenerateFilteredPreviewAsync(
                    currentVideo.FilePath,
                    60,
                    currentVideo.Brightness,
                    currentVideo.Contrast,
                    currentVideo.Saturation);

                await Task.WhenAll(task40, task60);

                currentVideo.PreviewFrame40Filtered = await task40;
                currentVideo.PreviewFrame60Filtered = await task60;

                // Display
                pic40After.Image = currentVideo.PreviewFrame40Filtered;
                pic60After.Image = currentVideo.PreviewFrame60Filtered;
            }
            catch (Exception ex)
            {
                LogError($"Error generating filtered previews: {ex.Message}");
            }
        }

        private void TrackAdjustments_ValueChanged(object? sender, EventArgs e)
        {
            if (currentVideo == null) return;

            currentVideo.Brightness = trackBrightness.Value / 100.0;
            currentVideo.Contrast = trackContrast.Value / 100.0;
            currentVideo.Saturation = trackSaturation.Value / 100.0;

            UpdateValueLabels();

            // Auto-refresh preview after user stops adjusting (debounced)
            previewRefreshTimer?.Stop();
            previewRefreshTimer?.Start();
        }

        private async void PreviewRefreshTimer_Tick(object? sender, EventArgs e)
        {
            previewRefreshTimer?.Stop();
            await RegenerateFilteredPreviewsAsync();
        }

        private void UpdateValueLabels()
        {
            if (currentVideo == null) return;

            lblBrightnessValue.Text = currentVideo.Brightness.ToString("0.00");
            lblContrastValue.Text = currentVideo.Contrast.ToString("0.00");
            lblSaturationValue.Text = currentVideo.Saturation.ToString("0.00");
        }

        private async void BtnRefreshPreview_Click(object? sender, EventArgs e)
        {
            await RegenerateFilteredPreviewsAsync();
        }

        private void BtnApplyToAll_Click(object? sender, EventArgs e)
        {
            if (currentVideo == null) return;

            var result = MessageBox.Show(
                $"Apply current settings to all {videoItems.Count} videos?\n\n" +
                $"Brightness: {currentVideo.Brightness:0.00}\n" +
                $"Contrast: {currentVideo.Contrast:0.00}\n" +
                $"Saturation: {currentVideo.Saturation:0.00}",
                "Apply to All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (var video in videoItems)
                {
                    video.Brightness = currentVideo.Brightness;
                    video.Contrast = currentVideo.Contrast;
                    video.Saturation = currentVideo.Saturation;
                    video.OutputWidth = currentVideo.OutputWidth;
                    video.OutputHeight = currentVideo.OutputHeight;

                    // Clear cached filtered previews since settings changed
                    video.PreviewFrame40Filtered?.Dispose();
                    video.PreviewFrame60Filtered?.Dispose();
                    video.PreviewFrame40Filtered = null;
                    video.PreviewFrame60Filtered = null;
                }

                LogInfo("Applied current settings to all videos");
                MessageBox.Show("Settings applied to all videos!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            if (currentVideo == null) return;

            currentVideo.Brightness = 0.0;
            currentVideo.Contrast = 1.0;
            currentVideo.Saturation = 1.2;

            trackBrightness.Value = 0;
            trackContrast.Value = 100;
            trackSaturation.Value = 120;

            UpdateValueLabels();
        }

        private void ChkEnableYouTube_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateYouTubeControlsEnabled();
        }

        private void UpdateYouTubeControlsEnabled()
        {
            bool enabled = chkEnableYouTube.Checked;
            txtYouTubeTitleTemplate.Enabled = enabled;
            txtYouTubeDescriptionTemplate.Enabled = enabled;
            txtYouTubeTags.Enabled = enabled;
            cmbYouTubePrivacy.Enabled = enabled;
            cmbYouTubeCategory.Enabled = enabled;
            btnYouTubeAuth.Enabled = enabled;
        }

        private async void BtnYouTubeAuth_Click(object? sender, EventArgs e)
        {
            btnYouTubeAuth.Enabled = false;
            lblYouTubeStatus.Text = "Authenticating...";
            lblYouTubeStatus.ForeColor = Color.Gray;

            try
            {
                youtubeUploader = new YouTubeUploader();
                bool success = await youtubeUploader.AuthenticateAsync();

                if (success)
                {
                    lblYouTubeStatus.Text = "Authenticated successfully!";
                    lblYouTubeStatus.ForeColor = Color.Green;
                    LogSuccess("YouTube authentication successful");
                }
                else
                {
                    lblYouTubeStatus.Text = "Authentication failed";
                    lblYouTubeStatus.ForeColor = Color.Red;
                    youtubeUploader = null;
                }
            }
            catch (Exception ex)
            {
                lblYouTubeStatus.Text = $"Error: {ex.Message}";
                lblYouTubeStatus.ForeColor = Color.Red;
                youtubeUploader = null;
            }
            finally
            {
                btnYouTubeAuth.Enabled = true;
            }
        }

        // Conversion logic continues in next file...
    }
}
