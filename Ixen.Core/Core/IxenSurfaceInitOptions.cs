namespace Ixen.Core
{
    public class IxenSurfaceInitOptions
    {
        public string Title { get; set; } = "Ixen";
        public int Width { get; set; } = 640;
        public int Height { get; set; } = 480;
        public bool Maximized { get; set; } = false;
        public bool Minimized { get; set; } = false;
    }
}
