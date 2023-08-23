namespace Ixen.Core.Visual
{
    public class DimensionalElement
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;
        private float _renderWidth;
        private float _renderHeight;

        internal bool Renderable => _renderWidth > 0 || _renderHeight > 0;

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

        internal float RenderWidth
        {
            get => _renderWidth;
            set => _renderWidth = value < 0 ? 0 : value;
        }

        internal float RenderHeight
        {
            get => _renderHeight;
            set => _renderHeight = value < 0 ? 0 : value;
        }

        internal virtual void SetSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        internal virtual void SetRenderSize(float width, float height)
        {
            RenderWidth = width;
            RenderHeight = height;
        }

        internal virtual void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
