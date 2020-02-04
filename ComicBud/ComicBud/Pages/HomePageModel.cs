using System;
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

            Comics = new ObservableCollection<Comic>();

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

        public ObservableCollection<Comic> Comics { get; }

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

            string url = result.Text;

            if (string.IsNullOrEmpty(url))
                return;

            if (!IsUrlValid(url))
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

            var comic = new Comic
            {
                Name = webpageName,
                ArchiveUrl = url
            };

            ComicDatabase.Instance.UpdateComic(comic);
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
            var loadedComics = ComicDatabase.Instance.GetComics();
            foreach (var comic in loadedComics)
                Comics.Add(comic);

            RaisePropertyChanged(nameof(IsAnyComics));
        }

        private bool IsUrlValid(string url)
        {
            try
            {
                var uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                return false;
            }

            return true;
        }
    }
}
