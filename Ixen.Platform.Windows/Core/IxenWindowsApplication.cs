using Ixen.Core;
using System;

namespace Ixen.Platform.Windows
{
    public static class IxenWindowsApplication
    {
        public static int CreateWindow(IxenSurface surface)
        {
            try
            {
                var window = new IxenWindow(surface);
                return window.Show();
            }
            catch
            {
                return 1;
            }
        }
    }
}
