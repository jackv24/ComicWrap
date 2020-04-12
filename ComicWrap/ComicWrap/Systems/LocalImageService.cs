using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ComicWrap.Systems
{
    public static class LocalImageService
    {
        private const Environment.SpecialFolder STORAGE_FOLDER = Environment.SpecialFolder.Personal;
        private const string SUBFOLDER = "Images";

        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(STORAGE_FOLDER), SUBFOLDER);

        public static async Task<string> DownloadImage(Uri url, string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Download image
            WebClient webClient = new WebClient();
            byte[] imageData = await webClient.DownloadDataTaskAsync(url);
            
            cancellationToken.ThrowIfCancellationRequested();

            // Make sure directory exists
            Directory.CreateDirectory(folderPath);

            // Save image data to file
            string filePath = Path.Combine(folderPath, key);
            File.WriteAllBytes(filePath, imageData);

            return filePath;
        }

        public static string GetImagePath(string key)
        {
            // No images have been downloaded yet
            if (!Directory.Exists(folderPath))
                return null;

            string filePath = Path.Combine(folderPath, key);

            // No image exists for key
            if (!File.Exists(filePath))
                return null;

            return filePath;
        }
    }
}
