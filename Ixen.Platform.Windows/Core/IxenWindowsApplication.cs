using Ixen.Core;
using System;

namespace Ixen.Platform.Windows
{
    public static class IxenWindowsApplication
    {
        public static int CreateWindow(IxenSurface surface)
        {
            var window = new IxenWindow(surface);
            return window.Show();
        }

        public static int CaptureWindow(IxenSurface surface, string path, int paints = 2)
        {
            var window = new IxenWindow(surface);

            surface.ReducedMotion = true;

            window.CaptureAfter(path, paints);

            return window.Show();
        }
    }
}
