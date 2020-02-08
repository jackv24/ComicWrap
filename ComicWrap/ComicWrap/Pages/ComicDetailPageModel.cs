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
            RefreshCommand = new Command(() => Refresh(refreshComic: true));
            OpenPageCommand = new Command<ComicPageData>(async (page) => await OpenPage(page));
        }

        private bool _isRefreshing;
        private bool isRefreshTaskRunning;

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

            Pages = new ObservableCollection<ComicPageData>();
            Refresh(refreshComic: false);
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

        private async void Refresh(bool refreshComic = true)
        {
            // Only one refresh task should be running (since Refresh is a "fire and forget" async method)
            if (isRefreshTaskRunning)
                return;

            isRefreshTaskRunning = true;
            IsRefreshing = true;

            // Get comic pages from database (fast) or internet (slow)
            var newPages = refreshComic
                ? await ComicUpdater.UpdateComic(Comic)
                : await ComicDatabase.Instance.GetComicPages(Comic);

            // Display new page list
            var reordered = newPages.Reverse();
            
            // Need to use ObservableCollection methods so UI is updated
            Pages.Clear();
            foreach (var page in reordered)
                Pages.Add(page);

            isRefreshTaskRunning = false;
            IsRefreshing = false;
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
