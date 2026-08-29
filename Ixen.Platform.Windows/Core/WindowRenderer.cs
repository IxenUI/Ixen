using SkiaSharp;
using System;

namespace Ixen.Platform.Windows
{
    internal abstract class WindowRenderer : IDisposable
    {
        protected readonly IntPtr Window;

        protected WindowRenderer(IntPtr window)
        {
            Window = window;
        }

        internal abstract string Backend { get; }

        internal abstract bool PreservesFrame { get; }

        internal abstract void Paint(int width, int height, Action<SKCanvas> render);

        public virtual void Dispose()
        {
        }
    }
}
