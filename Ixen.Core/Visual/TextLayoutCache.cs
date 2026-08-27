namespace Ixen.Core.Visual
{
    internal class TextLayoutCache
    {
        private ITextMeasurer _measurer;
        private string _text;
        private FontSpec _font;
        private bool _wrap;
        private bool _ellipsis;
        private float _availableWidth;
        private bool _valid;

        internal float Width { get; private set; }
        internal float Height { get; private set; }

        internal bool Matches(ITextMeasurer measurer, string text, FontSpec font, bool wrap, bool ellipsis,
            float availableWidth)
        {
            return _valid
                && _measurer == measurer
                && _wrap == wrap
                && _ellipsis == ellipsis
                && _availableWidth == availableWidth
                && string.Equals(_text, text)
                && _font.SameAs(font);
        }

        internal void Set(ITextMeasurer measurer, string text, FontSpec font, bool wrap, bool ellipsis,
            float availableWidth, float width, float height)
        {
            _measurer = measurer;
            _text = text;
            _font = font;
            _wrap = wrap;
            _ellipsis = ellipsis;
            _availableWidth = availableWidth;

            Width = width;
            Height = height;

            _valid = true;
        }

        internal void Reset()
        {
            _valid = false;
            _measurer = null;
            _text = null;
        }
    }
}
