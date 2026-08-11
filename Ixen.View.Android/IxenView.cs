using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Platform;
using SkiaSharp.Views.Android;

namespace Ixen.View.Android
{
    public class IxenView : FrameLayout
    {
        private SKCanvasView _skCanvasView;
        private IxenHost _host;

        public IxenView(Context context)
            : base(context)
        {
            Init();
        }

        public IxenView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init();
        }

        public IxenView(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Init();
        }

        protected IxenView(nint javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

        private void Init()
        {
            _skCanvasView = new SKCanvasView(Context);
            _host = new IxenHost(new IxenSurface(), _skCanvasView.Invalidate);

            _skCanvasView.PaintSurface += OnPaintSurface;
            _skCanvasView.Touch += OnTouch;

            AddView(_skCanvasView);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
            => _host.Paint(e.Surface.Canvas, e.Info.Width, e.Info.Height);

        private void OnTouch(object sender, TouchEventArgs e)
        {
            MotionEvent motion = e.Event;

            if (motion == null)
            {
                return;
            }

            float x = motion.GetX();
            float y = motion.GetY();

            switch (motion.Action)
            {
                case MotionEventActions.Down:
                    _host.PointerDown(x, y, PointerButton.Left);
                    break;

                case MotionEventActions.Move:
                    _host.PointerMove(x, y);
                    break;

                case MotionEventActions.Up:
                    _host.PointerUp(x, y, PointerButton.Left);
                    _host.PointerLeave();
                    break;

                case MotionEventActions.Cancel:
                    _host.PointerCaptureLost();
                    break;

                default:
                    return;
            }

            e.Handled = true;
        }

        public VisualElement Root
        {
            get => _host.Root;
            set => _host.Root = value;
        }
    }
}
