using System;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal static class XnlEvents
    {
        private static readonly Dictionary<string, string> _aliases
            = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "click", "PointerClick" },
                { "double-click", "PointerDoubleClick" },
                { "long-press", "PointerLongPress" },
                { "wheel", "PointerWheel" },
                { "drag", "PointerDrag" },
                { "drag-start", "PointerDragStart" },
                { "drag-end", "PointerDragEnd" },
                { "pinch", "PointerPinch" },
                { "pinch-start", "PointerPinchStart" },
                { "pinch-end", "PointerPinchEnd" }
            };

        internal static IReadOnlyCollection<string> Aliases => _aliases.Keys;

        internal static string Resolve(string xnlName, string pascalName)
        {
            if (xnlName != null && _aliases.TryGetValue(xnlName, out string name))
            {
                return name;
            }

            return pascalName;
        }
    }
}
