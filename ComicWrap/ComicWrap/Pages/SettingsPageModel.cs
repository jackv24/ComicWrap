using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.IO.Compression;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

using FreshMvvm;
using AsyncAwaitBestPractices.MVVM;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;

using ComicWrap.Systems;
using ComicWrap.Helpers;
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Pages
{
    public class SettingsPageModel : FreshBasePageModel
    {
        public class ThemeOptionDisplay
        {
            public Settings.ThemeSetting Theme { get; set; }
            public string Name { get; set; }
        }

        public SettingsPageModel()
        {
            ThemeOptions = Enum.GetValues(typeof(Settings.ThemeSetting))
                .Cast<Settings.ThemeSetting>()
                .Select(theme => new ThemeOptionDisplay
                {
                    Theme = theme,
                    // Get theme name using reflection (only at startup since reflection is expensive)
                    Name = typeof(Res).GetProperty($"Settings_Theme_{theme}")?.GetValue(null) as string ?? $"!{theme}!"
                })
                .ToList();

            SelectedTheme = ThemeOptions[(int)Settings.Theme];

            BackupDataCommand = new AsyncCommand(BackupData);
            RestoreDataCommand = new AsyncCommand(RestoreData);

            OpenSupportUrlCommand = new AsyncCommand(OpenSupportUrl);
        }

        public List<ThemeOptionDisplay> ThemeOptions { get; }

        public IAsyncCommand BackupDataCommand { get; }
        public IAsyncCommand RestoreDataCommand { get; }

        public string SupportUrl { get { return "https://github.com/jackv24/ComicWrap"; } }
        public IAsyncCommand OpenSupportUrlCommand { get; }

        private ThemeOptionDisplay _selectedTheme;
        public ThemeOptionDisplay SelectedTheme
        {
            get { return _selectedTheme; }
            set
            {
                _selectedTheme = value;
                Settings.Theme = value.Theme;
                RaisePropertyChanged();
            }
        }

        private Task BackupData()
        {
            // TODO: Tests

            // Save to cache directory temporarily
            string file = Path.Combine(FileSystem.CacheDirectory, "Backup.zip");
            if (File.Exists(file))
                File.Delete(file);

            // Temporary folder to be zipped
            string backupFolder = Path.Combine(FileSystem.CacheDirectory, "Backup");
            if (Directory.Exists(backupFolder))
                Directory.Delete(backupFolder, true);
            Directory.CreateDirectory(backupFolder);

            // Copy files to temp folder
            // Database file should always exist
            File.Copy(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "default.realm"),
                Path.Combine(backupFolder, "default.realm"));

            // Images folder may not exist
            if (Directory.Exists(LocalImageService.FolderPath))
                FileHelper.DirectoryCopy(LocalImageService.FolderPath, Path.Combine(backupFolder, LocalImageService.SUBFOLDER), true);

            // Create zip of from temp folder
            ZipFile.CreateFromDirectory(backupFolder, file);

            return Share.RequestAsync(new ShareFileRequest()
            {
                File = new ShareFile(file),
                
                // Position at bottom of screen on iPad (too hard to get button bounds from SettingsView)
                PresentationSourceBounds = new System.Drawing.Rectangle(
                    (int)CurrentPage.Bounds.Center.X,
                    (int)CurrentPage.Bounds.Bottom,
                    0,
                    0)
            });
        }

        private async Task RestoreData()
        {
            // TODO: Tests

            // Temporary folder to extract backup into for validation, copying, etc.
            string backupFolder = Path.Combine(FileSystem.CacheDirectory, "Restore");
            if (Directory.Exists(backupFolder))
                Directory.Delete(backupFolder, true);

            string[] allowedTypes;
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    allowedTypes = new[] { "com.pkware.zip-archive" };
                    break;

                case Device.Android:
                    allowedTypes = new[] { "application/zip" };
                    break;

                default:
                    throw new NotImplementedException();
            }

            // Select backup zip file
            using (FileData fileData = await CrossFilePicker.Current.PickFile(allowedTypes))
            {
                // User cancelled file picking
                if (fileData == null)
                    return;
                
                // Extract zip file to temp folder
                using (var stream = fileData.GetStream())
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(backupFolder);
                }
            }

            string databaseBackupPath = Path.Combine(backupFolder, "default.realm");

            // Validate backup contents (backup is not valid if it does not contain a database file)
            if (!File.Exists(databaseBackupPath))
            {
                await CoreMethods.DisplayAlert(
                    title: Res.Alert_Error_Title,
                    message: Res.Settings_Restore_Error_Invalid,
                    cancel: Res.Alert_Generic_Confirm);

                return;
            }

            // Must close database connection before editing database file
            var database = ComicDatabase.Instance;
            database.Close();

            // Replace database file with zip contents
            string databaseRestorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "default.realm");
            File.Delete(databaseRestorePath);
            File.Copy(databaseBackupPath, databaseRestorePath);

            // Reopen database connection now that database file is back in place
            database.Open();

            string imageFolderBackupPath = Path.Combine(backupFolder, LocalImageService.SUBFOLDER);
            if (Directory.Exists(imageFolderBackupPath))
            {
                if (Directory.Exists(LocalImageService.FolderPath))
                    Directory.Delete(LocalImageService.FolderPath, true);

                FileHelper.DirectoryCopy(imageFolderBackupPath, LocalImageService.FolderPath, true);
            }
        }

        private Task OpenSupportUrl()
        {
            return Launcher.OpenAsync(SupportUrl);
        }
    }
}
