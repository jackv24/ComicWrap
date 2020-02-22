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

            NavigatingCommand = new Command<CustomWebViewNavigatingArgs>(OnNavigating);
            NavigatedCommand = new AsyncCommand<CustomWebViewNavigatedArgs>(OnNavigated);
        }

        private string lastNavigatedUrl;

        private Dictionary<string, ComicPageData> cachedPages;

        public IAsyncCommand RefreshCommand { get; }

        public Command<CustomWebViewNavigatingArgs> NavigatingCommand { get; }
        public IAsyncCommand<CustomWebViewNavigatedArgs> NavigatedCommand { get; }

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

            ComicDatabase.Instance.Write(() => pageData.IsRead = true);
        }

        private async Task Refresh()
        {
            await UserDialogs.Instance.AlertAsync("Refreshing not yet implemented!");
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

            var pageData = LoadPageData(e.Url);
            if (pageData != null && !pageData.IsRead)
            {
                PageName = pageData.Name;

                // Mark as read and save to database (don't need to update cachedPages since pageData is a class reference)
                ComicDatabase.Instance.Write(() => pageData.IsRead = true);
            }
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
    }
}
