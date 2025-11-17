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
                    ApplicationName = "Steam Recording Video Converter"
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

        public async Task<(bool success, string? videoId, string? videoUrl)> UploadVideoAsync(
            string filePath,
            string title,
            string description,
            string[] tags,
            string privacyStatus,
            string categoryId,
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
                        PrivacyStatus = privacyStatus
                    }
                };

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

        public string ProcessTemplate(string template, string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileNameWithExt = Path.GetFileName(filePath);
            DateTime now = DateTime.Now;

            return template
                .Replace("{filename}", fileName)
                .Replace("{filename_ext}", fileNameWithExt)
                .Replace("{date}", now.ToString("yyyy-MM-dd"))
                .Replace("{time}", now.ToString("HH:mm:ss"))
                .Replace("{datetime}", now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{year}", now.Year.ToString())
                .Replace("{month}", now.Month.ToString("D2"))
                .Replace("{day}", now.Day.ToString("D2"));
        }
    }
}
