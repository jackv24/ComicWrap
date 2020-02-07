using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;
using AngleSharp;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class ComicReaderPageModel : FreshBasePageModel
    {
        public ComicReaderPageModel()
        {
            RefreshCommand = new Command(async () => await Refresh());
        }

        public Command RefreshCommand { get; }

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

            PageName = pageData.Name;

            // Load page as HTML instead of directly so it can be manipulated
            var context = ComicUpdater.GetBrowsingContext();
            context.OpenAsync(pageData.Url).ContinueWith((task) =>
            {
                var document = task.Result;

                // NOTE: below code currently zooms, but pinch-to-zoom doesn't work so zoom is permanent
                //var meta = document.CreateElement("meta");
                //meta.SetAttribute("name", "viewport");
                //meta.SetAttribute("content", "width=device-width, initial-scale=0.25, maximum-scale=3.0 user-scalable=1");
                //document.Head.AppendChild(meta);

                PageSource = new HtmlWebViewSource()
                {
                    Html = document.ToHtml()
                };
            });
        }

        private async Task Refresh()
        {
            await UserDialogs.Instance.AlertAsync("Refreshing not yet implemented!");
        }
    }
}
