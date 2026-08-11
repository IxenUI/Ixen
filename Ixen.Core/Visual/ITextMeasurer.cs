namespace Ixen.Core.Visual
{
    internal interface ITextMeasurer
    {
        void MeasureText(string text, FontSpec font, out float width, out float height);
        float GetLineHeight(FontSpec font);
    }
}
