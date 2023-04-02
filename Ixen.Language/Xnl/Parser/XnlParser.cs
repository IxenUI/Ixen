﻿using Ixen.Core.Visual;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ixen.Language.Xnl.Parser
{
    internal class XnlParser
    {
        private string[] _inputLines;

        private int _lineNum = 0;
        private int _nextLineNum = 0;
        private int _lineIndex = 0;
        private int _nextLineIndex = 0;

        private bool _expectElementName = false;

        private bool _expectElementType = false;
        private bool _expectElementTypeEqual = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectParamsBegin = false;
        private bool _expectParamsEnd = false;
        private bool _expectParamName = false;
        private bool _expectParamEqual = false;
        private bool _expectParamValueBegin = false;
        private bool _expectParamValue = false;

        private bool _identifier;
        private bool _content;

        private XnlParser(string[] lines)
        {
            _inputLines = lines;
        }

        public static XnlNode ParseFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            return new XnlParser(lines).Parse();
        }

        public static XnlNode Parse(string source)
        {
            string[] lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return new XnlParser(lines).Parse();
        }

        private XnlNode Parse()
        {
            try
            {
                var tree = ReadNodes();
                return tree;
            }
            catch (XnlException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new XnlParseException();
            }
        }

        private XnlNode ReadNodes()
        {
            char c;
            StringBuilder sb;
            XnlNodeParameter nodeParameter = null;

            XnlNode node = new XnlNode();
            var root = new XnlNode();
            var parent = root;
            var stack = new Stack<XnlNode>();
            stack.Push(root);

            sb = new StringBuilder();

            _expectElementName = true;

            while ((c = PeekChar()) != '\0')
            {
                if (_identifier)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectElementName)
                        {
                            ResetStatesFlags();
                            _expectElementTypeEqual = true;
                            node = new XnlNode();
                            node.Parent = parent;
                            node.LineNum = _nextLineNum;
                            node.Index = _nextLineIndex;
                            node.Name = sb.ToString();
                            parent.Children.Add(node);
                            sb.Clear();
                            continue;
                        }

                        if (_expectElementType)
                        {
                            ResetStatesFlags();
                            _expectParamsBegin = true;
                            _expectContentBegin = true;
                            node.Type = sb.ToString();
                            sb.Clear();
                            continue;
                        }

                        if (_expectParamName)
                        {
                            ResetStatesFlags();
                            _expectParamEqual = true;
                            nodeParameter = new XnlNodeParameter
                            {
                                Name = sb.ToString()
                            };
                            sb.Clear();
                            continue;
                        }
                    }
                }

                if (_content)
                {
                    if (c != '"')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectParamValue)
                        {
                            ResetStatesFlags();
                            _expectParamName = true;
                            _expectParamsEnd = true;
                            nodeParameter.Value = sb.ToString();
                            node.Parameters.Add(nodeParameter);
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
                        if (!_expectElementTypeEqual)
                        {
                            throw new XnlUnexpectedCharacterException(":", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectElementType = true;
                        _expectContentBegin = true;
                        MoveCursor();
                        break;

                    case '=':
                        if (!_expectParamEqual)
                        {
                            throw new XnlUnexpectedCharacterException("=", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectParamValueBegin = true;
                        MoveCursor();
                        break;

                    case '(':
                        if (!_expectParamsBegin)
                        {
                            throw new XnlUnexpectedCharacterException("(", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectParamName = true;
                        MoveCursor();
                        break;

                    case ')':
                        if (!_expectParamsEnd)
                        {
                            throw new XnlUnexpectedCharacterException(")", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectElementName = true;
                        _expectContentBegin = true;
                        _expectContentEnd = true;
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectContentBegin)
                        {
                            throw new XnlUnexpectedCharacterException("{", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();

                        stack.Push(node);
                        parent = node;

                        _expectElementName = true;
                        _expectContentEnd = true;
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectContentEnd)
                        {
                            throw new XnlUnexpectedCharacterException("}", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();

                        parent = stack.Pop().Parent;

                        _expectElementName = true;
                        _expectContentEnd = true;
                        MoveCursor();
                        break;

                    case '"':
                        if (!_expectParamValueBegin)
                        {
                            throw new XnlUnexpectedCharacterException("\"", _nextLineNum, _nextLineIndex);
                        }

                        ResetStatesFlags();
                        _expectParamValue = true;
                        MoveCursor();
                        break;

                    default:
                        if (_expectElementName || _expectElementType || _expectParamName)
                        {
                            _identifier = true;
                            continue;
                        }

                        if (_expectParamValue)
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
            _expectElementType = false;
            _expectElementTypeEqual = false;
            _expectContentBegin = false;
            _expectContentEnd = false;
            _expectParamsBegin = false;
            _expectParamsEnd = false;
            _expectParamName = false;
            _expectParamEqual = false;
            _expectParamValueBegin = false;
            _expectParamValue = false;

            _identifier = false;
            _content = false;
        }

        private char PeekChar()
        {
            _nextLineNum = _lineNum;
            _nextLineIndex = _lineIndex + 1;

            while (_nextLineIndex >= _inputLines[_nextLineNum].Length)
            {
                _nextLineIndex = 0;
                _nextLineNum++;
                if (_nextLineNum >= _inputLines.Length)
                {
                    return '\0';
                }
            }

            return _inputLines[_nextLineNum][_nextLineIndex];
        }

        private void MoveCursor()
        {
            _lineNum = _nextLineNum;
            _lineIndex = _nextLineIndex;
        }
    }
}
