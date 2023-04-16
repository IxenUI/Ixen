using Ixen.Core.Language.Base;
using System;
using System.Linq;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlSource : BaseSource
    {
        private XnlParser _parser;
        private XnlNode _content;

        public XnlSource(string source)
            : base(source)
        {
            _parser = new XnlParser(source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList());
        }

        public void Parse()
        {
            _content = _parser.Parse();
        }

        public XnlNode GetContent() => _content;
    }
}
