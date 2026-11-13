using Ixen.Core;
using Ixen.Platform.Desktop;
using System;

namespace Ixen.AotApp
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var options = new IxenSurfaceInitOptions
            {
                Title = "Ixen AOT",
                Width = 900,
                Height = 600
            };

            var surface = new IxenSurface(new MainComponent(), options);

            if (args.Length == 2 && args[0] == "--capture")
            {
                return IxenDesktopApplication.CaptureWindow(surface, args[1]);
            }

            return IxenDesktopApplication.CreateWindow(surface);
        }
    }
}
