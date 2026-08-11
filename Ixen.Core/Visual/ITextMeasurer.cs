namespace Ixen.Core.Visual
{
    internal interface ITextMeasurer
    {
        void MeasureText(string text, string fontFamily, float fontSize, out float width, out float height);
        float GetLineHeight(string fontFamily, float fontSize);
    }
}
