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
            labelComicName.Text = newComic?.Name;
            labelLastComicPageName.Text = newComic.LastReadPage?.Name;
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

            // Different appearance if updating
            if (newComic.IsUpdating)
            {
                // Can't tap on if still importing (need to use Any since Contains isn't supported)
                if (!ComicDatabase.Instance.GetComicsQuery()
                    .Any(c => c.Id == newComic.Id))
                {
                    IsEnabled = false;
                }


                Opacity = 0.3f;
            }
            else
            {
                IsEnabled = true;
                Opacity = 1.0;
            }
        }

        protected override void OnElevationChanged(float elevation)
        {
            imageFrame.Elevation = elevation;
        }
    }
}