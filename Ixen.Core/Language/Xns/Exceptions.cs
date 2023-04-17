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
}
