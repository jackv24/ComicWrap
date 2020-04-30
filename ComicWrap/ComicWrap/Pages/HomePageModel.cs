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

            importingComics = new List<ComicData>();
        }

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;

        private ComicUpdater comicUpdater;
        private List<ComicData> importingComics;

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

            // Subscribe to ComicUpdater events so page can be updated while importing comics
            comicUpdater = ComicUpdater.Instance;
            comicUpdater.ImportComicBegun += OnImportComicBegun;
            comicUpdater.ImportComicProgressed += OnImportComicProgressed;
            comicUpdater.ImportComicFinished += OnImportComicFinished;
            
            // Handle comics that were already importing
            foreach (var comic in comicUpdater.ImportingComics)
                OnImportComicBegun(comic);

            DisplayComics();
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.CancelAndDispose();
            IsRefreshing = false;

            // Only need to update page while active
            comicUpdater.ImportComicBegun -= OnImportComicBegun;
            comicUpdater.ImportComicProgressed -= OnImportComicProgressed;
            comicUpdater.ImportComicFinished -= OnImportComicFinished;
            importingComics.Clear();
        }

        private Task OpenAddComicPopup()
        {
            var page = (PopupPage)FreshPageModelResolver.ResolvePageModel<AddComicPageModel>();
            return PopupNavigation.Instance.PushAsync(page);
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
                    updateComicTasks.Add(comicUpdater.UpdateComic(comic, cancelToken: cancelToken));

                // Updating comics can all happen at once instead of sequentially for speed
                await Task.WhenAll(updateComicTasks);
            }
            catch (OperationCanceledException) { }

            DisplayComics();

            IsRefreshing = false;

            // TODO: Get comic updates in the background
        }

        private void DisplayComics()
        {
            // Load comics from local database
            List<ComicData> newComicLibrary = ComicDatabase.Instance.GetComics();

            // Get comics that have new pages
            var newComicUpdates = new List<ComicData>();
            foreach (var comic in newComicLibrary)
            {
                if (comic.Pages.Any(page => page.IsNew))
                    newComicUpdates.Add(comic);
            }

            // Sort and reverse lists
            newComicLibrary.Sort((a, b) => CompareDates(a.LastReadDate, b.LastReadDate) * -1);
            newComicUpdates.Sort((a, b) => CompareDates(a.LastUpdatedDate, b.LastUpdatedDate) * -1);

            // Display importing comics at top of list
            foreach (var comic in importingComics)
                newComicLibrary.Insert(0, comic);

            // Make ObservableCollections match new lists (done this way so UI is animated with changes)
            ComicLibrary.MatchList(newComicLibrary);
            ComicUpdates.MatchList(newComicUpdates);

            // Update list bindings
            RaisePropertyChanged(nameof(ComicLibrary));
            RaisePropertyChanged(nameof(ComicUpdates));

            // Update other related bindings
            RaisePropertyChanged(nameof(IsAnyComics));
            RaisePropertyChanged(nameof(IsAnyUpdates));
        }

        private Task OpenSettings()
        {
            return CoreMethods.PushPageModel<SettingsPageModel>();
        }

        private void OnImportComicBegun(ComicData comic)
        {
            importingComics.Add(comic);

            // Just add this comic to start of Library list - no need to refresh whole page
            ComicLibrary.Insert(0, comic);

            RaisePropertyChanged(nameof(ComicLibrary));
            RaisePropertyChanged(nameof(IsAnyComics));
        }

        private void OnImportComicProgressed(ComicData comic)
        {
            // Update views that are displaying this comic
            comic.ReportUpdated();
        }

        private void OnImportComicFinished(ComicData comic)
        {
            importingComics.Remove(comic);

            // Need to refresh whole list for correct ordering
            DisplayComics();
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
    }
}
