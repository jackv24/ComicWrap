using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Content.Res;
using Android.Views;
using Xamarin.Forms;
using Plugin.CurrentActivity;

[assembly: Dependency(typeof(ComicWrap.Droid.Environment_Android))]
namespace ComicWrap.Droid
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public class Environment_Android : IEnvironment
    {
        public MainActivity MainActivity { get; set; }

        public Theme GetOperatingSystemTheme()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Froyo)
            {
                var config = CrossCurrentActivity.Current.AppContext.Resources.Configuration;
                var uiModeFlags = config.UiMode & UiMode.NightMask;

                return uiModeFlags switch
                {
                    UiMode.NightYes => Theme.Dark,
                    UiMode.NightNo => Theme.Light,
                    _ => throw new NotSupportedException($"UiMode {uiModeFlags} not supported"),
                };
            }
            else
            {
                return Theme.Light;
            }


        }

        public Task<Theme> GetOperatingSystemThemeAsync()
        {
            return Task.FromResult(GetOperatingSystemTheme());
        }

        public void ApplyTheme(Theme theme)
        {
            if (MainActivity == null)
                return;

            switch (theme)
            {
                case Theme.Dark:
                    MainActivity.SetTheme(Resource.Style.DarkTheme);
                    break;

                case Theme.Light:
                default:
                    MainActivity.SetTheme(Resource.Style.LightTheme);
                    break;
            }
        }
    }
}