using Ixen.Core;

namespace Ixen.Platform.Mac
{
    public static class IxenMacApplication
    {
        public static int CreateWindow(IxenSurface surface)
        {
            using (var window = new MacWindow(surface))
            {
                return window.Show();
            }
        }

        public static int CaptureWindow(IxenSurface surface, string path, int paints = 2)
        {
            using (var window = new MacWindow(surface))
            {
                surface.ReducedMotion = true;

                window.CaptureAfter(path, paints);

                return window.Show();
            }
        }
    }
}
