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
using Acr.UserDialogs;
using Rg.Plugins.Popup.Services;
using Rg.Plugins.Popup.Pages;
using AngleSharp;
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
        }

        public List<ThemeOptionDisplay> ThemeOptions { get; set; }

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
    }
}
