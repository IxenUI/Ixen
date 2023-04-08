using System;

namespace Ixen.Core.Language
{
    internal class XnsException : Exception
    {
        public XnsException()
        { }

        public XnsException(string message)
            : base(message)
        { }

        public XnsException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public XnsException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    internal class XnsParseException : XnsException
    {
        public XnsParseException()
        { }

        public XnsParseException(string message)
            : base(message)
        { }

        public XnsParseException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public XnsParseException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    internal class XnsUnexpectedCharacterException : XnsParseException
    {
        public XnsUnexpectedCharacterException()
        { }

        public XnsUnexpectedCharacterException(string gotChar, int lineNum, int lineIndex)
            : base($"Unexpected {gotChar} character at line {lineNum} index {lineIndex}")
        { }

        public XnsUnexpectedCharacterException(string expectedChar, string gotChar, int lineNum, int lineIndex)
            : base($"Expected {expectedChar} character, got {gotChar} at line {lineNum} index {lineIndex}")
        { }
    }
}
