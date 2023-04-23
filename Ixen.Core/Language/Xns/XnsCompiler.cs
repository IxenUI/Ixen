using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsCompiler
    {
        public XnsCompiler()
        { }

        public ClassesSet Compile(XnsNode node)
        {
            try
            {
                var set = new ClassesSet();
                set.Classes = new List<StyleClass>();

                AddClass(node, set.Classes);

                return set;
            }
            catch
            {
                return null;
            }
        }

        private string GetScope(XnsNode node)
        {
            var sb = new StringBuilder();

            while (node.Parent != null)
            {
                if (!string.IsNullOrEmpty(node.Parent.Name))
                {
                    sb.Insert(0, node.Parent.Name);
                }

                node = node.Parent;
            }

            return sb.Length > 0
                ? sb.ToString()
                : null;
        }

        private StyleClass GetClass(XnsNode node)
        {
            string name = node.Name;
            var target = StyleClassTarget.ElementName;

            if (name.StartsWith("."))
            {
                target = StyleClassTarget.ClassName;
                name = name.Substring(1);
            }
            else if (name.StartsWith("#"))
            {
                target = StyleClassTarget.ElementType;
                name = name.Substring(1);
            }

            return new StyleClass(target, GetScope(node), name, ToStyles(node));
        }

        private void AddClass(XnsNode node, List<StyleClass> list)
        {
            if (node.Styles.Count > 0)
            {
                list.Add(GetClass(node));
            }

            foreach (var child in node.Children)
            {
                AddClass(child, list);
            }
        }

        private List<StyleDescriptor> ToStyles(XnsNode xnsNode)
        {
            var styles = new List<StyleDescriptor>();

            foreach (var xnsStyle in xnsNode.Styles)
            {
                styles.Add(ToStyleDescriptor(xnsStyle));
            }

            return styles;
        }

        private StyleDescriptor ToStyleDescriptor(XnsStyle xnsStyle)
        {
            switch (xnsStyle.Name.ToLower())
            {
                case StyleIdentifier.Background:
                    return new BackgroundStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Border:
                    return new BorderStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.ColumnTemplate:
                    return new ColumnTemplateStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Height:
                    return new HeightStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Layout:
                    return new LayoutStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Margin:
                    return new MarginStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Mask:
                    return new MaskStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Padding:
                    return new PaddingStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.RowTemplate:
                    return new RowTemplateStyleParser(xnsStyle.Value).Descriptor;

                case StyleIdentifier.Width:
                    return new WidthStyleParser(xnsStyle.Value).Descriptor;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
