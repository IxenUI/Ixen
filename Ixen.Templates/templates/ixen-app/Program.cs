using Ixen.Core;
using Ixen.Platform.Desktop;

namespace IxenApp
{
    public static class Program
    {
        public static int Main()
        {
            var options = new IxenSurfaceInitOptions
            {
                Title = "IxenApp",
                Width = 900,
                Height = 600
            };

            return IxenDesktopApplication.CreateWindow(new IxenSurface(new MainComponent(), options));
        }
    }
}
