﻿namespace Ixen.Core.Visual.Styles
{
    public class LayoutStyle : Style
    {
        public LayoutType Type { get; set; } = LayoutType.Column;

        public LayoutStyle()
        {}

        public LayoutStyle(string content)
            : base(content)
        {}

        protected override void Parse()
        {}
    }
}
