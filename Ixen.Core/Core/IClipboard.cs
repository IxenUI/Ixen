namespace Ixen.Core
{
    public interface IClipboard
    {
        string GetText();
        void SetText(string text);
    }
}
