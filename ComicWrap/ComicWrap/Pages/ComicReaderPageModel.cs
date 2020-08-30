using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using FreshMvvm;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using Acr.UserDialogs;

using ComicWrap.Systems;
using ComicWrap.Views;
using ComicWrap.Resources;

namespace ComicWrap.Pages
{
    public class ComicReaderPageModel : FreshBasePageModel
    {
        public ComicReaderPageModel()
        {
            NavigatingCommand = new Command<CustomWebViewNavigatingArgs>(OnNavigating);
            NavigatedCommand = new AsyncCommand<CustomWebViewNavigatedArgs>(OnNavigated);
            MoreCommand = new AsyncCommand(DisplayMoreOptions);
        }

        private WebView lastNavigatedWebView;
        private string lastNavigatedUrl;

        private CancellationTokenSource pageEndCancel;

        private Dictionary<string, ComicPageData> cachedPages;

        public Command<CustomWebViewNavigatingArgs> NavigatingCommand { get; }
        public IAsyncCommand<CustomWebViewNavigatedArgs> NavigatedCommand { get; }
        public IAsyncCommand MoreCommand { get; }

        public ComicData Comic { get; private set; }

        private string _pageName;
        public string PageName
        {
            get { return _pageName; }
            private set
            {
                _pageName = value;
                RaisePropertyChanged();
            }
        }

        private UrlWebViewSource _pageSource;
        public UrlWebViewSource PageSource
        {
            get { return _pageSource; }
            set
            {
                _pageSource = value;
                RaisePropertyChanged();
            }
        }

        public override void Init(object initData)
        {
            var pageData = (ComicPageData)initData;
            Comic = pageData.Comic;

            PageName = pageData.Name;
            PageSource = new UrlWebViewSource { Url = pageData.Url };

            ComicDatabase.Instance.MarkRead(pageData);
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageEndCancel = new CancellationTokenSource();
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageEndCancel.CancelAndDispose();
        }

        private void OnNavigating(CustomWebViewNavigatingArgs args)
        {
            var webView = (CustomWebView)args.Sender;
            var e = args.EventArgs;

            if (!string.IsNullOrEmpty(lastNavigatedUrl))
            {
                // Don't do anything when called multiple times on same page
                if (string.IsNullOrEmpty(e.Url)
                    || new Uri(e.Url).ToRelative() == new Uri(lastNavigatedUrl).ToRelative())
                {
                    return;
                }
            }

            lastNavigatedUrl = e.Url;
            lastNavigatedWebView = webView;

            // Record page read history if we're navigating to a new page
            if (Comic.RecentHistory.LastOrDefault() != e.Url)
                Comic.RecordHistory(e.Url);
            
            var pageData = LoadPageData(e.Url);
            if (pageData == null || pageData.IsRead)
                return;
            
            PageName = pageData.Name;

            // Mark as read and save to database (don't need to update cachedPages since pageData is a class reference)
            ComicDatabase.Instance.MarkRead(pageData);
        }

        private async Task OnNavigated(CustomWebViewNavigatedArgs args)
        {
            var webView = (CustomWebView)args.Sender;
            var e = args.EventArgs;

            // Set document title to page name if it's a comic page, else just webpage title
            var pageData = LoadPageData(e.Url);
            PageName = pageData != null
                ? pageData.Name
                : await webView.EvaluateJavaScriptAsync("document.title");
        }

        private ComicPageData LoadPageData(string pageUrl)
        {
            // Get cached pages once, since no pages will be added or removed and ComicPageData is a class
            if (cachedPages == null)
            {
                var pages = Comic.Pages;
                cachedPages = pages
                    .ToDictionary(page => new Uri(page.Url).ToRelative());
            }

            string pageUrlRelative = new Uri(pageUrl).ToRelative();
            
            if (cachedPages.ContainsKey(pageUrlRelative))
                return cachedPages[pageUrlRelative];

            return null;
        }

        private async Task DisplayMoreOptions()
        {
            string result = await UserDialogs.Instance.ActionSheetAsync(
                title: AppResources.ComicReader_More_Title,
                cancel: AppResources.Alert_Generic_Cancel,
                destructive: null,
                cancelToken: pageEndCancel.Token,
                AppResources.ComicReader_More_SetAsCover);

            if (result == AppResources.Alert_Generic_Cancel)
                return;
            else if (result == AppResources.ComicReader_More_SetAsCover)
                SetCoverImage().SafeFireAndForget();
            else
                throw new NotImplementedException();
        }

        private async Task SetCoverImage()
        {
            if (lastNavigatedWebView == null)
                return;

            // TODO: Expand to work with more sites
            string imageUrl = await lastNavigatedWebView.EvaluateJavaScriptAsync(
                "document.getElementById('cc-comic').getAttribute('src')");

            if (string.IsNullOrEmpty(imageUrl))
            {
                await CoreMethods.DisplayAlert(
                    title: AppResources.Alert_Error_Title,
                    message: AppResources.ImageDownload_LocateError_Msg,
                    cancel: AppResources.Alert_Generic_Confirm);

                return;
            }

            try
            {
                var cancelToken = pageEndCancel.Token;

                // Start download image task so download can progress while user is reading "Download Started" popup
                Task<string> downloadImageTask = LocalImageService.DownloadImage(new Uri(imageUrl), Comic.Id, cancelToken);

                await CoreMethods.DisplayAlert(
                    title: AppResources.ImageDownload_FoundUrl_Title,
                    message: AppResources.ImageDownload_FoundUrl_Msg,
                    cancel: AppResources.Alert_Generic_Confirm);

                await downloadImageTask;

                cancelToken.ThrowIfCancellationRequested();

                await CoreMethods.DisplayAlert(
                    title: AppResources.ImageDownload_Complete_Title,
                    message: AppResources.ImageDownload_Complete_Msg,
                    cancel: AppResources.Alert_Generic_Confirm);
            }
            catch (OperationCanceledException) { }
        }
    }
}
