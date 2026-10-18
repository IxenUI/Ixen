using CoreGraphics;
using Foundation;
using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Platform;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using System;
using System.Collections.Generic;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView : UIView, IUITextInput
    {
        private const float BODY_SIZE = 17f;

        private readonly Dictionary<IntPtr, int> _pointers = new Dictionary<IntPtr, int>();

        private SKCanvasView _canvasView;
        private IxenHost _host;

        public IxenView()
            : base()
        {
            Setup();
        }

        public IxenView(CGRect frame)
            : base(frame)
        {
            Setup();
        }

        protected IxenView(ObjCRuntime.NativeHandle handle)
            : base(handle)
        {
            Setup();
        }

        private void Setup()
        {
            MultipleTouchEnabled = true;

            _canvasView = new SKCanvasView(Bounds)
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                UserInteractionEnabled = false,
                Opaque = false,
            };

            _host = new IxenHost(new IxenSurface(), _canvasView.SetNeedsDisplay,
                new IosScheduler(), new IosClipboard(), null,
                BundleImageSource.ForMainBundle(), Wake, CanPresent);

            IxenSynchronizationContext.Install(_host.Surface);

            _host.Surface.ReducedMotion = UIAccessibility.IsReduceMotionEnabled;
            _host.Surface.HighContrast = UIAccessibility.DarkerSystemColorsEnabled;
            _host.Surface.AcceleratorModifier = KeyModifiers.Meta;

            _canvasView.PaintSurface += OnPaintSurface;

            AddSubview(_canvasView);
            FollowKeyboard();
        }

        private void Wake()
        {
            if (NSThread.IsMain)
            {
                _canvasView.SetNeedsDisplay();
                return;
            }

            BeginInvokeOnMainThread(_canvasView.SetNeedsDisplay);
        }

        private bool CanPresent() => Window != null && !Hidden;

        private float DeviceScale
        {
            get
            {
                float scale = (float)TraitCollection.DisplayScale;

                return scale > 0 ? scale : 1f;
            }
        }

        private static float ReadFontScale()
        {
            try
            {
                float scaled = (float)UIFontMetrics.DefaultMetrics.GetScaledValue(BODY_SIZE);

                return scaled > 0 ? scaled / BODY_SIZE : 1f;
            }
            catch
            {
                return 1f;
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _host.Surface.Presentable = CanPresent();

            _host.Surface.Scale = DeviceScale;
            _host.Surface.FontScale = ReadFontScale();
            _host.Paint(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            RevealFieldIfShrunk(e.Info.Height);
            RequestKeyboardSync();

            _paints++;

            if (_captured != null && _paints >= _capturePaints)
            {
                Capture(e.Surface, e.Info.Width, e.Info.Height);
            }
        }

        private int PointerOf(UITouch touch, bool adding)
        {
            IntPtr key = touch.Handle;

            if (_pointers.TryGetValue(key, out int id))
            {
                return id;
            }

            if (!adding)
            {
                return -1;
            }

            id = 0;

            while (_pointers.ContainsValue(id))
            {
                id++;
            }

            _pointers[key] = id;

            return id;
        }

        private CGPoint DevicePoint(UITouch touch)
        {
            CGPoint point = touch.LocationInView(this);
            float scale = DeviceScale;

            return new CGPoint(point.X * scale, point.Y * scale);
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                CGPoint point = DevicePoint(touch);

                _host.PointerDown((float)point.X, (float)point.Y, PointerButton.Left,
                    PointerKind.Touch, PointerOf(touch, true));
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                int id = PointerOf(touch, false);

                if (id < 0)
                {
                    continue;
                }

                CGPoint point = DevicePoint(touch);

                _host.PointerMove((float)point.X, (float)point.Y, PointerKind.Touch, id);
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                int id = PointerOf(touch, false);

                if (id < 0)
                {
                    continue;
                }

                CGPoint point = DevicePoint(touch);

                _pointers.Remove(touch.Handle);
                _host.PointerUp((float)point.X, (float)point.Y, PointerButton.Left, PointerKind.Touch, id);
            }

            if (_pointers.Count == 0)
            {
                _host.PointerLeave();
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                _pointers.Remove(touch.Handle);
            }

            if (_pointers.Count == 0)
            {
                _host.PointerCaptureLost();
            }
        }

        private int _capturePaints;
        private int _paints;
        private Action<IxenFrameCapture> _captured;

        public void CaptureAfter(int paints, Action<IxenFrameCapture> captured)
        {
            _capturePaints = paints < 1 ? 1 : paints;
            _captured = captured;

            _host.Surface.ReducedMotion = true;
        }

        private void Capture(SKSurface surface, int width, int height)
        {
            Action<IxenFrameCapture> captured = _captured;

            _captured = null;

            using SKBitmap host = ReadFrame(surface, width, height);
            using SKBitmap library = _host.Surface.RenderToBitmap();
            using var capture = new IxenFrameCapture(host, library, width, height,
                _host.Surface.Scale, _host.Surface.FontScale);

            captured(capture);
        }

        private static SKBitmap ReadFrame(SKSurface surface, int width, int height)
        {
            if (surface == null)
            {
                return null;
            }

            var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

            if (surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
            {
                return bitmap;
            }

            bitmap.Dispose();

            return null;
        }

        public IxenHost Host => _host;

        public VisualElement Root
        {
            get => _host.Root;
            set => _host.Root = value;
        }

        public Component RootComponent
        {
            set => _host.Root = value?.Initialize();
        }

        public string SaveState() => _host.Surface.SaveState();

        public void RestoreState(string state) => _host.Surface.RestoreState(state);
    }
}
