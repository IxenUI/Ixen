namespace Ixen.Visual
{
    public abstract class DimensionalElement
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        internal virtual float X
        {
            get => _x;
            set => _x = value;
        }

        internal virtual float Y
        {
            get => _y;
            set => _y = value;
        }

        internal float Width
        {
            get => _width;
            set => _width = value < 0 ? 0 : value;
        }

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
