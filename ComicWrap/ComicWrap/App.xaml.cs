using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;
using SQLite;

using ComicWrap.Pages;
using ComicWrap.Themes;

namespace ComicWrap
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var page = FreshPageModelResolver.ResolvePageModel<HomePageModel>();
            var navContainer = new FreshNavigationContainer(page);
            MainPage = navContainer;
        }

        protected override void OnStart()
        {
            var theme = DependencyService.Get<IEnvironment>().GetOperatingSystemTheme();
            SetTheme(theme);
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            var theme = DependencyService.Get<IEnvironment>().GetOperatingSystemTheme();
            SetTheme(theme);
        }

        private void SetTheme(Theme theme)
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            if (mergedDictionaries != null && mergedDictionaries.Count > 0)
            {
                mergedDictionaries.Clear();

                switch(theme)
                {
                    case Theme.Dark:
                        mergedDictionaries.Add(new DarkTheme());
                        break;

                    case Theme.Light:
                    default:
                        mergedDictionaries.Add(new LightTheme());
                        break;
                }

                mergedDictionaries.Add(new ElementStyles());
            }
        }
    }
}
