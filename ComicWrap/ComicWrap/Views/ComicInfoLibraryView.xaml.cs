using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;

using ComicWrap.Systems;
using ComicWrap.Pages;
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ComicInfoLibraryView : ComicInfoViewBase
    {
        public ComicInfoLibraryView() : base()
        {
            InitializeComponent();
        }

        public override ComicPageTargetType PageTarget => ComicPageTargetType.LastRead;
        public override Image CoverImage => coverImage;

        protected override void OnComicChanged(ComicData newComic)
        {
            labelComicName.Text = GetFormattedComicName(newComic);
            labelLastComicPageName.Text = newComic.LastReadPage?.Name ?? "$LAST_READ_PAGE$";
            progressBarReadProgress.Progress = newComic.ReadProgress;

            if (newComic.LastReadDate != null)
            {
                TimeSpan timeSince = DateTimeOffset.UtcNow - newComic.LastReadDate.Value;
                labelLastRead.Text = string.Format(Res.ComicInfo_LastRead, timeSince.Days);
            }
            else
            {
                labelLastRead.Text = string.Empty;
            }
        }
    }
}