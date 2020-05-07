using System;
using System.Collections.Generic;
using System.Text;

using FreshMvvm;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class EditComicPageModel : FreshBasePageModel
    {
        private ComicData _comic;
        public ComicData Comic
        {
            get { return _comic; }
            private set
            {
                _comic = value;

                RaisePropertyChanged();
            }
        }

        public override void Init(object initData)
        {
            Comic = initData as ComicData;
        }
    }
}
