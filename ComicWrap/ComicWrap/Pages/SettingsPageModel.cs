using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
            return CoreMethods.DisplayAlert("Error", "Data backup is not yet implemented", "OK");
        }

        private Task RestoreData()
        {
            return CoreMethods.DisplayAlert("Error", "Data restore is not yet implemented", "OK");
        }
    }
}
