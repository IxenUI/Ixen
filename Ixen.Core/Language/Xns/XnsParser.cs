using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ixen.Core.Language.Base;

namespace Ixen.Core.Language.Xns
{
    internal class XnsParser : BaseParser
    {
        private bool _expectElementName = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectStyleName = false;
        private bool _expectStyleEqual = false;
        private bool _expectStyleValue = false;

        private bool _identifier;
        private bool _content;

        public XnsParser(string[] lines)
            : base(lines)
        { }

        public XnsNode Parse()
        {
            try
            {
                var tree = ReadNodes();
                return tree;
            }
            catch (XnsException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new XnsParseException();
            }
        }

        private XnsNode ReadNodes()
        {
            char c;
            char c2;
            StringBuilder sb;
            XnsStyle style = null;

            XnsNode node = new XnsNode();
            var root = new XnsNode();
            var parent = root;
            var stack = new Stack<XnsNode>();
            stack.Push(root);

            sb = new StringBuilder();

            _expectElementName = true;

            while ((c = PeekChar()) != '\0')
            {
                if (_identifier)
                {
                    if (char.IsLetterOrDigit(c) || _expectElementName && (c == '_' || c == '.'))
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        c2 = PeekNonSpaceChar();
                        if (_expectElementName && c2 == '{')
                        {
                            ResetStatesFlags();
                            _expectContentBegin = true;
                            node = new XnsNode();
                            node.Parent = parent;
                            node.LineNum = _nextLineNum;
                            node.Index = _nextLineIndex;
                            node.Name = sb.ToString();
                            parent.Children.Add(node);
                            sb.Clear();
                            continue;
                        }

                        if (_expectStyleName && c2 == ':')
                        {
                            ResetStatesFlags();
                            _expectStyleEqual = true;
                            style = new XnsStyle
                            {
                                Name = sb.ToString().ToLower()
                            };
                            sb.Clear();
                            continue;
                        }
                    }
                }

                if (_content)
                {
                    if (!_isNewLine)
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectStyleValue)
                        {
                            ResetStatesFlags();
                            _expectStyleName = true;
                            _expectElementName = true;
                            _expectContentEnd = true;
                            style.Value = sb.ToString();
                            node.Styles.Add(style);
                            sb.Clear();
                            MoveCursor();
                            continue;
                        }
                    }
                }

                switch (c)
                {
                    case ' ':
                    case '\t':
                        MoveCursor();
                        break;

                    case ':':
                        if (!_expectStyleEqual)
                        {
                            throw new XnsUnexpectedCharacterException(":", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectStyleValue = true;
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectContentBegin)
                        {
                            throw new XnsUnexpectedCharacterException("{", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();

                        stack.Push(node);
                        parent = node;

                        _expectStyleName = true;
                        _expectContentEnd = true;
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectContentEnd)
                        {
                            throw new XnsUnexpectedCharacterException("}", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();

                        parent = stack.Pop().Parent;

                        _expectElementName = true;
                        _expectContentEnd = true;
                        MoveCursor();
                        break;

                    default:
                        if (_expectElementName || _expectStyleName)
                        {
                            _identifier = true;
                            continue;
                        }

                        if (_expectStyleValue)
                        {
                            _content = true;
                            continue;
                        }

                        break;
                }
            }

            return root;
        }

        private void ResetStatesFlags()
        {
            _expectElementName = false;

            _expectContentBegin = false;
            _expectContentEnd = false;
            _expectStyleName = false;
            _expectStyleEqual = false;
            _expectStyleValue = false;

            _identifier = false;
            _content = false;
        }
    }
}
