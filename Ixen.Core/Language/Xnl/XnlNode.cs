using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NameIndex { get; set; }
        public string Type { get; set; }
        public int TypeIndex { get; set; }
        public string Code { get; set; }
        public int CodeIndex { get; set; }
        public bool IsRegion => Code != null;
        public List<XnlNodeParameter> Properties { get; set; } = new();
        public XnlNode Parent { get; set; }
        public List<XnlNode> Children { get; set; } = new();
    }
}
