using Ixen.Core;
using Ixen.Platform.Desktop;
using System;

namespace IxenApp
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            var options = new IxenSurfaceInitOptions
            {
                Title = "Ixen App",
                Width = 900,
                Height = 600
            };

            return IxenDesktopApplication.CreateWindow(new IxenSurface(new MainComponent(), options));
        }
    }
}
