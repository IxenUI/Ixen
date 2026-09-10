using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal class GpuWindowRenderer : WindowRenderer
    {
        private const uint GL_SAMPLES = 0x80A9;
        private const uint GL_STENCIL_BITS = 0x0D57;
        private const uint DEFAULT_FRAMEBUFFER = 0;

        private const SKColorType COLOR_TYPE = SKColorType.Rgba8888;

        [DllImport("opengl32.dll", EntryPoint = "glGetIntegerv")]
        private static extern void GetIntegerValue(uint name, out int value);

        private GRGlInterface _glInterface;
        private GRContext _context;
        private int _samples;
        private int _stencilBits;

        private GRBackendRenderTarget _target;
        private SKSurface _surface;
        private int _width;
        private int _height;
        private bool _dead;

        private GpuWindowRenderer(IntPtr window)
            : base(window)
        {
        }

        internal static GpuWindowRenderer TryCreate(IntPtr window)
        {
            if (WindowApi.CreateGlContext(window) == 0)
            {
                return null;
            }

            var renderer = new GpuWindowRenderer(window);

            return renderer.Attach() ? renderer : null;
        }

        internal override string Backend => "gpu";

        internal override bool PreservesFrame => false;

        internal override bool Alive => !_dead;

        internal override void Paint(int width, int height, Action<SKCanvas> render)
        {
            if (!Ready())
            {
                return;
            }

            SKSurface surface = Surface(width, height);

            if (surface == null)
            {
                return;
            }

            render(surface.Canvas);

            _context.Flush();
            _context.Submit(false);

            WindowApi.SwapGlBuffers(Window);
        }

        internal override SKImage Snapshot()
        {
            if (_surface == null)
            {
                return null;
            }

            _context.Flush();
            _context.Submit(true);

            return _surface.Snapshot();
        }

        private bool Ready()
        {
            if (_dead)
            {
                return false;
            }

            int status = WindowApi.EnsureGlContext(Window);

            if (status == (int)NativeGlStatus.Current)
            {
                return true;
            }

            if (status == (int)NativeGlStatus.Recreated && Rebuild())
            {
                return true;
            }

            _dead = true;

            return false;
        }

        private bool Rebuild()
        {
            Release();

            return Attach();
        }

        private bool Attach()
        {
            try
            {
                _glInterface = GRGlInterface.Create();

                if (_glInterface == null || !_glInterface.Validate())
                {
                    Release();

                    return false;
                }

                _context = GRContext.CreateGl(_glInterface);

                if (_context == null)
                {
                    Release();

                    return false;
                }

                GetIntegerValue(GL_SAMPLES, out int samples);
                GetIntegerValue(GL_STENCIL_BITS, out _stencilBits);

                int maxSamples = _context.GetMaxSurfaceSampleCount(COLOR_TYPE);

                _samples = samples > maxSamples ? maxSamples : samples;

                return true;
            }
            catch (Exception)
            {
                Release();

                return false;
            }
        }

        private void Release()
        {
            _surface?.Dispose();
            _target?.Dispose();
            _context?.AbandonContext(false);
            _context?.Dispose();
            _glInterface?.Dispose();

            _surface = null;
            _target = null;
            _context = null;
            _glInterface = null;
            _width = 0;
            _height = 0;
        }

        private SKSurface Surface(int width, int height)
        {
            if (_surface != null && _width == width && _height == height)
            {
                return _surface;
            }

            _surface?.Dispose();
            _target?.Dispose();

            _width = width;
            _height = height;

            var info = new GRGlFramebufferInfo(DEFAULT_FRAMEBUFFER, COLOR_TYPE.ToGlSizedFormat());

            _target = new GRBackendRenderTarget(width, height, _samples, _stencilBits, info);
            _surface = SKSurface.Create(_context, _target, GRSurfaceOrigin.BottomLeft, COLOR_TYPE);

            return _surface;
        }

        public override void Dispose()
        {
            Release();
        }
    }
}
