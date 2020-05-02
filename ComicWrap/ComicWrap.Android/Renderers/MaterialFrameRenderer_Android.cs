using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.View;

using ComicWrap.Views;
using ComicWrap.Droid.Renderers;

using FrameRenderer = Xamarin.Forms.Platform.Android.AppCompat.FrameRenderer;

[assembly: ExportRenderer(typeof(MaterialFrame), typeof(MaterialFrameRenderer))]
namespace ComicWrap.Droid.Renderers
{
    public class MaterialFrameRenderer : FrameRenderer
    {
        public MaterialFrameRenderer(Context context) : base(context)
        {
            // Need to explicitly use this constructor since empty FrameRenderer constuctor is obsolete
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
                UpdateElevation();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(MaterialFrame.Elevation):
                    UpdateElevation();
                    break;
            }
        }

        // From: https://alexdunn.org/2018/06/06/xamarin-tip-dynamic-elevation-frames/
        private void UpdateElevation()
        {
            var frame = (MaterialFrame)Element;

            // We need to reset the StateListAnimator to override the setting of Elevation on touch down and release.
            Control.StateListAnimator = new Android.Animation.StateListAnimator();

            // Set the elevation manually
            var elevation = frame.Elevation;
            ViewCompat.SetElevation(this, elevation);
            ViewCompat.SetElevation(Control, elevation);
        }
    }
}