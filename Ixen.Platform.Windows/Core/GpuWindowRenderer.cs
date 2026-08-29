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

        private readonly GRGlInterface _glInterface;
        private readonly GRContext _context;
        private readonly int _samples;
        private readonly int _stencilBits;

        private GRBackendRenderTarget _target;
        private SKSurface _surface;
        private int _width;
        private int _height;

        private GpuWindowRenderer(IntPtr window, GRGlInterface glInterface, GRContext context,
            int samples, int stencilBits)
            : base(window)
        {
            _glInterface = glInterface;
            _context = context;
            _samples = samples;
            _stencilBits = stencilBits;
        }

        internal static GpuWindowRenderer TryCreate(IntPtr window)
        {
            if (WindowApi.CreateGlContext(window) == 0)
            {
                return null;
            }

            GRGlInterface glInterface = null;
            GRContext context = null;

            try
            {
                glInterface = GRGlInterface.Create();

                if (glInterface == null || !glInterface.Validate())
                {
                    glInterface?.Dispose();

                    return null;
                }

                context = GRContext.CreateGl(glInterface);

                if (context == null)
                {
                    glInterface.Dispose();

                    return null;
                }

                GetIntegerValue(GL_SAMPLES, out int samples);
                GetIntegerValue(GL_STENCIL_BITS, out int stencilBits);

                int maxSamples = context.GetMaxSurfaceSampleCount(COLOR_TYPE);

                if (samples > maxSamples)
                {
                    samples = maxSamples;
                }

                return new GpuWindowRenderer(window, glInterface, context, samples, stencilBits);
            }
            catch (Exception)
            {
                context?.Dispose();
                glInterface?.Dispose();

                return null;
            }
        }

        internal override string Backend => "gpu";

        internal override bool PreservesFrame => false;

        internal override void Paint(int width, int height, Action<SKCanvas> render)
        {
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
            _context?.AbandonContext(false);

            _surface?.Dispose();
            _target?.Dispose();
            _context?.Dispose();
            _glInterface?.Dispose();
        }
    }
}
