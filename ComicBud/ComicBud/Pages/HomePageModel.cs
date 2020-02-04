using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;
using AngleSharp;

using ComicBud.Systems;

namespace ComicBud.Pages
{
    public class HomePageModel : FreshBasePageModel
    {
        public HomePageModel() : base()
        {
            AddComicUrlCommand = new Command(async () => await AddComicUrl());
            RefreshCommand = new Command(Refresh);

            Comics = new ObservableCollection<ComicData>();

            ReloadComics();
        }

        private bool _isRefreshing;

        public Command AddComicUrlCommand { get; }
        public Command RefreshCommand { get; }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ComicData> Comics { get; }

        public bool IsAnyComics
        {
            get { return Comics.Count > 0; }
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            Refresh();
        }

        private async Task AddComicUrl()
        {
            PromptResult result = await UserDialogs.Instance.PromptAsync(
                "Enter webcomic archive page URL",
                title: "Add Webcomic",
                okText: "Add",
                cancelText: "Cancel",
                inputType: InputType.Url);


            if (!result.Ok)
                return;

            string url = result.Text;
            if (!ComicUpdater.IsUrlValid(url))
            {
                await UserDialogs.Instance.AlertAsync("URL is invalid.");

                // Stay in add comic until given url is valid or cancelled
                await AddComicUrl();
                return;
            }

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

            ReloadComics();
        }

        private void Refresh()
        {
            IsRefreshing = true;
            ReloadComics();
            IsRefreshing = false;
        }

        private void ReloadComics()
        {
            Comics.Clear();
            var loadedComics = ComicDatabase.Instance.GetData<ComicData>();
            foreach (var comic in loadedComics)
                Comics.Add(comic);

            RaisePropertyChanged(nameof(IsAnyComics));
        }
    }
}
