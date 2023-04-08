using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlNode
    {
        public int LineNum { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<XnxNodeParameter> Parameters { get; set; } = new();
        public XnlNode Parent { get; set; }
        public List<XnlNode> Children { get; set; } = new();
    }
}
