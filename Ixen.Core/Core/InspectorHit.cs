using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using System;
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
            Path = PathOf(element, int.MaxValue);
        }

        public string Path { get; }

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

        private const string PATH_SEPARATOR = " > ";
        private const string PATH_ELISION = "... > ";

        internal static string PathOf(VisualElement element, int segments)
        {
            var names = new List<string>();

            for (VisualElement walked = element; walked != null; walked = walked.Parent)
            {
                names.Add(NameOf(walked));
            }

            var text = new StringBuilder();
            int shown = Math.Min(segments, names.Count);

            if (shown < names.Count)
            {
                text.Append(PATH_ELISION);
            }

            for (int index = shown - 1; index >= 0; index--)
            {
                text.Append(names[index]);

                if (index > 0)
                {
                    text.Append(PATH_SEPARATOR);
                }
            }

            return text.ToString();
        }

        private static string NameOf(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.Name))
            {
                return element.Name;
            }

            return string.IsNullOrEmpty(element.TypeName) ? "(unnamed)" : "#" + element.TypeName;
        }

        internal static string Describe(VisualElement element, float width, float height)
        {
            var text = new StringBuilder(NameOf(element));

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
