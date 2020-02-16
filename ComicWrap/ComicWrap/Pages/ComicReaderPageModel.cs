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
            LastPageData = pageData;

            PageName = pageData.Name;
            PageUrl = pageData.Url;

            PageSource = new UrlWebViewSource { Url = pageData.Url };

            InitAsync(pageData).SafeFireAndForget();
        }

        private async Task InitAsync(ComicPageData pageData)
        {
            pageData.IsRead = true;
            await ComicDatabase.Instance.UpdateComicPage(pageData);
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

            PageName = "Loading...";
            PageUrl = e.Url;

            var pageData = await LoadPageData(e.Url);
            if (pageData != null && !pageData.IsRead)
            {
                pageData.IsRead = true;
                await ComicDatabase.Instance.UpdateComicPage(pageData);
            }
        }

        private async Task OnNavigated(CustomWebViewNavigatedArgs args)
        {
            var webView = (CustomWebView)args.Sender;
            var e = args.EventArgs;

            var pageData = await LoadPageData(e.Url);
            if (pageData != null)
            {
                PageName = pageData.Name;
                PageUrl = pageData.Url;
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
                {
                    LastPageData = page;
                    return page;
                }
            }

            return null;
        }
    }
}
