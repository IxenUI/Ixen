namespace Ixen.Core.Language.Xns
{
    internal class XnsToken
    {
        public int Index { get; set; }
        public XnsTokenType Type { get; set; }
        public bool IsError => Type == XnsTokenType.Error;
        public string Content { get; set; }
        public string Message { get; set; }
        public XnsTokenErrorType ErrorType { get; set; }
    }
}
