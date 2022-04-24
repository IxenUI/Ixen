using System;

namespace Ixen.Core.Rendering
{
    public class RenderingException : IxenCoreException
    {
        public RenderingException()
        { }

        public RenderingException(string message)
            : base(message)
        { }

        public RenderingException(string message, params object[] obj)
            : base(string.Format(message, obj))
        { }

        public RenderingException(string message, Exception inner)
            : base(message, inner)
        { }

        public RenderingException(Exception inner)
            : base(null, inner)
        { }
    }
}
