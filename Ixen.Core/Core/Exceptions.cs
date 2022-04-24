using System;

namespace Ixen.Core
{
    public class IxenException : Exception
    {
        public IxenException()
        { }

        public IxenException(string message)
            : base(message)
        { }

        public IxenException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public IxenException(string message, Exception inner)
            : base(message, inner)
        { }

        public IxenException(Exception inner)
            : base(null, inner)
        { }
    }

    public class IxenCoreException : Exception
    {
        public IxenCoreException()
        { }

        public IxenCoreException(string message)
            : base(message)
        { }

        public IxenCoreException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public IxenCoreException(string message, Exception inner)
            : base(message, inner)
        { }

        public IxenCoreException(Exception inner)
            : base(null, inner)
        { }
    }
}
