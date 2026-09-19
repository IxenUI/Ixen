using Ixen.Core;

namespace Ixen.Platform.Linux
{
    public static class IxenLinuxApplication
    {
        public static int CreateWindow(IxenSurface surface)
        {
            using (var window = new LinuxWindow(surface))
            {
                return window.Show();
            }
        }

        public static int CaptureWindow(IxenSurface surface, string path, int paints = 2)
        {
            using (var window = new LinuxWindow(surface))
            {
                surface.ReducedMotion = true;

                window.CaptureAfter(path, paints);

                return window.Show();
            }
        }
    }
}
