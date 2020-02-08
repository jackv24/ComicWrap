using System;
using System.Threading.Tasks;

namespace ComicWrap
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public interface IEnvironment
    {
        Theme GetOperatingSystemTheme();
        Task<Theme> GetOperatingSystemThemeAsync();
        void ApplyTheme(Theme theme);
    }

    public enum Theme { Light, Dark }
}
