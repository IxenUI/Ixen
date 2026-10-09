using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Visual.Shapes
{
    internal static class PathData
    {
        private const int MAX_ARC_SEGMENTS = 4;
        private const float EPSILON = 1e-7f;

        internal static PathGeometry Parse(string data, bool evenOdd)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            var builder = new PathBuilder();

            if (!Read(data, builder))
            {
                return null;
            }

            return builder.Build(evenOdd);
        }

        private static bool Read(string data, PathBuilder builder)
        {
            int index = 0;
            char verb = (char)0;

            float startX = 0;
            float startY = 0;
            float currentX = 0;
            float currentY = 0;
            float controlX = 0;
            float controlY = 0;
            char previous = (char)0;

            while (true)
            {
                SkipSeparators(data, ref index);

                if (index >= data.Length)
                {
                    return builder.HasContent;
                }

                char character = data[index];

                if (IsVerb(character))
                {
                    verb = character;
                    index++;
                }
                else if (verb == (char)0)
                {
                    return false;
                }
                else if (verb == 'M')
                {
                    verb = 'L';
                }
                else if (verb == 'm')
                {
                    verb = 'l';
                }
                else if (verb == 'Z' || verb == 'z')
                {
                    return false;
                }

                bool relative = char.IsLower(verb);
                float originX = relative ? currentX : 0;
                float originY = relative ? currentY : 0;

                switch (char.ToUpperInvariant(verb))
                {
                    case 'M':
                        if (!ReadNumber(data, ref index, out float moveX)
                            || !ReadNumber(data, ref index, out float moveY))
                        {
                            return false;
                        }

                        currentX = originX + moveX;
                        currentY = originY + moveY;
                        startX = currentX;
                        startY = currentY;
                        builder.Move(currentX, currentY);
                        break;

                    case 'L':
                        if (!ReadNumber(data, ref index, out float lineX)
                            || !ReadNumber(data, ref index, out float lineY))
                        {
                            return false;
                        }

                        currentX = originX + lineX;
                        currentY = originY + lineY;
                        builder.Line(currentX, currentY);
                        break;

                    case 'H':
                        if (!ReadNumber(data, ref index, out float horizontal))
                        {
                            return false;
                        }

                        currentX = originX + horizontal;
                        builder.Line(currentX, currentY);
                        break;

                    case 'V':
                        if (!ReadNumber(data, ref index, out float vertical))
                        {
                            return false;
                        }

                        currentY = originY + vertical;
                        builder.Line(currentX, currentY);
                        break;

                    case 'C':
                        if (!ReadNumber(data, ref index, out float firstX)
                            || !ReadNumber(data, ref index, out float firstY)
                            || !ReadNumber(data, ref index, out float secondX)
                            || !ReadNumber(data, ref index, out float secondY)
                            || !ReadNumber(data, ref index, out float cubicX)
                            || !ReadNumber(data, ref index, out float cubicY))
                        {
                            return false;
                        }

                        controlX = originX + secondX;
                        controlY = originY + secondY;
                        builder.Cubic(originX + firstX, originY + firstY, controlX, controlY,
                            originX + cubicX, originY + cubicY);
                        currentX = originX + cubicX;
                        currentY = originY + cubicY;
                        break;

                    case 'S':
                        if (!ReadNumber(data, ref index, out float smoothControlX)
                            || !ReadNumber(data, ref index, out float smoothControlY)
                            || !ReadNumber(data, ref index, out float smoothX)
                            || !ReadNumber(data, ref index, out float smoothY))
                        {
                            return false;
                        }

                        float reflectedX = IsCubic(previous) ? currentX * 2 - controlX : currentX;
                        float reflectedY = IsCubic(previous) ? currentY * 2 - controlY : currentY;

                        controlX = originX + smoothControlX;
                        controlY = originY + smoothControlY;
                        builder.Cubic(reflectedX, reflectedY, controlX, controlY,
                            originX + smoothX, originY + smoothY);
                        currentX = originX + smoothX;
                        currentY = originY + smoothY;
                        break;

                    case 'Q':
                        if (!ReadNumber(data, ref index, out float quadControlX)
                            || !ReadNumber(data, ref index, out float quadControlY)
                            || !ReadNumber(data, ref index, out float quadX)
                            || !ReadNumber(data, ref index, out float quadY))
                        {
                            return false;
                        }

                        controlX = originX + quadControlX;
                        controlY = originY + quadControlY;
                        builder.Quad(controlX, controlY, originX + quadX, originY + quadY);
                        currentX = originX + quadX;
                        currentY = originY + quadY;
                        break;

                    case 'T':
                        if (!ReadNumber(data, ref index, out float tightX)
                            || !ReadNumber(data, ref index, out float tightY))
                        {
                            return false;
                        }

                        controlX = IsQuad(previous) ? currentX * 2 - controlX : currentX;
                        controlY = IsQuad(previous) ? currentY * 2 - controlY : currentY;

                        builder.Quad(controlX, controlY, originX + tightX, originY + tightY);
                        currentX = originX + tightX;
                        currentY = originY + tightY;
                        break;

                    case 'A':
                        if (!ReadNumber(data, ref index, out float radiusX)
                            || !ReadNumber(data, ref index, out float radiusY)
                            || !ReadNumber(data, ref index, out float rotation)
                            || !ReadFlag(data, ref index, out bool largeArc)
                            || !ReadFlag(data, ref index, out bool sweep)
                            || !ReadNumber(data, ref index, out float arcX)
                            || !ReadNumber(data, ref index, out float arcY))
                        {
                            return false;
                        }

                        float endX = originX + arcX;
                        float endY = originY + arcY;

                        Arc(builder, currentX, currentY, radiusX, radiusY, rotation,
                            largeArc, sweep, endX, endY);

                        currentX = endX;
                        currentY = endY;
                        break;

                    case 'Z':
                        builder.Close();
                        currentX = startX;
                        currentY = startY;
                        break;

                    default:
                        return false;
                }

                previous = verb;
            }
        }

        private static bool IsVerb(char character)
        {
            switch (char.ToUpperInvariant(character))
            {
                case 'M':
                case 'L':
                case 'H':
                case 'V':
                case 'C':
                case 'S':
                case 'Q':
                case 'T':
                case 'A':
                case 'Z':
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsCubic(char verb)
            => verb == 'C' || verb == 'c' || verb == 'S' || verb == 's';

        private static bool IsQuad(char verb)
            => verb == 'Q' || verb == 'q' || verb == 'T' || verb == 't';

        private static void SkipSeparators(string data, ref int index)
        {
            while (index < data.Length)
            {
                char character = data[index];

                if (character == ',' || character == ' ' || character == '\t'
                    || character == '\r' || character == '\n')
                {
                    index++;
                    continue;
                }

                return;
            }
        }

        private static bool ReadFlag(string data, ref int index, out bool value)
        {
            SkipSeparators(data, ref index);

            value = false;

            if (index >= data.Length)
            {
                return false;
            }

            char character = data[index];

            if (character != '0' && character != '1')
            {
                return false;
            }

            value = character == '1';
            index++;

            return true;
        }

        private static bool ReadNumber(string data, ref int index, out float value)
        {
            SkipSeparators(data, ref index);

            value = 0;

            int start = index;

            if (index < data.Length && (data[index] == '-' || data[index] == '+'))
            {
                index++;
            }

            bool digits = false;

            while (index < data.Length && char.IsDigit(data[index]))
            {
                index++;
                digits = true;
            }

            if (index < data.Length && data[index] == '.')
            {
                index++;

                while (index < data.Length && char.IsDigit(data[index]))
                {
                    index++;
                    digits = true;
                }
            }

            if (!digits)
            {
                index = start;

                return false;
            }

            if (index < data.Length && (data[index] == 'e' || data[index] == 'E'))
            {
                int exponent = index + 1;

                if (exponent < data.Length && (data[exponent] == '-' || data[exponent] == '+'))
                {
                    exponent++;
                }

                bool exponentDigits = false;

                while (exponent < data.Length && char.IsDigit(data[exponent]))
                {
                    exponent++;
                    exponentDigits = true;
                }

                if (exponentDigits)
                {
                    index = exponent;
                }
            }

            return float.TryParse(data.Substring(start, index - start),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static void Arc(PathBuilder builder, float x1, float y1,
            float radiusX, float radiusY, float rotation, bool largeArc, bool sweep,
            float x2, float y2)
        {
            if (radiusX == 0 || radiusY == 0)
            {
                builder.Line(x2, y2);

                return;
            }

            if (Math.Abs(x2 - x1) < EPSILON && Math.Abs(y2 - y1) < EPSILON)
            {
                return;
            }

            double rx = Math.Abs(radiusX);
            double ry = Math.Abs(radiusY);
            double phi = rotation * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            double halfX = (x1 - x2) / 2.0;
            double halfY = (y1 - y2) / 2.0;

            double primeX = cosPhi * halfX + sinPhi * halfY;
            double primeY = -sinPhi * halfX + cosPhi * halfY;

            double lambda = primeX * primeX / (rx * rx) + primeY * primeY / (ry * ry);

            if (lambda > 1.0)
            {
                double root = Math.Sqrt(lambda);
                rx *= root;
                ry *= root;
            }

            double denominator = rx * rx * primeY * primeY + ry * ry * primeX * primeX;
            double numerator = rx * rx * ry * ry - denominator;
            double factor = denominator <= 0 ? 0 : Math.Sqrt(Math.Max(0, numerator / denominator));

            if (largeArc == sweep)
            {
                factor = -factor;
            }

            double centrePrimeX = factor * rx * primeY / ry;
            double centrePrimeY = -factor * ry * primeX / rx;

            double centreX = cosPhi * centrePrimeX - sinPhi * centrePrimeY + (x1 + x2) / 2.0;
            double centreY = sinPhi * centrePrimeX + cosPhi * centrePrimeY + (y1 + y2) / 2.0;

            double startVectorX = (primeX - centrePrimeX) / rx;
            double startVectorY = (primeY - centrePrimeY) / ry;
            double endVectorX = (-primeX - centrePrimeX) / rx;
            double endVectorY = (-primeY - centrePrimeY) / ry;

            double start = Angle(1, 0, startVectorX, startVectorY);
            double sweepAngle = Angle(startVectorX, startVectorY, endVectorX, endVectorY);

            if (!sweep && sweepAngle > 0)
            {
                sweepAngle -= 2 * Math.PI;
            }
            else if (sweep && sweepAngle < 0)
            {
                sweepAngle += 2 * Math.PI;
            }

            int segments = (int)Math.Ceiling(Math.Abs(sweepAngle) / (Math.PI / 2));

            if (segments < 1)
            {
                segments = 1;
            }

            if (segments > MAX_ARC_SEGMENTS)
            {
                segments = MAX_ARC_SEGMENTS;
            }

            double step = sweepAngle / segments;
            double alpha = Math.Sin(step) * (Math.Sqrt(4 + 3 * Math.Pow(Math.Tan(step / 2), 2)) - 1) / 3;

            double currentAngle = start;

            for (int segment = 0; segment < segments; segment++)
            {
                double nextAngle = currentAngle + step;

                Evaluate(centreX, centreY, rx, ry, cosPhi, sinPhi, currentAngle,
                    out double fromX, out double fromY, out double fromDx, out double fromDy);
                Evaluate(centreX, centreY, rx, ry, cosPhi, sinPhi, nextAngle,
                    out double toX, out double toY, out double toDx, out double toDy);

                builder.Cubic(
                    (float)(fromX + alpha * fromDx), (float)(fromY + alpha * fromDy),
                    (float)(toX - alpha * toDx), (float)(toY - alpha * toDy),
                    (float)toX, (float)toY);

                currentAngle = nextAngle;
            }
        }

        private static void Evaluate(double centreX, double centreY, double rx, double ry,
            double cosPhi, double sinPhi, double angle,
            out double x, out double y, out double dx, out double dy)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            x = centreX + rx * cosAngle * cosPhi - ry * sinAngle * sinPhi;
            y = centreY + rx * cosAngle * sinPhi + ry * sinAngle * cosPhi;
            dx = -rx * sinAngle * cosPhi - ry * cosAngle * sinPhi;
            dy = -rx * sinAngle * sinPhi + ry * cosAngle * cosPhi;
        }

        private static double Angle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double lengths = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));

            if (lengths <= 0)
            {
                return 0;
            }

            double cosine = dot / lengths;

            if (cosine < -1)
            {
                cosine = -1;
            }
            else if (cosine > 1)
            {
                cosine = 1;
            }

            double angle = Math.Acos(cosine);

            return ux * vy - uy * vx < 0 ? -angle : angle;
        }

        private sealed class PathBuilder
        {
            private readonly List<PathVerb> _verbs = new List<PathVerb>();
            private readonly List<float> _points = new List<float>();

            private float _left = float.MaxValue;
            private float _top = float.MaxValue;
            private float _right = float.MinValue;
            private float _bottom = float.MinValue;

            private float _x;
            private float _y;
            private bool _open;

            internal bool HasContent => _verbs.Count > 0;

            internal void Move(float x, float y)
            {
                _verbs.Add(PathVerb.Move);
                _points.Add(x);
                _points.Add(y);

                IncludeAxis(x, true);
                IncludeAxis(y, false);

                _x = x;
                _y = y;
                _open = true;
            }

            internal void Line(float x, float y)
            {
                if (!_open)
                {
                    Move(x, y);

                    return;
                }

                _verbs.Add(PathVerb.Line);
                _points.Add(x);
                _points.Add(y);

                IncludeAxis(x, true);
                IncludeAxis(y, false);

                _x = x;
                _y = y;
            }

            internal void Quad(float controlX, float controlY, float x, float y)
            {
                if (!_open)
                {
                    Move(_x, _y);
                }

                _verbs.Add(PathVerb.Quad);
                _points.Add(controlX);
                _points.Add(controlY);
                _points.Add(x);
                _points.Add(y);

                IncludeQuad(_x, controlX, x, true);
                IncludeQuad(_y, controlY, y, false);

                _x = x;
                _y = y;
            }

            internal void Cubic(float firstX, float firstY, float secondX, float secondY,
                float x, float y)
            {
                if (!_open)
                {
                    Move(_x, _y);
                }

                _verbs.Add(PathVerb.Cubic);
                _points.Add(firstX);
                _points.Add(firstY);
                _points.Add(secondX);
                _points.Add(secondY);
                _points.Add(x);
                _points.Add(y);

                IncludeCubic(_x, firstX, secondX, x, true);
                IncludeCubic(_y, firstY, secondY, y, false);

                _x = x;
                _y = y;
            }

            internal void Close()
            {
                if (!_open)
                {
                    return;
                }

                _verbs.Add(PathVerb.Close);
                _open = false;
            }

            internal PathGeometry Build(bool evenOdd)
            {
                if (_verbs.Count == 0)
                {
                    return null;
                }

                return new PathGeometry(_verbs.ToArray(), _points.ToArray(), evenOdd,
                    _left, _top, _right, _bottom);
            }

            private void IncludeAxis(float value, bool horizontal)
            {
                if (horizontal)
                {
                    if (value < _left)
                    {
                        _left = value;
                    }

                    if (value > _right)
                    {
                        _right = value;
                    }

                    return;
                }

                if (value < _top)
                {
                    _top = value;
                }

                if (value > _bottom)
                {
                    _bottom = value;
                }
            }

            private void IncludeQuad(float from, float control, float to, bool horizontal)
            {
                IncludeAxis(to, horizontal);

                float denominator = from - 2 * control + to;

                if (Math.Abs(denominator) < EPSILON)
                {
                    return;
                }

                float t = (from - control) / denominator;

                if (t <= 0 || t >= 1)
                {
                    return;
                }

                float inverse = 1 - t;

                IncludeAxis(inverse * inverse * from + 2 * inverse * t * control + t * t * to,
                    horizontal);
            }

            private void IncludeCubic(float from, float first, float second, float to,
                bool horizontal)
            {
                IncludeAxis(to, horizontal);

                float a = -from + 3 * first - 3 * second + to;
                float b = 2 * (from - 2 * first + second);
                float c = first - from;

                if (Math.Abs(a) < EPSILON)
                {
                    if (Math.Abs(b) < EPSILON)
                    {
                        return;
                    }

                    IncludeCubicAt(from, first, second, to, -c / b, horizontal);

                    return;
                }

                float discriminant = b * b - 4 * a * c;

                if (discriminant < 0)
                {
                    return;
                }

                float root = (float)Math.Sqrt(discriminant);

                IncludeCubicAt(from, first, second, to, (-b + root) / (2 * a), horizontal);
                IncludeCubicAt(from, first, second, to, (-b - root) / (2 * a), horizontal);
            }

            private void IncludeCubicAt(float from, float first, float second, float to,
                float t, bool horizontal)
            {
                if (t <= 0 || t >= 1)
                {
                    return;
                }

                float inverse = 1 - t;

                IncludeAxis(inverse * inverse * inverse * from
                    + 3 * inverse * inverse * t * first
                    + 3 * inverse * t * t * second
                    + t * t * t * to, horizontal);
            }
        }
    }
}
