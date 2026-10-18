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
    public partial class IxenView : UIView
    {
        private const float BODY_SIZE = 17f;

        private readonly Dictionary<IntPtr, int> _pointers = new Dictionary<IntPtr, int>();

        private SKCanvasView _canvasView;
        private IxenHost _host;
        private IosAccessibility _accessibility;
        private TextInputView _input;

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
                new IosScheduler(), new IosClipboard(), SetCursor,
                BundleImageSource.ForMainBundle(), Wake, CanPresent);

            IxenSynchronizationContext.Install(_host.Surface);

            _host.Surface.AcceleratorModifier = KeyModifiers.Meta;

            AttachPreferences();
            AttachPointer();
            AttachDrop();
            AttachBack();
            _accessibility = new IosAccessibility(this, _host.Surface);
            IsAccessibilityElement = false;

            _canvasView.PaintSurface += OnPaintSurface;

            AddSubview(_canvasView);

            _input = new TextInputView(_host, _canvasView.SetNeedsDisplay) { Frame = Bounds };
            AddSubview(_input);
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
            _accessibility.MarkStale();
            _accessibility.Sync();
            _input.RequestKeyboardSync();

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

        private readonly Dictionary<IntPtr, (PointerKind Kind, PointerButton Button)> _touches
            = new Dictionary<IntPtr, (PointerKind Kind, PointerButton Button)>();

        private static (PointerKind Kind, PointerButton Button) Describe(UITouch touch, UIEvent evt)
        {
            if (touch.Type != UITouchType.IndirectPointer)
            {
                return (PointerKind.Touch, PointerButton.Left);
            }

            UIEventButtonMask buttons = evt?.ButtonMask ?? UIEventButtonMask.Primary;

            return (PointerKind.Mouse, (buttons & UIEventButtonMask.Secondary) != 0
                ? PointerButton.Right
                : PointerButton.Left);
        }

        private (PointerKind Kind, PointerButton Button) Described(UITouch touch)
            => _touches.TryGetValue(touch.Handle, out var described) ? described : (PointerKind.Touch, PointerButton.Left);

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                CGPoint point = DevicePoint(touch);
                var described = Describe(touch, evt);

                _touches[touch.Handle] = described;
                _host.PointerDown((float)point.X, (float)point.Y, described.Button,
                    described.Kind, PointerOf(touch, true));
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

                _host.PointerMove((float)point.X, (float)point.Y, Described(touch).Kind, id);
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            bool finger = false;

            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                int id = PointerOf(touch, false);

                if (id < 0)
                {
                    continue;
                }

                CGPoint point = DevicePoint(touch);
                var described = Described(touch);

                finger |= described.Kind == PointerKind.Touch;

                _pointers.Remove(touch.Handle);
                _touches.Remove(touch.Handle);
                _host.PointerUp((float)point.X, (float)point.Y, described.Button, described.Kind, id);
            }

            if (_pointers.Count == 0 && finger)
            {
                _host.PointerLeave();
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                _pointers.Remove(touch.Handle);
                _touches.Remove(touch.Handle);
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

        [Export("accessibilityElements")]
        public NSArray ExposedElements => NSArray.FromNSObjects(_accessibility.Elements);

        [Export("accessibilityHitTest:withEvent:")]
        public NSObject AccessibilityHitTest(CGPoint point, UIEvent evt)
        {
            return _accessibility.HitTest(point) ?? this;
        }

        internal CGRect ScreenFrame(CGRect rect)
        {
            UIWindow window = Window;

            if (window == null)
            {
                return rect;
            }

            CGRect inWindow = ConvertRectToView(rect, null);
            UIScreen screen = window.WindowScene?.Screen;

            return screen == null ? inWindow : window.ConvertRectToCoordinateSpace(inWindow, screen.CoordinateSpace);
        }

        internal void AccessibilityActed()
        {
            _canvasView.SetNeedsDisplay();
        }

        private int _canvasHeight;

        private TextField Field => _host.FocusedElement as TextField;

        private void FollowKeyboard()
        {
            UIKeyboardLayoutGuide keyboard = KeyboardLayoutGuide;

            if (OperatingSystem.IsIOSVersionAtLeast(17))
            {
                keyboard.UsesBottomSafeArea = false;
            }

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _canvasView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                _canvasView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                _canvasView.TopAnchor.ConstraintEqualTo(TopAnchor),
                _canvasView.BottomAnchor.ConstraintEqualTo(keyboard.TopAnchor),
            });
        }

        private void RevealFieldIfShrunk(int height)
        {
            bool shrunk = _canvasHeight > 0 && height < _canvasHeight;

            _canvasHeight = height;

            TextField field = Field;

            if (!shrunk || field == null)
            {
                return;
            }

            if (ScrollNavigator.IntoView(field))
            {
                BeginInvokeOnMainThread(_canvasView.SetNeedsDisplay);
            }
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
