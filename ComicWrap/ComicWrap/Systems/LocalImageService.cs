using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ComicWrap.Systems
{
    public static class LocalImageService
    {
        // If these are changed make sure to update backup exclusions in Android backup_rules.xml
        public const Environment.SpecialFolder STORAGE_FOLDER = Environment.SpecialFolder.Personal;
        public const string SUBFOLDER = "Images";

        public static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(STORAGE_FOLDER), SUBFOLDER);

        public static async Task<string> DownloadImage(Uri url, string key, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;
            
            // Download image
            WebClient webClient = new WebClient()
            {
                // Avoid potentially time consuming proxy detection
                Proxy = null
            };
            byte[] imageData = await webClient.DownloadDataTaskAsync(url);

            if (cancellationToken.IsCancellationRequested)
                return null;

            // Make sure directory exists
            Directory.CreateDirectory(FolderPath);

            // Save image data to file
            string filePath = Path.Combine(FolderPath, key);
            File.WriteAllBytes(filePath, imageData);

            return filePath;
        }

        public static string GetImagePath(string key)
        {
            // No images have been downloaded yet
            if (!Directory.Exists(FolderPath))
                return null;

            string filePath = Path.Combine(FolderPath, key);

            // No image exists for key
            if (!File.Exists(filePath))
                return null;

            return filePath;
        }
    }
}
