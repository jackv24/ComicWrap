using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicWrap.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ComicDetailPage : ContentPage
    {
        private ComicDetailPageModel model;

        public ComicDetailPage()
        {
            InitializeComponent();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (model != null)
            {
                model.PagesUpdated -= OnPagesUpdated;
                model = null;
            }

            var newModel = BindingContext as ComicDetailPageModel;
            if (newModel != null)
            {
                newModel.PagesUpdated += OnPagesUpdated;
                model = newModel;
            }
        }

        private void OnPagesUpdated()
        {
            var targetPage = model.ScrollToPage;
            if (targetPage != null)
            {
                int indexOf = model.Pages.IndexOf(targetPage);
                comicPagesCollectionView.ScrollTo(indexOf, position: ScrollToPosition.Center, animate: false);
            }
        }
    }
}