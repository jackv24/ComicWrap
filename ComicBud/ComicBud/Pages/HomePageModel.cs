using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;

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

            if (string.IsNullOrEmpty(result.Text))
                return;

            var comic = new Comic
            {
                ArchiveUrl = result.Text
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
    }
}
