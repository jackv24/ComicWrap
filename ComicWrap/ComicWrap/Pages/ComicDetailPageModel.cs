using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel()
        {
            OpenOptionsCommand = new AsyncCommand(OpenOptions);

            RefreshCommand = new AsyncCommand(async () =>
            {
                try
                {
                    IsRefreshing = true;

                    var cancelToken = pageCancelTokenSource.Token;

                    // Refresh without updating first so list loads quickly
                    cancelToken.ThrowIfCancellationRequested();
                    await Refresh(doUpdate: false, cancelToken);

                    cancelToken.ThrowIfCancellationRequested();
                    await Refresh(doUpdate: true, cancelToken);

                    IsRefreshing = false;
                }
                catch (OperationCanceledException)
                {
                    // Should cancel silently
                }
            });

            OpenPageCommand = new AsyncCommand<ComicPageData>(OpenPage);
        }

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;
        private ComicData _comic;

        public IAsyncCommand OpenOptionsCommand { get; }
        public IAsyncCommand RefreshCommand { get; }
        public IAsyncCommand<ComicPageData> OpenPageCommand { get; }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        public ComicData Comic
        {
            get { return _comic; }
            private set
            {
                _comic = value;

                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ComicPageData> Pages { get; set; }

        public override void Init(object initData)
        {
            Comic = initData as ComicData;

            Pages = new ObservableCollection<ComicPageData>();
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();

            if (RefreshCommand.CanExecute(null))
                RefreshCommand.Execute(null);
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.Cancel();
            IsRefreshing = false;
        }

        private async Task OpenOptions()
        {
            string buttonPressed = await UserDialogs.Instance.ActionSheetAsync(
                title: "Comic Options",
                cancel: "Cancel",
                destructive: "Delete"
                );

            switch (buttonPressed)
            {
                case "Cancel":
                    return;

                case "Delete":
                    await ComicDatabase.Instance.DeleteComic(Comic);
                    break;

                default:
                    throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
            UserDialogs.Instance.Toast($"Deleted Comic: {Comic.Name}");
        }

        private async Task Refresh(bool doUpdate, CancellationToken cancelToken)
        {
            var newPages = doUpdate
                ? await ComicUpdater.UpdateComic(Comic, cancelToken: cancelToken)
                : await ComicDatabase.Instance.GetComicPages(Comic);

            // Display new page list
            var reordered = newPages.Reverse();

            // Need to use ObservableCollection methods so UI is updated
            Pages.Clear();
            foreach (var page in reordered)
                Pages.Add(page);
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
