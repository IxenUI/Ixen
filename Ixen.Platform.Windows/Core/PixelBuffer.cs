using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal class PixelBuffer : IDisposable
    {
        private int _width;
        private int _height;

        public IntPtr Ptr { get; private set; }
        public int RowBytes { get; private set; }

        ~PixelBuffer()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (Ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.Ptr);
                Ptr = IntPtr.Zero;
            }
        }

        public void EnsureAlloc(int width, int height)
        {
            if (_width == width && _height == height)
            {
                return;
            }

            if (Ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Ptr);
            }

            RowBytes = 4 * ((width * 32 + 31) / 32);
            Ptr = Marshal.AllocHGlobal(height * RowBytes);

            _width = width;
            _height = height;
        }
    }
}
