using SkiaSharp;

namespace Ixen.Core.Rendering
{
    internal readonly struct FontRun
    {
        internal FontRun(int start, int length, SKFont font)
        {
            Start = start;
            Length = length;
            Font = font;
        }

        internal int Start { get; }

        internal int Length { get; }

        internal SKFont Font { get; }
    }
}
