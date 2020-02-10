using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

using Acr.UserDialogs;
using FreshMvvm;
using AngleSharp;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;

using ComicWrap.Systems;
using ComicWrap.Views;

namespace ComicWrap.Pages
{
    public class ComicReaderPageModel : FreshBasePageModel
    {
        public ComicReaderPageModel()
        {
            RefreshCommand = new AsyncCommand(Refresh);

            NavigatingCommand = new AsyncCommand<CustomWebViewNavigatingArgs>(OnNavigating);
            NavigatedCommand = new AsyncCommand<CustomWebViewNavigatedArgs>(OnNavigated);
        }


        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand<CustomWebViewNavigatingArgs> NavigatingCommand { get; }
        public IAsyncCommand<CustomWebViewNavigatedArgs> NavigatedCommand { get; }

        public ComicPageData LastPageData { get; private set; }
        public string PageUrl { get; private set; }

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

        private HtmlWebViewSource _pageSource;
        public HtmlWebViewSource PageSource
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

            InitAsync(pageData).SafeFireAndForget();
        }

        private async Task InitAsync(ComicPageData pageData)
        {
            PageName = pageData.Name;
            PageUrl = pageData.Url;

            // For some reason Navigated event is not called on first load
            await OnPageLoaded(pageData);

            // Call after manual OnPageLoaded above just in case Navigated event does get called
            await LoadPage(pageData);
        }

        private async Task Refresh()
        {
            await UserDialogs.Instance.AlertAsync("Refreshing not yet implemented!");
        }

        private async Task OnNavigating(CustomWebViewNavigatingArgs args)
        {
            var webView = (CustomWebView)args.Sender;
            var e = args.EventArgs;

            // Don't do anything when called multiple times on same page
            if (string.IsNullOrEmpty(e.Url)
                || new Uri(e.Url).ToRelative() == new Uri(PageUrl).ToRelative())
            {
                return;
            }

            var nextPageBase = new Uri(e.Url).Host.Replace("www.", string.Empty);
            var currentPageBase = new Uri(PageUrl).Host.Replace("www.", string.Empty);

            if (nextPageBase != currentPageBase)
            {
                // Next page is not on same site, open in system default app instead
                e.Cancel = true;
                await Launcher.OpenAsync(e.Url);
            }
            else
            {
                PageName = "Loading...";
                PageUrl = e.Url;

                var pageData = await LoadPageData(e.Url);
                if (pageData != null)
                {
                    e.Cancel = true;
                    await LoadPage(pageData);
                }
            }
        }

        private async Task OnNavigated(CustomWebViewNavigatedArgs args)
        {
            var webView = (CustomWebView)args.Sender;
            var e = args.EventArgs;

            var pageData = await LoadPageData(e.Url);
            if (pageData != null)
            {
                await OnPageLoaded(pageData);
            }
            else
            {
                PageName = await webView.EvaluateJavaScriptAsync("document.title");
                PageUrl = e.Url;
            }
        }

        private async Task<ComicPageData> LoadPageData(string pageUrl)
        {
            string pageUrlRelative = new Uri(pageUrl).ToRelative();

            var pages = await ComicDatabase.Instance.GetComicPages(LastPageData.ComicId);
            foreach (var page in pages)
            {
                string otherPageUrlRelative = new Uri(page.Url).ToRelative();
                if (otherPageUrlRelative == pageUrlRelative)
                    return page;
            }

            return null;
        }

        private async Task LoadPage(ComicPageData pageData)
        {
            LastPageData = pageData;

            // Load page as HTML instead of directly so it can be manipulated
            var context = ComicUpdater.GetBrowsingContext();
            var document = await context.OpenAsync(pageData.Url);

            // NOTE: below code currently zooms, but pinch-to-zoom doesn't work so zoom is permanent
            //var meta = document.CreateElement("meta");
            //meta.SetAttribute("name", "viewport");
            //meta.SetAttribute("content", "width=device-width, initial-scale=0.25, maximum-scale=3.0 user-scalable=1");
            //document.Head.AppendChild(meta);

            PageSource = new HtmlWebViewSource()
            {
                Html = document.ToHtml()
            };
        }

        private async Task OnPageLoaded(ComicPageData pageData)
        {
            PageName = pageData.Name;
            PageUrl = pageData.Url;

            pageData.IsRead = true;
            await ComicDatabase.Instance.UpdateComicPage(pageData);
        }
    }
}
