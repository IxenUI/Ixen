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
                        node.NameIndex = token.Index;
                        break;

                    case XnlTokenType.ElementTypeName:
                        if (createNode)
                        {
                            node = CreateNode(++nodeId, parent);
                            createNode = false;
                        }
                        node.Type = token.Content;
                        node.TypeIndex = token.Index;
                        break;

                    case XnlTokenType.PropertyName:
                        nodeParameter = new XnlNodeParameter
                        {
                            Name = token.Content,
                            NameIndex = token.Index
                        };
                        break;

                    case XnlTokenType.PropertyValue:
                        nodeParameter.Value = token.Content;
                        nodeParameter.ValueIndex = token.Index;
                        node.Properties.Add(nodeParameter);
                        break;

                    case XnlTokenType.ChildrenBegin:
                        stack.Push(node);
                        parent = node;
                        break;

                    case XnlTokenType.ChildrenEnd:
                        parent = stack.Pop().Parent;
                        break;

                    case XnlTokenType.CodeRegionBegin:
                        node = CreateNode(++nodeId, parent);
                        node.Code = token.Content;
                        node.CodeIndex = token.Index;
                        stack.Push(node);
                        parent = node;
                        createNode = true;
                        break;

                    case XnlTokenType.CodeRegionEnd:
                        parent = stack.Pop().Parent;
                        createNode = true;
                        break;
                }
            }

            return root;
        }
    }
}
