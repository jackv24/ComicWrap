using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;

namespace ComicWrap.Views
{
    // Original source: https://alexdunn.org/2018/06/06/xamarin-tip-dynamic-elevation-frames/
    public class MaterialFrame : Frame
    {
        public static BindableProperty ElevationProperty = BindableProperty.Create(
            nameof(Elevation),
            typeof(float),
            typeof(MaterialFrame),
            defaultValue: 4.0f);

        public float Elevation
        {
            get { return (float)GetValue(ElevationProperty); }
            set { SetValue(ElevationProperty, value); }
        }
    }
}
