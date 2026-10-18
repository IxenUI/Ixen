using CoreGraphics;
using Foundation;
using Ixen.Core.Input;
using System;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView
    {
        private const float BACK_EDGE = 20f;
        private const float BACK_SHARE = 0.3f;
        private const float BACK_MIN_DISTANCE = 40f;
        private const float BACK_FLICK_SPEED = 500f;

        private UIPanGestureRecognizer _backSwipe;
        private BackGate _backGate;

        private void AttachBack()
        {
            _backGate = new BackGate(this);
            _backSwipe = new UIPanGestureRecognizer(OnBackSwipe)
            {
                MaximumNumberOfTouches = 1,
                AllowedTouchTypes = new[] { NSNumber.FromInt64((long)UITouchType.Direct) },
                Delegate = _backGate,
            };

            AddGestureRecognizer(_backSwipe);
        }

        private void OnBackSwipe(UIPanGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Ended)
            {
                return;
            }

            CGPoint travel = recognizer.TranslationInView(this);
            CGPoint speed = recognizer.VelocityInView(this);

            bool far = travel.X >= Bounds.Width * BACK_SHARE;
            bool flicked = travel.X >= BACK_MIN_DISTANCE && speed.X >= BACK_FLICK_SPEED;

            if (far || flicked)
            {
                Offer(Key.Back);
            }
        }

        public bool Offer(Key key)
        {
            bool claimed = _host.KeyDown(key, KeyModifiers.None, false);

            _host.KeyUp(key, KeyModifiers.None);

            return claimed;
        }

        private sealed class BackGate : UIGestureRecognizerDelegate
        {
            private readonly IxenView _view;

            internal BackGate(IxenView view)
            {
                _view = view;
            }

            public override bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
            {
                return touch.LocationInView(_view).X <= BACK_EDGE;
            }

            public override bool ShouldBegin(UIGestureRecognizer recognizer)
            {
                CGPoint speed = ((UIPanGestureRecognizer)recognizer).VelocityInView(_view);

                return speed.X > 0 && Math.Abs(speed.X) > Math.Abs(speed.Y);
            }
        }
    }
}
