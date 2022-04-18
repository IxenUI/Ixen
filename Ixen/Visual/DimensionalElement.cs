namespace Ixen.Visual
{
    public abstract class DimensionalElement
    {
        internal virtual float X { get; set; }
        internal virtual float Y { get; set; }

        private float _width;
        internal float Width
        {
            get => _width;
            set => _width = value < 0 ? 0 : value;
        }

        private float _height;
        internal float Height
        {
            get => _height;
            set => _height = value < 0 ? 0 : value;
        }

        internal void SetSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        internal void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
