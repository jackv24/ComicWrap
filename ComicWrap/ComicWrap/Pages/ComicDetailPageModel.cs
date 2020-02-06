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

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel()
        {
            OpenOptionsCommand = new Command(async () => await OpenOptions());
            RefreshCommand = new Command(async () => await Refresh());
            OpenPageCommand = new Command<ComicPageData>(async (page) => await OpenPage(page));
        }

        private bool _isRefreshing;

        public Command OpenOptionsCommand { get; }
        public Command RefreshCommand { get; }
        public Command<ComicPageData> OpenPageCommand { get; }

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

            Pages = GetOrderedPages(
                ComicDatabase.Instance.GetData<ComicPageData>(data => data.ComicId == Comic.Id));
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
                    ComicDatabase.Instance.DeleteData(Comic);
                    break;

                default:
                    throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
            UserDialogs.Instance.Toast($"Deleted Comic: {Comic.Name}");
        }

        private async Task Refresh()
        {
            IsRefreshing = true;

            var newPages = await ComicUpdater.GetPages(Comic.ArchiveUrl);

            // Remove old pages from database, they will be replaced
            foreach (var page in Pages)
                ComicDatabase.Instance.DeleteData(page);

            // New page data is bare, so fill out missing data
            foreach (var newPage in newPages)
            {
                newPage.ComicId = Comic.Id;

                foreach (var oldPage in Pages)
                {
                    // Transfer persistent data to new page data
                    if (newPage.Url == oldPage.Url)
                        newPage.IsRead = oldPage.IsRead;
                }

                ComicDatabase.Instance.SetData(newPage);
            }

            // Clear old pages collection and fill with new pages
            Pages = GetOrderedPages(newPages);

            IsRefreshing = false;
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }

        private ObservableCollection<ComicPageData> GetOrderedPages(IEnumerable<ComicPageData> pages)
        {
            var reordered = pages
                .Reverse();

            return new ObservableCollection<ComicPageData>(reordered);
        }
    }
}
