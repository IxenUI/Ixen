namespace Ixen.Core.Visual
{
    internal interface IImageMeasurer
    {
        bool TryMeasure(string source, out float width, out float height);
    }
}
