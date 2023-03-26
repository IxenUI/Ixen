using Ixen.Core.Rendering;

namespace Ixen.Core.Visual.Styles
{
    public class BorderStyle : RenderedStyle
    {
        private Pen _pen;
        private Color _color = Color.Black;
        private float _thickness = 1;
        
        public BorderType Type { get; set; } = BorderType.Outer;

        public BorderStyle()
        {
            UpdatePen();
        }

        public BorderStyle(string content)
            : base(content)
        {
            UpdatePen();
        }

        private void UpdatePen()
        {
            _pen = new Pen(_color, _thickness);
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                UpdatePen();
            }
        }

        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                UpdatePen();
            }
        }

        protected override bool Parse() => true;

        internal override void Render(VisualElement element, RendererContext context)
        {
            switch (Type)
            {
                case BorderType.Center:
                    context.DrawRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
                case BorderType.Inner:
                    context.DrawInnerRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
                case BorderType.Outer:
                    context.DrawOuterRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
            }
        }
    }
}
