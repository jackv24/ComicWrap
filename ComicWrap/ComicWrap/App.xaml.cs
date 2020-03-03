using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using FreshMvvm;

using ComicWrap.Pages;
using ComicWrap.Themes;
using Secrets = ComicWrap.Helpers.Secrets;

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
            AppCenter.Start($"ios={Secrets.AppSecret_iOS};android={Secrets.AppSecret_Android}", typeof(Analytics), typeof(Crashes));

            UpdateTheme();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            UpdateTheme();
        }

        private void UpdateTheme()
        {
            var environment = DependencyService.Get<IEnvironment>();
            var theme = environment.GetOperatingSystemTheme();

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            if (mergedDictionaries != null)
            {
                mergedDictionaries.Clear();

                ResourceDictionary themeDict;
                switch (theme)
                {
                    case Theme.Dark:
                        themeDict = new DarkTheme();
                        break;

                    case Theme.Light:
                    default:
                        themeDict = new LightTheme();
                    break;
                }

                var styleDict = new ElementStyles();

                mergedDictionaries.Add(themeDict);
                styleDict.MergedDictionaries.Add(themeDict);
                mergedDictionaries.Add(styleDict);
            }

            environment.ApplyTheme(theme);
        }
    }
}
