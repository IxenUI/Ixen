using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlNodifier
    {
        public XnlNode Nodify(List<XnlToken> tokens)
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

        private XnlNode ReadNodes(List<XnlToken> tokens)
        {
            var node = new XnlNode();
            var root = new XnlNode();
            var parent = root;
            var stack = new Stack<XnlNode>();
            XnlNodeParameter nodeParameter = null;

            stack.Push(root);

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case XnlTokenType.ElementName:
                        node = new XnlNode()
                        {
                            Parent = parent,
                            Name = token.Content
                        };
                        parent.Children.Add(node);
                        break;

                    case XnlTokenType.ElementTypeName:
                        node.Type = token.Content;
                        break;

                    case XnlTokenType.ParamName:
                        nodeParameter = new XnlNodeParameter
                        {
                            Name = token.Content
                        };
                        break;

                    case XnlTokenType.ParamValue:
                        nodeParameter.Value = token.Content;
                        node.Parameters.Add(nodeParameter);
                        break;

                    case XnlTokenType.BeginContent:
                        stack.Push(node);
                        parent = node;
                        break;

                    case XnlTokenType.EndContent:
                        parent = stack.Pop().Parent;
                        break;
                }
            }

            return root;
        }
    }
}
