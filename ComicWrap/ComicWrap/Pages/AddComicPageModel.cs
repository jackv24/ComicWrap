using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
    public class AddComicPageModel : FreshBasePageModel
    {
        public AddComicPageModel()
        {
            CancelCommand = new AsyncCommand(Cancel);
            SubmitCommand = new AsyncCommand(Submit);
        }

        private string _pageUrl;
        public string PageUrl
        {
            get { return _pageUrl; }
            set
            {
                _pageUrl = value;
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
            if (!ComicUpdater.IsUrlValid(url))
            {
                await UserDialogs.Instance.AlertAsync(Res.AddComic_Error_InvalidUrl);
                return;
            }

            // Pop page immediately, comic will load in background
            await PopupNavigation.Instance.PopAsync();

            ComicUpdater.Instance.StartImportComic(url);
        }
    }
}
