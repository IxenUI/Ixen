namespace Ixen.Core.Language.Xnl
{
    internal class XnlToken
    {
        public int Index { get; set; }
        public XnlTokenType Type { get; set; }
        public bool IsError => Type == XnlTokenType.Error;
        public string Content { get; set; }
        public string Message { get; set; }
        public XnlTokenErrorType ErrorType { get; set; }
    }
}
