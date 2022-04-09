namespace Ixen.Rendering
{
    internal abstract class DimensionalElement
    {
        internal virtual float X { get; set; }
        internal virtual float Y { get; set; }

        private float _width;
        internal float Width
        {
            get => _width;
            set => _width = value < 0 ? 0 : value;
        }

        internal float _height;
        internal float Height
        {
            get => _height;
            set => _height = value < 0 ? 0 : value;
        }
    }
}
