using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using IxenComponent = Ixen.Core.Components.Component;
using Ixen.Platform;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ixen.View.WPF
{
    public class IxenView : ContentControl, IDisposable
    {
        private static Type _type = typeof(IxenView);

        private IxenHost _host;
        private SKElement _skElement;

        public IxenView()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            _skElement = new SKElement();
            _skElement.IgnorePixelScaling = true;
            _skElement.Focusable = true;
            _skElement.FocusVisualStyle = null;
            _host = new IxenHost(new IxenSurface(), _skElement.InvalidateVisual, new DispatcherScheduler());

            _skElement.PaintSurface += OnPaintSurface;
            _skElement.MouseMove += OnMouseMove;
            _skElement.MouseDown += OnMouseDown;
            _skElement.MouseUp += OnMouseUp;
            _skElement.MouseLeave += OnMouseLeave;
            _skElement.LostMouseCapture += OnLostMouseCapture;
            _skElement.MouseWheel += OnMouseWheel;
            _skElement.PreviewKeyDown += OnPreviewKeyDown;
            _skElement.PreviewKeyUp += OnPreviewKeyUp;
            _skElement.TextInput += OnTextInput;

            AddChild(_skElement);
        }

        public void Dispose()
        {
            _skElement.PaintSurface -= OnPaintSurface;
            _skElement.MouseMove -= OnMouseMove;
            _skElement.MouseDown -= OnMouseDown;
            _skElement.MouseUp -= OnMouseUp;
            _skElement.MouseLeave -= OnMouseLeave;
            _skElement.LostMouseCapture -= OnLostMouseCapture;
            _skElement.MouseWheel -= OnMouseWheel;
            _skElement.PreviewKeyDown -= OnPreviewKeyDown;
            _skElement.PreviewKeyUp -= OnPreviewKeyUp;
            _skElement.TextInput -= OnTextInput;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
            => _host.Paint(e.Surface.Canvas, e.Info.Width, e.Info.Height);

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(_skElement);
            _host.PointerMove((float)position.X, (float)position.Y);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(_skElement);

            _skElement.CaptureMouse();
            _skElement.Focus();
            _host.PointerDown((float)position.X, (float)position.Y, ToButton(e.ChangedButton));
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(_skElement);

            _host.PointerUp((float)position.X, (float)position.Y, ToButton(e.ChangedButton));
            _skElement.ReleaseMouseCapture();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
            => _host.PointerLeave();

        private const float WHEEL_DELTA = 120f;

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point position = e.GetPosition(_skElement);
            _host.PointerWheel((float)position.X, (float)position.Y, 0, e.Delta / WHEEL_DELTA);
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
            => _host.PointerCaptureLost();

        private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _host.KeyDown(WpfKeys.ToKey(RealKey(e)), WpfKeys.ToModifiers(Keyboard.Modifiers));

            if (e.Key == System.Windows.Input.Key.Tab)
            {
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
            => _host.KeyUp(WpfKeys.ToKey(RealKey(e)), WpfKeys.ToModifiers(Keyboard.Modifiers));

        private static System.Windows.Input.Key RealKey(System.Windows.Input.KeyEventArgs e)
            => e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

        private void OnTextInput(object sender, TextCompositionEventArgs e)
            => _host.TextInput(e.Text);

        private static PointerButton ToButton(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return PointerButton.Left;

                case MouseButton.Middle:
                    return PointerButton.Middle;

                case MouseButton.Right:
                    return PointerButton.Right;

                default:
                    return PointerButton.None;
            }
        }

        private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((IxenView)d)._host.Root = (VisualElement)e.NewValue;
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

        private static void OnRootComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((IxenView)d)._host.Root = ((IxenComponent)e.NewValue)?.Initialize();
        }

        public static readonly DependencyProperty RootComponentProperty = DependencyProperty.Register
        (
            nameof(RootComponent),
            typeof(IxenComponent), _type,
            new FrameworkPropertyMetadata(null, OnRootComponentChanged)
        );

        public IxenComponent RootComponent
        {
            get => (IxenComponent)GetValue(RootComponentProperty);
            set => SetValue(RootComponentProperty, value);
        }
    }
}
