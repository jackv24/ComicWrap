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
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel(ComicPageTargetType scrollTo)
        {
            scrollToPageTarget = scrollTo;

            OpenOptionsCommand = new AsyncCommand(OpenOptions);

            RefreshCommand = new AsyncCommand(async () =>
            {
                try
                {
                    IsRefreshing = true;

                    var cancelToken = pageCancelTokenSource.Token;

                    cancelToken.ThrowIfCancellationRequested();
                    await Refresh(cancelToken);

                    IsRefreshing = false;
                }
                catch (OperationCanceledException) { }
            });

            OpenPageCommand = new AsyncCommand<ComicPageData>(OpenPage);
        }

        public event Action PagesUpdated;

        private CancellationTokenSource pageCancelTokenSource;
        private ComicPageTargetType scrollToPageTarget;

        public IAsyncCommand OpenOptionsCommand { get; }
        public IAsyncCommand RefreshCommand { get; }
        public IAsyncCommand<ComicPageData> OpenPageCommand { get; }

        public ObservableCollection<ComicPageData> Pages { get; set; }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        private ComicData _comic;
        public ComicData Comic
        {
            get { return _comic; }
            private set
            {
                _comic = value;

                RaisePropertyChanged();
            }
        }

        public ComicPageData ScrollToPage
        {
            get
            {
                switch (scrollToPageTarget)
                {
                    case ComicPageTargetType.None:
                        return null;

                    case ComicPageTargetType.LastRead:
                        return Comic.LastReadPage;

                    case ComicPageTargetType.FirstNew:
                        return Comic.LatestNewPage;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override void Init(object initData)
        {
            Comic = initData as ComicData;

            Pages = new ObservableCollection<ComicPageData>();
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();
            UpdatePages(Comic.Pages);
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.CancelAndDispose();
            IsRefreshing = false;
        }

        private async Task OpenOptions()
        {
            string buttonPressed = await UserDialogs.Instance.ActionSheetAsync(
                title: Res.ComicDetail_Options_Title,
                cancel: Res.Alert_Generic_Cancel,
                destructive: Res.ComicDetail_Options_Delete,
                cancelToken: pageCancelTokenSource.Token,
                Res.ComicDetail_Options_Edit
                );

            if (buttonPressed == Res.Alert_Generic_Cancel)
            {
                return;
            }
            else if(buttonPressed == Res.ComicDetail_Options_Delete)
            {
                var comicName = Comic.Name;
                ComicDatabase.Instance.DeleteComic(Comic);
                UserDialogs.Instance.Toast(string.Format(Res.ComicDetail_DeletedComic, comicName));
            }
            else if (buttonPressed == Res.ComicDetail_Options_Edit)
            {
                await CoreMethods.PushPageModel<EditComicPageModel>(Comic);
                return;
            }
            else
            {
                throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
        }

        private async Task Refresh(CancellationToken cancelToken)
        {
            try
            {
                var newPages = await ComicUpdater.Instance.UpdateComic(Comic, cancelToken);
                UpdatePages(newPages);
            }
            catch (OperationCanceledException) { }
        }

        private void UpdatePages(IEnumerable<ComicPageData> newPages)
        {
            if (newPages == null || !newPages.Any())
            {
                // Use history if no actual pages exist
                newPages = Comic.RecentHistory
                    .Select(
                        historyItem => new ComicPageData
                        {
                            Comic = Comic,
                            Name = historyItem,
                            Url = historyItem,
                            IsRead = true
                        });
            }

            var reversed = newPages
                .Reverse()
                .ToList();

            // Need to use ObservableCollection methods so UI is updated with animation
            Pages.MatchList(reversed);

            PagesUpdated?.Invoke();

            // Clear ScrollTo target since page shuld have been scrolled during PagesUpdated event (if desired)
            scrollToPageTarget = ComicPageTargetType.None;
        }

        private Task OpenPage(ComicPageData pageData)
        {
            return CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
