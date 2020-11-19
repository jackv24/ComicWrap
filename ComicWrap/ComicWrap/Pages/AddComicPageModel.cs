using System;
using System.Threading.Tasks;
using FreshMvvm;
using Acr.UserDialogs;
using Rg.Plugins.Popup.Services;
using AsyncAwaitBestPractices.MVVM;
using ComicWrap.Systems;
using Xamarin.Essentials;
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Pages
{
    public class AddComicPageModel : FreshBasePageModel
    {
        public AddComicPageModel()
        {
            CancelCommand = new AsyncCommand(Cancel);
            SubmitCommand = new AsyncCommand(Submit);
        }

        private const string PAGEURL_PREFS_KEY = "AddComic_LastPageUrl"; 
        
        public string PageUrl
        {
            get => Preferences.Get(PAGEURL_PREFS_KEY, string.Empty);
            set
            {
                Preferences.Set(PAGEURL_PREFS_KEY, value);
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand CancelCommand { get; }
        public IAsyncCommand SubmitCommand { get; }

        private Task Cancel()
        {
            return PopupNavigation.Instance.PopAsync();
        }

        private async Task Submit()
        {
            string url = PageUrl;
            if (!TryGetValidUrl(url, out url))
            {
                await UserDialogs.Instance.AlertAsync(Res.AddComic_Error_InvalidUrl);
                return;
            }

            // Pop page immediately, comic will load in background
            await PopupNavigation.Instance.PopAsync();

            ComicUpdater.Instance.StartImportComic(url);
        }

        private bool TryGetValidUrl(string sourceUrl, out string outUrl)
        {
            if (ComicUpdater.IsUrlValid(sourceUrl))
            {
                outUrl = sourceUrl;
                return true;
            }

            // Should hopefully be redirected into https if the server supports it
            string addHttp = "http://" + sourceUrl;
            if (ComicUpdater.IsUrlValid(addHttp))
            {
                outUrl = addHttp;
                return true;
            }
            
            outUrl = string.Empty;
            return false;
        }
    }
}
