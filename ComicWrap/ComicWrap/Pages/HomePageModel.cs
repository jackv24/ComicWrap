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

            Comics = new ObservableCollection<ComicData>();
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

        public ObservableCollection<ComicData> Comics { get; }

        public bool IsAnyComics
        {
            get { return Comics.Count > 0; }
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();

            // Will call refresh command (don't set true in manually called refresh command
            // or it will be called multiple times)
            IsRefreshing = true;
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

            // Load comics from local database
            var loadedComics = ComicDatabase.Instance.GetComics();

            // Update UI
            Comics.Clear();
            foreach (var comic in loadedComics)
                Comics.Add(comic);

            // Update comics from internet after loading from database so UI is filled ASAP
            foreach (var comic in loadedComics)
                await ComicUpdater.UpdateComic(comic, cancelToken: cancelToken);

            RaisePropertyChanged(nameof(IsAnyComics));

            IsRefreshing = false;

            // TODO: Get comic updates in the background
        }
    }
}
