using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsNodifier
    {
        public XnsNode Nodify(List<XnsToken> tokens)
        {
            try
            {
                return ReadNodes(tokens);
            }
            catch
            {
                return null;
            }
        }

        private XnsNode ReadNodes(List<XnsToken> tokens)
        {
            int nodeId = 0;
            var root = new XnsNode { Id = nodeId };
            var parent = root;
            var stack = new Stack<XnsNode>();

            XnsNode node = null;
            XnsStyle style = null;

            stack.Push(root);

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case XnsTokenType.ClassName:
                        node = new XnsNode()
                        {
                            Id = ++nodeId,
                            Parent = parent,
                            Name = token.Content
                        };
                        parent.Children.Add(node);
                        break;

                    case XnsTokenType.BeginClassContent:
                        stack.Push(node);
                        parent = node;
                        break;

                    case XnsTokenType.EndClassContent:
                        parent = stack.Pop().Parent;
                        break;

                    case XnsTokenType.StyleName:
                        style = new XnsStyle
                        {
                            Name = token.Content
                        };
                        break;

                    case XnsTokenType.StyleValue:
                    case XnsTokenType.StyleSizeValue:
                    case XnsTokenType.StyleColorValue:
                        style.Value = token.Content;
                        node.Styles.Add(style);
                        break;
                }
            }

            return root;
        }
    }
}
