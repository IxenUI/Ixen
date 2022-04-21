namespace Ixen.Core.Visual
{
    public sealed class ViewPort : DimensionalElement
    {
        private float _x;
        private float _y;

        internal override float X
        {
            get => _x;
            set => _x = value < 0 ? 0 : value;
        }

        internal override float Y
        {
            get => _y;
            set => _y = value < 0 ? 0 : value;
        }
    }
}
