﻿using System;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlException : Exception
    {
        public XnlException()
        { }

        public XnlException(string message)
            : base(message)
        { }

        public XnlException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public XnlException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    internal class XnlParseException : XnlException
    {
        public XnlParseException()
        { }

        public XnlParseException(string message)
            : base(message)
        { }

        public XnlParseException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public XnlParseException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    internal class XnlUnexpectedCharacterException : XnlParseException
    {
        public XnlUnexpectedCharacterException()
        { }

        public XnlUnexpectedCharacterException(string gotChar, int lineNum, int lineIndex)
            : base($"Unexpected {gotChar} character at line {lineNum} index {lineIndex}")
        { }

        public XnlUnexpectedCharacterException(string expectedChar, string gotChar, int lineNum, int lineIndex)
            : base($"Expected {expectedChar} character, got {gotChar} at line {lineNum} index {lineIndex}")
        { }
    }
}
