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
    }
}
