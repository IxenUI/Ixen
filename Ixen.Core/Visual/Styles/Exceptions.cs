using System;

namespace Ixen.Core.Visual.Styles.Exceptions
{
    public class StyleException : IxenCoreException
    {
        public StyleException()
        { }

        public StyleException(string message)
            : base(message)
        { }

        public StyleException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public StyleException(string message, Exception inner)
            : base(message, inner)
        { }

        public StyleException(Exception inner)
            : base(null, inner)
        { }
    }
}
