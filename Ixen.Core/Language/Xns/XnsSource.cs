using Ixen.Core.Language.Base;
using Ixen.Core.Visual.Classes;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsSource : BaseSource
    {
        private XnsTokenizer _tokenizer;
        private XnsNodifier _nodifier = new();
        private XnsCompiler _compiler = new();

        private List<XnsToken> _tokens;
        private XnsNode _node;
        private ClassesSet _classesSet;       

        public XnsSource(string source)
            : base(source)
        {
            _tokenizer = new XnsTokenizer(_source);
        }

        public List<XnsToken> Tokenize()
        {
            _tokens = _tokenizer.Tokenize();

            if (_tokenizer.HasErrors)
            {
                HasErrors = true;
            }

            return _tokens;
        }

        public XnsNode Nodify()
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

            return _node;
        }

        public ClassesSet Compile()
        {
            if (_node == null)
            {
                Nodify();
            }

            if (HasErrors)
            {
                return null;
            }

            _classesSet = _compiler.Compile(_node);

            return _classesSet;
        }

        public IEnumerable<XnsToken> GetTokens() => _tokenizer.GetTokens();
        public IEnumerable<XnsToken> GetTokens(int indexFrom, int indexTo) => _tokenizer.GetTokens(indexFrom, indexTo);
    }
}
