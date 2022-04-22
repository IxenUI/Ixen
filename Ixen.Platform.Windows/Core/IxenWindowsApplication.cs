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
    }
}
