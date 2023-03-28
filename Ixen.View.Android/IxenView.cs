using Android.Content;
using Android.Runtime;
using Android.Util;
using Ixen.Core;
using Ixen.Core.Visual;
using SkiaSharp.Views.Android;

namespace Ixen.View.Android
{
    public class IxenView : FrameLayout
    {
        private SKCanvasView _skCanvasView;
        private IxenSurface _ixenSurface = new IxenSurface();

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
            _skCanvasView.PaintSurface += OnPaintSurface;
            this.AddView(_skCanvasView);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _ixenSurface.ComputeLayout(e.Info.Width, e.Info.Height);
            _ixenSurface.Render(e.Surface.Canvas);
        }

        public VisualElement Root
        {
            get => _ixenSurface.Root;
            set => _ixenSurface.Root = value;
        }
    }
}
