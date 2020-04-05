using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using Rg.Plugins.Popup.Services;
using Rg.Plugins.Popup.Pages;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class HomePageModel : FreshBasePageModel
    {
        public HomePageModel() : base()
        {
            AddComicCommand = new AsyncCommand(OpenAddComicPopup);

            RefreshCommand = new AsyncCommand(async () =>
            {
                try
                {
                    await Refresh();
                }
                catch (OperationCanceledException)
                {
                    // Should cancel silently
                }
            });

            ComicLibrary = new ObservableCollection<ComicData>();
            ComicUpdates = new ObservableCollection<ComicData>();
        }

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;

        public IAsyncCommand AddComicCommand { get; }
        public IAsyncCommand RefreshCommand { get; }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ComicData> ComicLibrary { get; }
        public ObservableCollection<ComicData> ComicUpdates { get; }

        public bool IsAnyComics
        {
            get { return ComicLibrary.Count > 0; }
        }

        public bool IsAnyUpdates
        {
            get { return ComicUpdates.Count > 0; }
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();
            DisplayComics();
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.Cancel();
            IsRefreshing = false;
        }

        private async Task OpenAddComicPopup()
        {
            var page = (PopupPage)FreshPageModelResolver.ResolvePageModel<AddComicPageModel>();
            await PopupNavigation.Instance.PushAsync(page);
        }

        private async Task Refresh()
        {
            var cancelToken = pageCancelTokenSource.Token;

            DisplayComics();

            // Update comics from internet after loading from database so UI is filled ASAP
            foreach (var comic in ComicLibrary)
                await ComicUpdater.UpdateComic(comic, cancelToken: cancelToken);

            RaisePropertyChanged(nameof(IsAnyComics));

            IsRefreshing = false;

            // TODO: Get comic updates in the background
        }

        private void DisplayComics()
        {
            // Load comics from local database
            var loadedComics = ComicDatabase.Instance.GetComics();

            // Update UI
            ComicLibrary.Clear();
            ComicUpdates.Clear();
            foreach (var comic in loadedComics)
            {
                ComicLibrary.Add(comic);
                
                foreach (var page in comic.Pages)
                {
                    if (page.IsNew)
                        ComicUpdates.Add(comic);
                }
            }
        }
    }
}
