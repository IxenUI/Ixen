using Ixen.Core.Language.Base;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlSource : BaseSource
    {
        private XnlParser _parser;
        private XnlNode _content;
        private XnlSource(List<string> sourceLines)
            : base(sourceLines)
        {
            _parser = new XnlParser(_inputLines);
        }

        private XnlSource(string filePath, string source)
            : base(filePath, source)
        {
            _parser = new XnlParser(_inputLines);
        }

        public static XnlSource FromFile(string filePath)
        {
            return new XnlSource(filePath, null);
        }

        public static XnlSource FromSource(string source)
        {
            return new XnlSource(null, source);
        }

        public static XnlSource FromSourceLines(IEnumerable<string> lines)
        {
            return new XnlSource(lines.ToList());
        }

        public void Parse()
        {
            _content = _parser.Parse();
        }

        public XnlNode GetContent() => _content;
    }
}
