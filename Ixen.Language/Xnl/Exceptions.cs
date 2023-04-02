using System;

namespace Ixen.Language.Xnl.Parser
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
}
