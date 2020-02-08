using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Content.Res;
using Plugin.CurrentActivity;
using Xamarin.Forms;

[assembly: Dependency(typeof(ComicWrap.Droid.Environment_Android))]
namespace ComicWrap.Droid
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public class Environment_Android : IEnvironment
    {
        public Theme GetOperatingSystemTheme()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Froyo)
            {
                var config = CrossCurrentActivity.Current.AppContext.Resources.Configuration;
                var uiModeFlags = config.UiMode & UiMode.NightMask;

                switch (uiModeFlags)
                {
                    case UiMode.NightYes:
                        return Theme.Dark;

                    case UiMode.NightNo:
                        return Theme.Light;

                    default:
                        throw new NotSupportedException($"UiMode {uiModeFlags} not supported");
                }
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
    }
}