namespace Ixen.Core.Visual
{
    public class BoxedElement : DimensionalElement
    {
        private float _marginTop;
        private float _marginRight;
        private float _marginBottom;
        private float _marginLeft;

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

        internal float BorderInsideTop { get; set; }
        internal float BorderInsideRight { get; set; }
        internal float BorderInsideBottom { get; set; }
        internal float BorderInsideLeft { get; set; }

        internal float BorderOutsideTop { get; set; }
        internal float BorderOutsideRight { get; set; }
        internal float BorderOutsideBottom { get; set; }
        internal float BorderOutsideLeft { get; set; }

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
                + BorderOutsideLeft + BorderOutsideRight;

        internal float BoxHeight
            => Height
                + MarginTop + MarginBottom
                + BorderOutsideTop + BorderOutsideBottom;

        internal float ScrollbarGutterWidth { get; set; }
        internal float ScrollbarGutterHeight { get; set; }

        internal float ContentWidth
        {
            get
            {
                float value = Width - HorizontalPadding - HorizontalBorderInside - ScrollbarGutterWidth;
                return value < 0 ? 0 : value;
            }
        }

        internal float ContentHeight
        {
            get
            {
                float value = Height - VerticalPadding - VerticalBorderInside - ScrollbarGutterHeight;
                return value < 0 ? 0 : value;
            }
        }

        internal float HorizontalBorderInside
            => BorderInsideLeft + BorderInsideRight;

        internal float VerticalBorderInside
            => BorderInsideTop + BorderInsideBottom;

        internal float HorizontalBorderOutside
            => BorderOutsideLeft + BorderOutsideRight;

        internal float VerticalBorderOutside
            => BorderOutsideTop + BorderOutsideBottom;

        internal float HorizontalMargin
            => MarginLeft + MarginRight;

        internal float VerticalMargin
            => MarginTop + MarginBottom;

        internal float HorizontalPadding
            => PaddingRight + PaddingLeft;

        internal float VerticalPadding
            => PaddingTop + PaddingBottom;

        internal override void SetPosition(float x, float y)
        {
            base.SetPosition
            (
                x + MarginLeft + BorderOutsideLeft,
                y + MarginTop + BorderOutsideTop
            );
        }
    }
}
