using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Media { get; set; }
        public string Mixin { get; set; }
        public int NameIndex { get; set; }
        public List<XnsStyle> Styles { get; set; } = new();
        public List<XnsVariable> Variables { get; set; } = new();
        public XnsNode Parent { get; set; }
        public List<XnsNode> Children { get; set; } = new();
    }
}
