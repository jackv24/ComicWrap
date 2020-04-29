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

using ComicWrap.Systems;
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
        }

        public List<ThemeOptionDisplay> ThemeOptions { get; }

        public IAsyncCommand BackupDataCommand { get; }
        public IAsyncCommand RestoreDataCommand { get; }

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
                DirectoryCopy(LocalImageService.FolderPath, Path.Combine(backupFolder, LocalImageService.SUBFOLDER), true);

            // Create zip of from temp folder
            ZipFile.CreateFromDirectory(backupFolder, file);

            return Share.RequestAsync(new ShareFileRequest()
            {
                File = new ShareFile(file)
            });
        }

        private Task RestoreData()
        {
            // TODO: Implement, tests

            return CoreMethods.DisplayAlert("!Error!", "!Data restore is not yet implemented!", "!OK!");
        }

        // From: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        // TODO: Move into another helpers class
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
