using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class PositionReader
    {
        internal const string LEFT = "left";
        internal const string CENTER = "center";
        internal const string RIGHT = "right";
        internal const string TOP = "top";
        internal const string MIDDLE = "middle";
        internal const string BOTTOM = "bottom";

        private enum Edge
        {
            None,
            Left,
            Right,
            Top,
            Bottom
        }

        private static readonly Regex _percent = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)%$");
        private static readonly Regex _pixels = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)px$");

        private Edge _edge;
        private bool _offsetX;
        private bool _offsetY;

        internal bool HasX { get; private set; }
        internal bool HasY { get; private set; }

        internal float X { get; private set; }
        internal float Y { get; private set; }
        internal float OffsetX { get; private set; }
        internal float OffsetY { get; private set; }

        internal bool IsInvalid { get; private set; }

        internal bool Accept(string token)
        {
            Edge edge = _edge;
            _edge = Edge.None;

            switch (token)
            {
                case LEFT:
                    return SetX(0f, Edge.Left);

                case CENTER:
                    return SetX(0.5f, Edge.None);

                case RIGHT:
                    return SetX(1f, Edge.Right);

                case TOP:
                    return SetY(0f, Edge.Top);

                case MIDDLE:
                    return SetY(0.5f, Edge.None);

                case BOTTOM:
                    return SetY(1f, Edge.Bottom);
            }

            if (TryNumber(_pixels, token, out float pixels))
            {
                return edge != Edge.None ? Offset(edge, pixels) : Distance(pixels);
            }

            if (TryNumber(_percent, token, out float percent))
            {
                return HasX ? SetY(percent / 100f, Edge.None) : SetX(percent / 100f, Edge.None);
            }

            return false;
        }

        internal void Interrupt() => _edge = Edge.None;

        private bool SetX(float value, Edge edge)
        {
            if (HasX)
            {
                return Refuse();
            }

            HasX = true;
            X = value;
            _edge = edge;
            return true;
        }

        private bool SetY(float value, Edge edge)
        {
            if (HasY)
            {
                return Refuse();
            }

            HasY = true;
            Y = value;
            _edge = edge;
            return true;
        }

        private bool Distance(float pixels)
        {
            if (!HasX)
            {
                SetX(0f, Edge.None);
                _offsetX = true;
                OffsetX = pixels;
                return true;
            }

            if (!SetY(0f, Edge.None))
            {
                return false;
            }

            _offsetY = true;
            OffsetY = pixels;
            return true;
        }

        private bool Offset(Edge edge, float pixels)
        {
            if (edge == Edge.Left || edge == Edge.Right)
            {
                if (_offsetX)
                {
                    return Refuse();
                }

                _offsetX = true;
                OffsetX = edge == Edge.Right ? -pixels : pixels;
                return true;
            }

            if (_offsetY)
            {
                return Refuse();
            }

            _offsetY = true;
            OffsetY = edge == Edge.Bottom ? -pixels : pixels;
            return true;
        }

        private bool Refuse()
        {
            IsInvalid = true;
            return false;
        }

        private static bool TryNumber(Regex pattern, string token, out float value)
        {
            value = 0;
            Match match = pattern.Match(token);

            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value);
        }
    }
}
