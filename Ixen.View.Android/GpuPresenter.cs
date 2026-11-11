using Android.Content;
using Android.Opengl;
using Android.Views;
using SkiaSharp;
using SkiaSharp.Views.Android;
using System;

namespace Ixen.View.Android
{
    internal sealed class GpuPresenter : Java.Lang.Object, Choreographer.IFrameCallback
    {
        private readonly SKGLSurfaceView _view;
        private readonly Action<SKCanvas, int, int> _produce;
        private readonly object _gate = new object();
        private readonly SKPictureRecorder _recorder = new SKPictureRecorder();

        private SKPicture _pending;
        private SKPicture _current;
        private bool _scheduled;
        private bool _released;

        internal GpuPresenter(Context context, Action<SKCanvas, int, int> produce)
        {
            _produce = produce;

            _view = new SKGLSurfaceView(context);
            _view.PaintSurface += OnPaintSurface;
            _view.PreserveEGLContextOnPause = true;
            _view.RenderMode = Rendermode.WhenDirty;
        }

        internal SKGLSurfaceView View => _view;

        internal Action<SKSurface, int, int> Presented { get; set; }

        internal void Schedule()
        {
            if (_scheduled || _released)
            {
                return;
            }

            _scheduled = true;
            Choreographer.Instance?.PostFrameCallback(this);
        }

        internal void ProduceNow()
        {
            if (_scheduled)
            {
                Choreographer.Instance?.RemoveFrameCallback(this);
            }

            DoFrame(0);
        }

        public void DoFrame(long frameTimeNanos)
        {
            _scheduled = false;

            int width = _view.Width;
            int height = _view.Height;

            if (_released || width <= 0 || height <= 0)
            {
                return;
            }

            SKCanvas canvas = _recorder.BeginRecording(new SKRect(0, 0, width, height));

            _produce(canvas, width, height);

            SKPicture picture = _recorder.EndRecording();
            SKPicture skipped;

            lock (_gate)
            {
                skipped = _pending;
                _pending = picture;
            }

            skipped?.Dispose();

            _view.RequestRender();
        }

        private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            SKPicture retired = null;
            bool fresh = false;

            lock (_gate)
            {
                if (_pending != null)
                {
                    retired = _current;
                    _current = _pending;
                    _pending = null;
                    fresh = true;
                }
            }

            retired?.Dispose();

            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            if (_current != null)
            {
                canvas.DrawPicture(_current);
            }

            canvas.Flush();

            if (fresh)
            {
                Presented?.Invoke(e.Surface, e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
            }
        }

        internal void Resume() => _view.OnResume();

        internal void Pause() => _view.OnPause();

        internal void Release()
        {
            _released = true;

            lock (_gate)
            {
                _pending?.Dispose();
                _pending = null;
            }

            _view.PaintSurface -= OnPaintSurface;
        }
    }
}
