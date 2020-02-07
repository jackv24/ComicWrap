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

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class AddComicPageModel : FreshBasePageModel
    {
        public AddComicPageModel()
        {
            CancelCommand = new Command(async () => await Cancel());
            SubmitCommand = new Command(async () => await Submit());
        }

        private string _archivePageUrl;
        public string ArchivePageUrl
        {
            get { return _archivePageUrl; }
            set
            {
                _archivePageUrl = value;
                RaisePropertyChanged();
            }
        }

        private string _currentPageUrl;
        public string CurrentPageUrl
        {
            get { return _currentPageUrl; }
            set
            {
                _currentPageUrl = value;
                RaisePropertyChanged();
            }
        }

        public Command CancelCommand { get; }
        public Command SubmitCommand { get; }

        private async Task Cancel()
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private async Task Submit()
        {
            string url = ArchivePageUrl;
            if (!ComicUpdater.IsUrlValid(url))
            {
                await UserDialogs.Instance.AlertAsync("URL is invalid.");
                return;
            }

            // Pop page immediately, comic will load in background
            // TODO: Extract comic importing code elsewhere
            await PopupNavigation.Instance.PopAsync();

            string webpageName;
            // TEMP
            {
                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(url);
                webpageName = document.Title;
            }

            var comic = new ComicData
            {
                Name = webpageName,
                ArchiveUrl = url
            };

            // Add comic to database before adding pages, so the ID auto set
            ComicDatabase.Instance.SetData(comic);

            var pages = await ComicUpdater.GetPages(url);
            foreach (var page in pages)
            {
                page.ComicId = comic.Id;
                ComicDatabase.Instance.SetData(page);
            }
        }
    }
}
