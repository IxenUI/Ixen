namespace Ixen.Core.Visual
{
    public class BoxedElement : DimensionalElement
    {
        private float _marginTop;
        private float _marginRight;
        private float _marginBottom;
        private float _marginLeft;

        private float _borderTop;
        private float _borderRight;
        private float _borderBottom;
        private float _borderLeft;

        private float _paddingTop;
        private float _paddingRight;
        private float _paddingBottom;
        private float _paddingLeft;

        internal float MarginTop
        {
            get => _marginTop;
            set => _marginTop = value;
        }

        internal float MarginRight
        {
            get => _marginRight;
            set => _marginRight = value;
        }

        internal float MarginBottom
        {
            get => _marginBottom;
            set => _marginBottom = value;
        }

        internal float MarginLeft
        {
            get => _marginLeft;
            set => _marginLeft = value;
        }

        internal float BorderTop
        {
            get => _borderTop;
            set => _borderTop = value;
        }

        internal float BorderRight
        {
            get => _borderRight;
            set => _borderRight = value;
        }

        internal float BorderBottom
        {
            get => _borderBottom;
            set => _borderBottom = value;
        }

        internal float BorderLeft
        {
            get => _borderLeft;
            set => _borderLeft = value;
        }

        internal float PaddingTop
        {
            get => _paddingTop;
            set => _paddingTop = value;
        }

        internal float PaddingRight
        {
            get => _paddingRight;
            set => _paddingRight = value;
        }

        internal float PaddingBottom
        {
            get => _paddingBottom;
            set => _paddingBottom = value;
        }

        internal float PaddingLeft
        {
            get => _paddingLeft;
            set => _paddingLeft = value;
        }

        internal float BoxWidth
            => Width
                + MarginLeft + MarginRight
                + BorderLeft + BorderRight;

        internal float BoxHeight
            => Height
                + MarginTop + MarginBottom
                + BorderTop + BorderBottom;

        internal float HorizontalMargin
            => MarginLeft + MarginRight;

        internal float VerticalMargin
            => MarginTop + MarginBottom;

        internal override void SetPosition(float x, float y)
        {
            base.SetPosition
            (
                x + MarginLeft + BorderLeft,
                y + MarginTop + BorderTop
            );
        }
    }
}
