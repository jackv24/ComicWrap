using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Content.Res;
using Android.Views;
using Xamarin.Forms;
using Plugin.CurrentActivity;
using ComicWrap.Systems;
using ComicWrap.Droid.Systems;

[assembly: Dependency(typeof(Environment_Android))]
namespace ComicWrap.Droid.Systems
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public class Environment_Android : IEnvironment
    {
        public MainActivity MainActivity { get; set; }

        public SystemTheme GetOperatingSystemTheme()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Froyo)
            {
                var config = CrossCurrentActivity.Current.AppContext.Resources.Configuration;
                var uiModeFlags = config.UiMode & UiMode.NightMask;

                return uiModeFlags switch
                {
                    UiMode.NightYes => SystemTheme.Dark,
                    UiMode.NightNo => SystemTheme.Light,
                    _ => throw new NotSupportedException($"UiMode {uiModeFlags} not supported"),
                };
            }
            else
            {
                return SystemTheme.Light;
            }


        }

        public Task<SystemTheme> GetOperatingSystemThemeAsync()
        {
            return Task.FromResult(GetOperatingSystemTheme());
        }

        public void ApplyTheme(SystemTheme theme)
        {
            if (MainActivity == null)
                return;

            switch (theme)
            {
                case SystemTheme.Dark:
                    MainActivity.SetTheme(Resource.Style.DarkTheme);
                    break;

                case SystemTheme.Light:
                default:
                    MainActivity.SetTheme(Resource.Style.LightTheme);
                    break;
            }
        }
    }
}