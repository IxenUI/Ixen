using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsNode
    {
        public string Name { get; set; }
        public List<XnsStyle> Styles { get; set; } = new();
        public XnsNode Parent { get; set; }
        public List<XnsNode> Children { get; set; } = new();
    }
}
