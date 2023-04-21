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

        private XnlNode CreateNode(int id, XnlNode parent)
        {
            var node = new XnlNode
            {
                Id = id,
                Parent = parent
            };

            parent.Children.Add(node);

            return node;
        }

        private XnlNode ReadNodes(List<XnlToken> tokens)
        {
            int nodeId = 0;
            bool createNode = true;
            var root = new XnlNode { Id = nodeId };
            var parent = root;
            var stack = new Stack<XnlNode>();
            XnlNode node = null;
            XnlNodeParameter nodeParameter = null;

            stack.Push(root);

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case XnlTokenType.PropertiesBegin:
                        if (createNode)
                        {
                            node = CreateNode(++nodeId, parent);
                            createNode = false;
                        }
                        break;

                    case XnlTokenType.PropertiesEnd:
                        createNode = true;
                        break;

                    case XnlTokenType.ElementName:
                        if (createNode)
                        {
                            node = CreateNode(++nodeId, parent);
                            createNode = false;
                        }
                        node.Name = token.Content;
                        break;

                    case XnlTokenType.ElementTypeName:
                        if (createNode)
                        {
                            node = CreateNode(++nodeId, parent);
                            createNode = false;
                        }
                        node.Type = token.Content;
                        break;

                    case XnlTokenType.PropertyName:
                        nodeParameter = new XnlNodeParameter
                        {
                            Name = token.Content
                        };
                        break;

                    case XnlTokenType.PropertyValue:
                        nodeParameter.Value = token.Content;
                        node.Properties.Add(nodeParameter);
                        break;

                    case XnlTokenType.ChildrenBegin:
                        stack.Push(node);
                        parent = node;
                        break;

                    case XnlTokenType.ChildrenEnd:
                        parent = stack.Pop().Parent;
                        break;
                }
            }

            return root;
        }
    }
}
