using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Diagnostics;

namespace VideoConverterApp
{
    public class YouTubeUploader
    {
        private const string CredentialsFileName = "youtube_credentials.json";
        private const string TokenFileName = "youtube_token.json";
        private YouTubeService? youtubeService;
        private static readonly string[] Scopes = { YouTubeService.Scope.YoutubeUpload };

        /// <summary>
        /// Try to restore authentication from saved token (silent, no UI prompts)
        /// </summary>
        public async Task<bool> TryRestoreAuthenticationAsync()
        {
            try
            {
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsFileName);
                string tokenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TokenFileName);

                // Check if credentials file exists
                if (!File.Exists(credPath))
                    return false;

                // Check if token directory exists with saved tokens
                if (!Directory.Exists(tokenPath))
                    return false;

                var tokenFiles = Directory.GetFiles(tokenPath, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-*");
                if (tokenFiles.Length == 0)
                    return false;

                // Try to load saved credentials silently
                UserCredential credential;
                using (var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(tokenPath, true));
                }

                // Check if token is valid (not expired or can be refreshed)
                if (credential.Token.IsStale)
                {
                    // Try to refresh the token
                    bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
                    if (!refreshed)
                        return false;
                }

                youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Steam Recording Utility"
                });

                return true;
            }
            catch
            {
                // Silent failure - user will need to authenticate manually
                return false;
            }
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CredentialsFileName);
                string tokenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TokenFileName);

                if (!File.Exists(credPath))
                {
                    MessageBox.Show(
                        $"YouTube API credentials file not found!\n\n" +
                        $"Please create '{CredentialsFileName}' with your OAuth 2.0 Client ID.\n\n" +
                        "Instructions:\n" +
                        "1. Go to Google Cloud Console: https://console.cloud.google.com\n" +
                        "2. Create a project and enable YouTube Data API v3\n" +
                        "3. Create OAuth 2.0 credentials (Desktop app)\n" +
                        "4. Download the JSON file and save it as 'youtube_credentials.json'\n" +
                        "5. Place it in the same folder as this application",
                        "YouTube Credentials Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                UserCredential credential;
                using (var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new Google.Apis.Util.Store.FileDataStore(tokenPath, true));
                }

                youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Steam Recording Utility"
                });

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"YouTube authentication failed:\n{ex.Message}\n\n" +
                    "Please check your credentials file and try again.",
                    "Authentication Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        public bool IsAuthenticated => youtubeService != null;

        public async Task<(bool success, string? videoId, string? videoUrl)> UploadVideoAsync(
            string filePath,
            string title,
            string description,
            string[] tags,
            string privacyStatus,
            string categoryId,
            bool madeForKids = false,
            bool ageRestricted = false,
            IProgress<int>? progress = null)
        {
            if (youtubeService == null)
            {
                return (false, null, null);
            }

            try
            {
                var video = new Video
                {
                    Snippet = new VideoSnippet
                    {
                        Title = title,
                        Description = description,
                        Tags = tags,
                        CategoryId = categoryId
                    },
                    Status = new VideoStatus
                    {
                        PrivacyStatus = privacyStatus,
                        SelfDeclaredMadeForKids = madeForKids
                    }
                };

                // Set age restriction if requested (requires content rating)
                if (ageRestricted)
                {
                    video.ContentDetails = new VideoContentDetails
                    {
                        ContentRating = new ContentRating
                        {
                            YtRating = "ytAgeRestricted"
                        }
                    };
                }

                using var fileStream = new FileStream(filePath, FileMode.Open);
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");

                videosInsertRequest.ProgressChanged += (uploadProgress) =>
                {
                    switch (uploadProgress.Status)
                    {
                        case UploadStatus.Uploading:
                            int percent = (int)((uploadProgress.BytesSent * 100) / fileStream.Length);
                            progress?.Report(percent);
                            break;
                        case UploadStatus.Completed:
                            progress?.Report(100);
                            break;
                        case UploadStatus.Failed:
                            progress?.Report(-1);
                            break;
                    }
                };

                var uploadResponse = await videosInsertRequest.UploadAsync();

                if (uploadResponse.Status == UploadStatus.Completed)
                {
                    string videoId = videosInsertRequest.ResponseBody.Id;
                    string videoUrl = $"https://www.youtube.com/watch?v={videoId}";
                    return (true, videoId, videoUrl);
                }
                else
                {
                    return (false, null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Upload failed:\n{ex.Message}",
                    "Upload Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return (false, null, null);
            }
        }

        public string ProcessTemplate(string template, string filePath, bool removeDateFromFilename = false)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileNameWithExt = Path.GetFileName(filePath);
            DateTime now = DateTime.Now;

            // Try to extract recording date from filename (yyyy-MM-dd format)
            string recordingDate = ExtractRecordingDate(fileName);

            // Remove date from filename if requested
            if (removeDateFromFilename)
            {
                fileName = RemoveDateFromString(fileName);
                fileNameWithExt = RemoveDateFromString(fileNameWithExt);
            }

            return template
                .Replace("{filename}", fileName)
                .Replace("{filename_ext}", fileNameWithExt)
                .Replace("{recording_date}", recordingDate)
                .Replace("{date}", now.ToString("yyyy-MM-dd"))
                .Replace("{time}", now.ToString("HH:mm:ss"))
                .Replace("{datetime}", now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{year}", now.Year.ToString())
                .Replace("{month}", now.Month.ToString("D2"))
                .Replace("{day}", now.Day.ToString("D2"));
        }

        /// <summary>
        /// Remove date patterns from a string (yyyy-MM-dd format)
        /// Also cleans up resulting double spaces, leading/trailing separators
        /// </summary>
        private static string RemoveDateFromString(string input)
        {
            // Remove yyyy-MM-dd pattern (e.g., 2024-01-15)
            string result = System.Text.RegularExpressions.Regex.Replace(
                input,
                @"\d{4}-\d{2}-\d{2}",
                "");

            // Clean up double spaces
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");

            // Clean up leading/trailing separators and spaces
            result = result.Trim(' ', '-', '_', '.');

            // Clean up double separators (e.g., "Game -- Title" -> "Game - Title")
            result = System.Text.RegularExpressions.Regex.Replace(result, @"[-_]{2,}", "-");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s*-\s*-\s*", " - ");

            return result;
        }

        /// <summary>
        /// Extract recording date from filename in yyyy-MM-dd format
        /// Returns empty string if no date found
        /// </summary>
        private static string ExtractRecordingDate(string fileName)
        {
            // Match yyyy-MM-dd pattern (e.g., 2024-01-15)
            var match = System.Text.RegularExpressions.Regex.Match(
                fileName,
                @"(\d{4}-\d{2}-\d{2})");

            if (match.Success)
            {
                // Validate it's actually a valid date
                if (DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out _))
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }
    }
}
