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
                    await Refresh();
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

        public ComicData Comic { get; private set; }
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

            // Will call refresh command (don't set true in manually called refresh command
            // or it will be called multiple times)
            IsRefreshing = true;
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.Cancel();
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

        private async Task Refresh()
        {
            var cancelToken = pageCancelTokenSource.Token;

            var newPages = await ComicUpdater.UpdateComic(Comic, cancelToken: cancelToken);

            // Display new page list
            var reordered = newPages.Reverse();
            
            // Need to use ObservableCollection methods so UI is updated
            Pages.Clear();
            foreach (var page in reordered)
                Pages.Add(page);

            IsRefreshing = false;
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
