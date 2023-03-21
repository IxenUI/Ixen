using Ixen.Core;
using Ixen.Core.Visual;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Ixen.View.WPF
{
    public class IxenView : ContentControl, IDisposable
    {
        private static Type _type = typeof(IxenView);

        private IxenSurface _ixenSurface;
        private SKElement _skElement;

        public IxenView()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            _ixenSurface = new IxenSurface();

            _skElement = new SKElement();
            _skElement.IgnorePixelScaling = true;
            _skElement.PaintSurface += OnPaintSurface;
            AddChild(_skElement);
        }

        public void Dispose()
        {
            _skElement.PaintSurface -= OnPaintSurface;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _ixenSurface.ComputeLayout(e.Info.Width, e.Info.Height);
            _ixenSurface.Render(e.Surface.Canvas);
        }

        private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((IxenView)d)._ixenSurface.Root = (VisualElement)e.NewValue;
        }

        public static readonly DependencyProperty RootProperty = DependencyProperty.Register
        (
            nameof(Root),
            typeof(VisualElement), _type,
            new FrameworkPropertyMetadata(null, OnRootChanged)
        );

        public VisualElement Root
        {
            get => (VisualElement)GetValue(RootProperty);
            set => SetValue(RootProperty, value);
        }
    }
}
