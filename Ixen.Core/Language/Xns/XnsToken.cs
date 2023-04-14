﻿namespace Ixen.Core.Language.Xns
{
    internal class XnsToken
    {
        public int LineNum { get; set; }
        public int LineIndex { get; set; }
        public XnsTokenType Type { get; set; }
        public bool IsError => Type == XnsTokenType.Error;
        public string Content { get; set; }
        public string Message { get; set; }
        public XnsTokenErrorType ErrorType { get; set; }
    }
}
