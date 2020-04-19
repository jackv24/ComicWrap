using System;
using System.Threading.Tasks;

namespace ComicWrap.Systems
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public interface IEnvironment
    {
        SystemTheme GetOperatingSystemTheme();
        Task<SystemTheme> GetOperatingSystemThemeAsync();
        void ApplyTheme(SystemTheme theme);
    }

    public enum SystemTheme { Light, Dark }
}
