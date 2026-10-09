using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Ixen.Core.Visual.Shapes
{
    internal sealed class SvgShape
    {
        internal PathGeometry Geometry { get; }

        internal Matrix2D Transform { get; }

        internal string Fill { get; }

        internal string Stroke { get; }

        internal float StrokeWidth { get; }

        internal float FillAlpha { get; }

        internal float StrokeAlpha { get; }

        internal SvgShape(PathGeometry geometry, Matrix2D transform,
            string fill, string stroke, float strokeWidth, float fillAlpha, float strokeAlpha)
        {
            Geometry = geometry;
            Transform = transform;
            Fill = fill;
            Stroke = stroke;
            StrokeWidth = strokeWidth;
            FillAlpha = fillAlpha;
            StrokeAlpha = strokeAlpha;
        }
    }

    internal sealed class SvgDocument
    {
        private const int MAX_DEPTH = 64;
        private const int MAX_SHAPES = 4096;
        private const string NONE = "none";

        internal float Left { get; private set; }

        internal float Top { get; private set; }

        internal float Width { get; private set; }

        internal float Height { get; private set; }

        internal List<SvgShape> Shapes { get; } = new List<SvgShape>();

        private struct State
        {
            internal Matrix2D Transform;
            internal string Fill;
            internal string Stroke;
            internal float StrokeWidth;
            internal float Opacity;
            internal float FillOpacity;
            internal float StrokeOpacity;
            internal bool EvenOdd;
        }

        internal static SvgDocument Parse(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                XmlResolver = null,
                CloseInput = false
            };

            try
            {
                using (XmlReader reader = XmlReader.Create(stream, settings))
                {
                    return Read(reader);
                }
            }
            catch
            {
                return null;
            }
        }

        private static SvgDocument Read(XmlReader reader)
        {
            var document = new SvgDocument();
            var levels = new State[MAX_DEPTH];

            levels[0] = new State
            {
                Transform = Matrix2D.Identity,
                Fill = "#000000",
                Stroke = null,
                StrokeWidth = 1,
                Opacity = 1,
                FillOpacity = 1,
                StrokeOpacity = 1,
                EvenOdd = false
            };

            var started = false;
            int hidden = -1;

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                int depth = reader.Depth;

                if (depth >= MAX_DEPTH - 1)
                {
                    continue;
                }

                string name = reader.LocalName;

                if (hidden >= 0)
                {
                    if (depth > hidden)
                    {
                        continue;
                    }

                    hidden = -1;
                }

                if (!started)
                {
                    if (name != "svg")
                    {
                        return null;
                    }

                    ReadViewBox(reader, document);
                    started = true;
                }

                if (IsHidden(name))
                {
                    if (!reader.IsEmptyElement)
                    {
                        hidden = depth;
                    }

                    continue;
                }

                State state = Inherit(levels[depth], reader);

                if (!reader.IsEmptyElement)
                {
                    levels[depth + 1] = state;
                }

                string data = DataOf(name, reader);

                if (data == null || document.Shapes.Count >= MAX_SHAPES)
                {
                    continue;
                }

                PathGeometry geometry = PathData.Parse(data, state.EvenOdd);

                if (geometry == null)
                {
                    continue;
                }

                document.Shapes.Add(new SvgShape(geometry, state.Transform,
                    state.Fill, state.Stroke, state.StrokeWidth,
                    state.Opacity * state.FillOpacity, state.Opacity * state.StrokeOpacity));
            }

            return started && document.Width > 0 && document.Height > 0 ? document : null;
        }

        private static bool IsHidden(string name)
        {
            switch (name)
            {
                case "defs":
                case "symbol":
                case "clipPath":
                case "mask":
                case "pattern":
                case "marker":
                case "filter":
                case "linearGradient":
                case "radialGradient":
                case "style":
                case "script":
                case "metadata":
                case "title":
                case "desc":
                    return true;

                default:
                    return false;
            }
        }

        private static void ReadViewBox(XmlReader reader, SvgDocument document)
        {
            string box = reader.GetAttribute("viewBox");

            if (box != null)
            {
                string[] parts = box.Split(new[] { ' ', ',', '\t', '\r', '\n' },
                    System.StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 4
                    && Number(parts[0], out float left) && Number(parts[1], out float top)
                    && Number(parts[2], out float width) && Number(parts[3], out float height)
                    && width > 0 && height > 0)
                {
                    document.Left = left;
                    document.Top = top;
                    document.Width = width;
                    document.Height = height;

                    return;
                }
            }

            document.Left = 0;
            document.Top = 0;
            document.Width = Length(reader.GetAttribute("width"), 0);
            document.Height = Length(reader.GetAttribute("height"), 0);
        }

        private static State Inherit(State parent, XmlReader reader)
        {
            State state = parent;

            string style = reader.GetAttribute("style");

            Apply(ref state, "fill", Declaration(style, "fill") ?? reader.GetAttribute("fill"));
            Apply(ref state, "stroke", Declaration(style, "stroke") ?? reader.GetAttribute("stroke"));
            Apply(ref state, "stroke-width",
                Declaration(style, "stroke-width") ?? reader.GetAttribute("stroke-width"));
            Apply(ref state, "fill-rule",
                Declaration(style, "fill-rule") ?? reader.GetAttribute("fill-rule"));
            Apply(ref state, "fill-opacity",
                Declaration(style, "fill-opacity") ?? reader.GetAttribute("fill-opacity"));
            Apply(ref state, "stroke-opacity",
                Declaration(style, "stroke-opacity") ?? reader.GetAttribute("stroke-opacity"));
            Apply(ref state, "opacity",
                Declaration(style, "opacity") ?? reader.GetAttribute("opacity"));

            string transform = reader.GetAttribute("transform");

            if (transform != null)
            {
                state.Transform = Matrix2D.Concat(state.Transform, Transform(transform));
            }

            return state;
        }

        private static void Apply(ref State state, string property, string value)
        {
            if (value == null)
            {
                return;
            }

            value = value.Trim();

            if (value.Length == 0 || value == "inherit")
            {
                return;
            }

            switch (property)
            {
                case "fill":
                    state.Fill = Paint(value);
                    return;

                case "stroke":
                    state.Stroke = Paint(value);
                    return;

                case "stroke-width":
                    state.StrokeWidth = Length(value, state.StrokeWidth);
                    return;

                case "fill-rule":
                    state.EvenOdd = value == "evenodd";
                    return;

                case "fill-opacity":
                    state.FillOpacity = Alpha(value, state.FillOpacity);
                    return;

                case "stroke-opacity":
                    state.StrokeOpacity = Alpha(value, state.StrokeOpacity);
                    return;

                default:
                    state.Opacity *= Alpha(value, 1);
                    return;
            }
        }

        private static string Paint(string value)
        {
            if (value == NONE || value == "transparent" || value.StartsWith("url("))
            {
                return null;
            }

            if (value == "currentColor")
            {
                return "#000000";
            }

            return value;
        }

        private static string Declaration(string style, string property)
        {
            if (style == null)
            {
                return null;
            }

            int at = 0;

            while (at < style.Length)
            {
                int end = style.IndexOf(';', at);

                if (end < 0)
                {
                    end = style.Length;
                }

                int colon = style.IndexOf(':', at);

                if (colon > at && colon < end
                    && string.CompareOrdinal(style.Substring(at, colon - at).Trim(), property) == 0)
                {
                    return style.Substring(colon + 1, end - colon - 1).Trim();
                }

                at = end + 1;
            }

            return null;
        }

        private static Matrix2D Transform(string value)
        {
            Matrix2D matrix = Matrix2D.Identity;
            int at = 0;

            while (at < value.Length)
            {
                int open = value.IndexOf('(', at);

                if (open < 0)
                {
                    break;
                }

                int close = value.IndexOf(')', open);

                if (close < 0)
                {
                    break;
                }

                string name = value.Substring(at, open - at).Trim(' ', ',', '\t', '\r', '\n');
                string[] parts = value.Substring(open + 1, close - open - 1)
                    .Split(new[] { ' ', ',', '\t', '\r', '\n' },
                        System.StringSplitOptions.RemoveEmptyEntries);

                var numbers = new float[parts.Length];
                var ok = true;

                for (int index = 0; index < parts.Length; index++)
                {
                    ok &= Number(parts[index], out numbers[index]);
                }

                if (ok)
                {
                    matrix = Matrix2D.Concat(matrix, Operation(name, numbers));
                }

                at = close + 1;
            }

            return matrix;
        }

        private static Matrix2D Operation(string name, float[] numbers)
        {
            switch (name)
            {
                case "translate" when numbers.Length >= 1:
                    return Matrix2D.Translation(numbers[0], numbers.Length > 1 ? numbers[1] : 0);

                case "scale" when numbers.Length >= 1:
                    return Matrix2D.Scaling(numbers[0], numbers.Length > 1 ? numbers[1] : numbers[0]);

                case "rotate" when numbers.Length >= 3:
                    return Matrix2D.Concat(
                        Matrix2D.Concat(Matrix2D.Translation(numbers[1], numbers[2]),
                            Matrix2D.Rotation(numbers[0])),
                        Matrix2D.Translation(-numbers[1], -numbers[2]));

                case "rotate" when numbers.Length >= 1:
                    return Matrix2D.Rotation(numbers[0]);

                case "skewX" when numbers.Length >= 1:
                    return Matrix2D.Skewing(numbers[0], 0);

                case "skewY" when numbers.Length >= 1:
                    return Matrix2D.Skewing(0, numbers[0]);

                case "matrix" when numbers.Length >= 6:
                    return new Matrix2D(numbers[0], numbers[2], numbers[4],
                        numbers[1], numbers[3], numbers[5]);

                default:
                    return Matrix2D.Identity;
            }
        }

        private static string DataOf(string name, XmlReader reader)
        {
            switch (name)
            {
                case "path":
                    return reader.GetAttribute("d");

                case "rect":
                    return Rectangle(reader);

                case "circle":
                    return Ellipse(Length(reader.GetAttribute("cx"), 0),
                        Length(reader.GetAttribute("cy"), 0),
                        Length(reader.GetAttribute("r"), 0),
                        Length(reader.GetAttribute("r"), 0));

                case "ellipse":
                    return Ellipse(Length(reader.GetAttribute("cx"), 0),
                        Length(reader.GetAttribute("cy"), 0),
                        Length(reader.GetAttribute("rx"), 0),
                        Length(reader.GetAttribute("ry"), 0));

                case "line":
                    return Segment(reader);

                case "polyline":
                    return Points(reader.GetAttribute("points"), false);

                case "polygon":
                    return Points(reader.GetAttribute("points"), true);

                default:
                    return null;
            }
        }

        private static string Rectangle(XmlReader reader)
        {
            float x = Length(reader.GetAttribute("x"), 0);
            float y = Length(reader.GetAttribute("y"), 0);
            float width = Length(reader.GetAttribute("width"), 0);
            float height = Length(reader.GetAttribute("height"), 0);

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            float rx = Length(reader.GetAttribute("rx"), -1);
            float ry = Length(reader.GetAttribute("ry"), -1);

            if (rx < 0)
            {
                rx = ry < 0 ? 0 : ry;
            }

            if (ry < 0)
            {
                ry = rx;
            }

            rx = rx > width / 2 ? width / 2 : rx;
            ry = ry > height / 2 ? height / 2 : ry;

            if (rx <= 0 || ry <= 0)
            {
                return Build("M ", x, " ", y, " H ", x + width, " V ", y + height, " H ", x, " Z");
            }

            return Build("M ", x + rx, " ", y,
                " H ", x + width - rx,
                " A ", rx, " ", ry, " 0 0 1 ", x + width, " ", y + ry,
                " V ", y + height - ry,
                " A ", rx, " ", ry, " 0 0 1 ", x + width - rx, " ", y + height,
                " H ", x + rx,
                " A ", rx, " ", ry, " 0 0 1 ", x, " ", y + height - ry,
                " V ", y + ry,
                " A ", rx, " ", ry, " 0 0 1 ", x + rx, " ", y, " Z");
        }

        private static string Ellipse(float cx, float cy, float rx, float ry)
        {
            if (rx <= 0 || ry <= 0)
            {
                return null;
            }

            return Build("M ", cx - rx, " ", cy,
                " A ", rx, " ", ry, " 0 1 0 ", cx + rx, " ", cy,
                " A ", rx, " ", ry, " 0 1 0 ", cx - rx, " ", cy, " Z");
        }

        private static string Segment(XmlReader reader)
        {
            return Build("M ", Length(reader.GetAttribute("x1"), 0),
                " ", Length(reader.GetAttribute("y1"), 0),
                " L ", Length(reader.GetAttribute("x2"), 0),
                " ", Length(reader.GetAttribute("y2"), 0));
        }

        private static string Points(string value, bool close)
        {
            if (value == null)
            {
                return null;
            }

            string[] parts = value.Split(new[] { ' ', ',', '\t', '\r', '\n' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
            {
                return null;
            }

            var builder = new StringBuilder();

            for (int index = 0; index + 1 < parts.Length; index += 2)
            {
                if (!Number(parts[index], out float x) || !Number(parts[index + 1], out float y))
                {
                    return null;
                }

                builder.Append(index == 0 ? "M " : " L ");
                builder.Append(Text(x));
                builder.Append(' ');
                builder.Append(Text(y));
            }

            if (close)
            {
                builder.Append(" Z");
            }

            return builder.ToString();
        }

        private static string Build(params object[] parts)
        {
            var builder = new StringBuilder();

            foreach (object part in parts)
            {
                builder.Append(part is float number ? Text(number) : (string)part);
            }

            return builder.ToString();
        }

        private static string Text(float value)
            => value.ToString("R", CultureInfo.InvariantCulture);

        private static float Length(string value, float fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            value = value.Trim();

            if (value.EndsWith("px"))
            {
                value = value.Substring(0, value.Length - 2);
            }

            return Number(value, out float number) ? number : fallback;
        }

        private static float Alpha(string value, float fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            value = value.Trim();

            bool percent = value.EndsWith("%");

            if (percent)
            {
                value = value.Substring(0, value.Length - 1);
            }

            if (!Number(value, out float number))
            {
                return fallback;
            }

            if (percent)
            {
                number /= 100;
            }

            return number < 0 ? 0 : number > 1 ? 1 : number;
        }

        private static bool Number(string value, out float number)
            => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }
}
