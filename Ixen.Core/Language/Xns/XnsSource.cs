using Ixen.Core.Language.Base;
using Ixen.Core.Visual.Classes;
using System.Collections.Generic;
using System.Linq;

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

        private XnsSource(List<string> sourceLines)
            : base(sourceLines)
        {
            _tokenizer = new XnsTokenizer(_inputLines);
        }

        private XnsSource(string filePath, string source)
            : base (filePath, source)
        {
            _tokenizer = new XnsTokenizer(_inputLines);
        }

        public static XnsSource FromFile(string filePath)
        {
            return new XnsSource(filePath, null);
        }

        public static XnsSource FromSource(string source)
        {
            return new XnsSource(null, source);
        }

        public static XnsSource FromSourceLines(IEnumerable<string> lines)
        {
            return new XnsSource(lines.ToList());
        }

        public override bool UpdateLine(int lineNum, string line, int totalLines)
        {
            if (base.UpdateLine(lineNum, line, totalLines))
            {
                _tokenizer.UpdateLine(lineNum);

                return true;
            }

            return false;
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

        public List<XnsToken> GetTokens() => _tokens;
        public List<XnsToken> GetTokensOfLine(int lineNum) => _tokenizer.GetTokensOfLine(lineNum);
    }
}
