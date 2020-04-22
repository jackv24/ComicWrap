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
            RefreshCommand = new AsyncCommand(Refresh);
            OpenSettingsCommand = new AsyncCommand(OpenSettings);

            ComicLibrary = new ObservableCollection<ComicData>();
            ComicUpdates = new ObservableCollection<ComicData>();
        }

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;

        public IAsyncCommand AddComicCommand { get; }
        public IAsyncCommand RefreshCommand { get; }
        public IAsyncCommand OpenSettingsCommand { get; }

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

            pageCancelTokenSource.CancelAndDispose();
            IsRefreshing = false;
        }

        private async Task OpenAddComicPopup()
        {
            var page = (PopupPage)FreshPageModelResolver.ResolvePageModel<AddComicPageModel>();
            await PopupNavigation.Instance.PushAsync(page);
        }

        private async Task Refresh()
        {
            DisplayComics();

            try
            {
                var cancelToken = pageCancelTokenSource.Token;

                // Update comics from internet after loading from database so UI is filled ASAP
                var updateComicTasks = new List<Task>(ComicLibrary.Count);
                foreach (var comic in ComicLibrary)
                    updateComicTasks.Add(ComicUpdater.UpdateComic(comic, cancelToken: cancelToken));

                // Updating comics can all happen at once instead of sequentially for speed
                await Task.WhenAll(updateComicTasks);
            }
            catch (OperationCanceledException) { }
            
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

                if (comic.Pages.Any(page => page.IsNew))
                    ComicUpdates.Add(comic);
            }

            // Sort and reverse lists
            ComicLibrary.Sort((a, b) => CompareDates(a.LastReadDate, b.LastReadDate) * -1);
            ComicUpdates.Sort((a, b) => CompareDates(a.LastUpdatedDate, b.LastUpdatedDate) * -1);

            // Update list bindings
            RaisePropertyChanged(nameof(ComicLibrary));
            RaisePropertyChanged(nameof(ComicUpdates));

            // Update other related bindings
            RaisePropertyChanged(nameof(IsAnyComics));
            RaisePropertyChanged(nameof(IsAnyUpdates));
        }

        private static int CompareDates(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return -1;

            if (b == null)
                return 1;

            return a.Value.CompareTo(b.Value);
        }

        private async Task OpenSettings()
        {
            await CoreMethods.PushPageModel<SettingsPageModel>();
        }
    }
}
