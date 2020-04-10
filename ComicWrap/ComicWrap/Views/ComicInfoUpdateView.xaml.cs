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
    public partial class ComicInfoUpdateView : ComicInfoViewBase
    {
        public ComicInfoUpdateView() : base()
        {
            InitializeComponent();
        }

        protected override void OnComicChanged(ComicData newComic)
        {
            labelComicName.Text = GetFormattedComicName(newComic);
            labelComicPageName.Text = newComic.Pages.LastOrDefault()?.Name ?? "$NEWEST_PAGE$";
            labelLastUpdated.Text = string.Format(Res.ComicInfo_LastUpdatedShort, newComic.DaysSinceLastUpdated);
        }
    }
}