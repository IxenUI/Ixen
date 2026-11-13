using System.Globalization;

namespace Ixen.Core
{
    public sealed class WindowPlacement
    {
        public WindowPlacement(int x, int y, int width, int height, bool maximized)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Maximized = maximized;
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public bool Maximized { get; }

        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4}",
                X, Y, Width, Height, Maximized ? "maximized" : "normal");

        public static WindowPlacement Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string[] parts = text.Trim().Split(' ');

            if (parts.Length != 5
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height)
                || width <= 0
                || height <= 0
                || (parts[4] != "maximized" && parts[4] != "normal"))
            {
                return null;
            }

            return new WindowPlacement(x, y, width, height, parts[4] == "maximized");
        }
    }
}
