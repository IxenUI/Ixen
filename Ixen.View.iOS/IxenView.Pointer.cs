using CoreGraphics;
using Foundation;
using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual.Styles.Descriptors;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView
    {
        private const float WHEEL_NOTCH = 48f;
        private const float BEAM_LENGTH = 20f;
        private const int HOVER_POINTER = 0;

        private UIPointerInteraction _pointerInteraction;
        private CursorKind _cursor = CursorKind.Default;
        private CGPoint _lastScroll;

        private void AttachPointer()
        {
            AddGestureRecognizer(new UIHoverGestureRecognizer(OnHover));

            AddGestureRecognizer(new UIPanGestureRecognizer(OnScroll)
            {
                AllowedScrollTypesMask = UIScrollTypeMask.All,
                MaximumNumberOfTouches = 0,
            });

            _pointerInteraction = new UIPointerInteraction(new PointerStyles(this));
            AddInteraction(_pointerInteraction);
        }

        private CGPoint DevicePoint(UIGestureRecognizer recognizer)
        {
            CGPoint point = recognizer.LocationInView(this);
            float scale = DeviceScale;

            return new CGPoint(point.X * scale, point.Y * scale);
        }

        private void OnHover(UIHoverGestureRecognizer recognizer)
        {
            switch (recognizer.State)
            {
                case UIGestureRecognizerState.Began:
                case UIGestureRecognizerState.Changed:
                    CGPoint point = DevicePoint(recognizer);

                    _host.PointerMove((float)point.X, (float)point.Y, PointerKind.Mouse, HOVER_POINTER);
                    break;

                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                    if (_pointers.Count == 0)
                    {
                        _host.PointerLeave();
                    }
                    break;
            }
        }

        private void OnScroll(UIPanGestureRecognizer recognizer)
        {
            if (recognizer.State == UIGestureRecognizerState.Began)
            {
                _lastScroll = CGPoint.Empty;
                _host.PointerWheelFling(0, 0);
            }

            if (recognizer.State == UIGestureRecognizerState.Ended)
            {
                CGPoint velocity = recognizer.VelocityInView(this);

                _host.PointerWheelFling(-(float)velocity.X / WHEEL_NOTCH, (float)velocity.Y / WHEEL_NOTCH);
                return;
            }

            if (recognizer.State != UIGestureRecognizerState.Began
                && recognizer.State != UIGestureRecognizerState.Changed)
            {
                return;
            }

            CGPoint translation = recognizer.TranslationInView(this);
            float deltaX = (float)(translation.X - _lastScroll.X);
            float deltaY = (float)(translation.Y - _lastScroll.Y);

            _lastScroll = translation;

            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            CGPoint point = DevicePoint(recognizer);

            _host.PointerWheel((float)point.X, (float)point.Y, -deltaX / WHEEL_NOTCH, deltaY / WHEEL_NOTCH,
                IosKeys.ToModifiers(recognizer.ModifierFlags));
        }

        private void SetCursor(CursorKind kind, CursorImage image)
        {
            if (kind == _cursor)
            {
                return;
            }

            _cursor = kind;
            _pointerInteraction?.Invalidate();
        }

        private UIPointerStyle CursorStyle()
        {
            switch (_cursor)
            {
                case CursorKind.Text:
                    return UIPointerStyle.Create(UIPointerShape.CreateBeam(BEAM_LENGTH, UIAxis.Vertical), (UIAxis)0);

                case CursorKind.Hidden:
                    return UIPointerStyle.CreateHiddenPointerStyle();

                default:
                    return null;
            }
        }

        private sealed class PointerStyles : NSObject, IUIPointerInteractionDelegate
        {
            private readonly IxenView _view;

            internal PointerStyles(IxenView view)
            {
                _view = view;
            }

            [Export("pointerInteraction:regionForRequest:defaultRegion:")]
            public UIPointerRegion GetRegionForRequest(UIPointerInteraction interaction, UIPointerRegionRequest request,
                UIPointerRegion defaultRegion)
            {
                return defaultRegion;
            }

            [Export("pointerInteraction:styleForRegion:")]
            public UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region)
            {
                return _view.CursorStyle();
            }
        }
    }
}
