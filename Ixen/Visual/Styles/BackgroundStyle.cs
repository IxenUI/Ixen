﻿using Ixen.Core;
using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public class BackgroundStyle : Style
    {
        public Color Color { get; set; } = Color.Transparent;
        public string ImageUrl { get; set; }
        public bool RepeatX { get; set; }
        public bool RepeatY { get; set; }

        public BackgroundStyle()
        {}

        public BackgroundStyle(string content)
            : base(content)
        {}

        public override void Parse()
        {}

        public void Render(VisualElement element, RendererContext context)
        {
            context.FillRectangle(element.X, element.Y, element.Width, element.Height, new Brush(Color));
        }
    }
}
