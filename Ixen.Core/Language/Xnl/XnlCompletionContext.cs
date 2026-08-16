using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlCompletionContext
    {
        internal static readonly XnlCompletionContext None
            = new XnlCompletionContext(XnlCompletionKind.None, null, null, 0, 0, new string[0]);

        internal XnlCompletionKind Kind { get; }
        internal string TypeName { get; }
        internal string PropertyName { get; }
        internal int SpanStart { get; }
        internal int SpanLength { get; }
        internal IReadOnlyList<string> Items { get; }

        internal XnlCompletionContext(XnlCompletionKind kind, string typeName, string propertyName,
            int spanStart, int spanLength, IReadOnlyList<string> items)
        {
            Kind = kind;
            TypeName = typeName;
            PropertyName = propertyName;
            SpanStart = spanStart;
            SpanLength = spanLength;
            Items = items;
        }
    }
}
