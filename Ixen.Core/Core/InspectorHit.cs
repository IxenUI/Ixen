using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core
{
    public sealed class InspectorHit
    {
        internal InspectorHit(VisualElement element, StyleTrace styles, int depth)
        {
            Element = element;
            Styles = styles;
            Depth = depth;

            X = element.X;
            Y = element.Y;
            Width = element.ActualWidth;
            Height = element.ActualHeight;

            ContentX = element.ContentX;
            ContentY = element.ContentY;
            ContentWidth = element.ContentWidth;
            ContentHeight = element.ContentHeight;

            Label = Describe(element, Width, Height);
        }

        public VisualElement Element { get; }

        public StyleTrace Styles { get; }

        public int Depth { get; }

        public float X { get; }

        public float Y { get; }

        public float Width { get; }

        public float Height { get; }

        public float ContentX { get; }

        public float ContentY { get; }

        public float ContentWidth { get; }

        public float ContentHeight { get; }

        public string Label { get; }

        internal static string Describe(VisualElement element, float width, float height)
        {
            var text = new StringBuilder();

            if (!string.IsNullOrEmpty(element.Name))
            {
                text.Append(element.Name);
            }
            else if (!string.IsNullOrEmpty(element.TypeName))
            {
                text.Append('#').Append(element.TypeName);
            }
            else
            {
                text.Append("(unnamed)");
            }

            List<string> classes = element.Classes;

            if (classes != null)
            {
                foreach (string name in classes)
                {
                    text.Append('.').Append(name);
                }
            }

            text.Append("  ")
                .Append(width.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(" x ")
                .Append(height.ToString("0.#", CultureInfo.InvariantCulture));

            return text.ToString();
        }
    }
}
