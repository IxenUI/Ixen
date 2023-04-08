using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsNode
    {
        public int LineNum { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public List<XnsStyle> Styles { get; set; } = new();
        public XnsNode Parent { get; set; }
        public List<XnsNode> Children { get; set; } = new();
    }
}
