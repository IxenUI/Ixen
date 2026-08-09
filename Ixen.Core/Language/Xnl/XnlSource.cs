using Ixen.Core.Language.Base;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlSource : BaseSource
    {
        private XnlTokenizer _tokenizer;
        private XnlNodifier _nodifier = new();

        private List<XnlToken> _tokens;
        private XnlNode _node;

        public XnlSource(string source)
            : base(source)
        {
            _tokenizer = new XnlTokenizer(_source);
        }

        public List<XnlToken> Tokenize()
        {
            _diagnostics.Clear();
            _tokens = _tokenizer.Tokenize();
            _diagnostics.AddRange(_tokenizer.Diagnostics);

            return _tokens;
        }

        public XnlNode Nodify()
        {
            if (_tokens == null)
            {
                Tokenize();
            }

            if (HasErrors)
            {
                return null;
            }

            _node = _nodifier.Nodify(_tokens);

            if (_node == null)
            {
                AddError(LanguageErrorCode.STRUCTURE, "Could not build the element tree from this file.", 0, 0);
            }

            return _node;
        }

        public IEnumerable<XnlToken> GetTokens() => _tokenizer.GetTokens();
        public IEnumerable<XnlToken> GetTokens(int indexFrom, int indexTo) => _tokenizer.GetTokens(indexFrom, indexTo);
    }
}
