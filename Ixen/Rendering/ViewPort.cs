namespace Ixen.Rendering
{
    internal sealed class ViewPort : DimensionalElement
    {
        private float _x;
        internal override float X
        {
            get => _x;
            set => _x = value < 0 ? 0 : value;
        }

        private float _y;
        internal override float Y
        {
            get => _y;
            set => _y = value < 0 ? 0 : value;
        }
    }
}
