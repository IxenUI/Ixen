using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Ixen.Core;
using Ixen.Core.Visual;
using SkiaSharp;
using SkiaSharp.Views.Android;

namespace Ixen.View.Android
{
    public class IxenView : SKCanvasView
    {
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
            PaintSurface += OnPaintSurface;
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
