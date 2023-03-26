using Ixen.Core.Rendering;
using System;

namespace Ixen.Core.Visual.Styles
{
    public class BackgroundStyle : RenderedStyle
    {
        private Brush _brush;
        private Color _color = Color.Transparent;
        public string ImageUrl { get; set; }
        public bool RepeatX { get; set; }
        public bool RepeatY { get; set; }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                UpdateBrush();
            }
        }

        public BackgroundStyle()
        {
            UpdateBrush();
        }

        public BackgroundStyle(string content)
            : base(content)
        {
            UpdateBrush();
        }

        private void UpdateBrush()
        {
            _brush = new Brush(_color);
        }

        protected override bool Parse() => true;

        internal override void Render(VisualElement element, RendererContext context)
        {
            context.FillRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _brush);
        }
    }
}
