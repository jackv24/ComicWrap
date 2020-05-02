using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Foundation;
using UIKit;
using CoreGraphics;

using ComicWrap.Views;
using ComicWrap.iOS.Renderers;

[assembly: ExportRenderer(typeof(MaterialFrame), typeof(MaterialFrameRenderer))]
namespace ComicWrap.iOS.Renderers
{
    public class MaterialFrameRenderer : FrameRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(MaterialFrame.Elevation):
                // Need to update shadow when size changes so shadow bounds can be updated
                case nameof(MaterialFrame.X):
                case nameof(MaterialFrame.Y):
                case nameof(MaterialFrame.Width):
                case nameof(MaterialFrame.Height):
                    UpdateShadow();
                    break;
            }
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            UpdateShadow();
        }

        // From: https://xamarinhowto.com/customising-ios-frame-shadows-in-xamarin-forms-4-5/
        private void UpdateShadow()
        {
            if (Element == null || !Element.HasShadow)
                return;

            var frame = (MaterialFrame)Element;

            if (Superview != null && Superview.Subviews != null)
            {
                foreach (var uiView in Superview.Subviews)
                {
                    var name = uiView.ToString();

                    // Find the Xamarin.Forms ShadowView and customise the look and feel
                    if (uiView != this && uiView.Layer.ShadowRadius > 0 && name.Contains("_ShadowView"))
                    {
                        float elevation = frame.Elevation;

                        uiView.Layer.ShadowRadius = elevation * 0.2f;
                        uiView.Layer.ShadowColor = UIColor.Black.CGColor;
                        uiView.Layer.ShadowOffset = new CGSize(-1, elevation * 0.15f);
                        uiView.Layer.ShadowOpacity = 0.2f;
                        uiView.Layer.MasksToBounds = false;
                        uiView.Layer.BorderWidth = 0;

                        uiView.Bounds = Bounds;
                        uiView.Frame = base.Frame;
                    }
                }
            }

            Layer.MasksToBounds = true;
            Layer.BorderColor = UIColor.Clear.CGColor;
        }
    }
}
