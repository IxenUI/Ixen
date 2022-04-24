using System;

namespace Ixen.Core.Visual.Exceptions
{
    public class VisualException : IxenCoreException
    {
        public VisualException()
        { }

        public VisualException(string message)
            : base(message)
        { }

        public VisualException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public VisualException(string message, Exception inner)
            : base(message, inner)
        { }

        public VisualException(Exception inner)
            : base(null, inner)
        { }
    }
}
