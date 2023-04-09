using Ixen.Core.Language.Base;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsSource : BaseSource
    {
        private XnsParser _parser;
        private XnsNode _content;
        private ClassesSet _set;

        private XnsSource(string filePath, string source)
            : base (filePath, source)
        {
            _parser = new XnsParser(_inputLines);
        }

        public static XnsSource FromFile(string filePath)
        {
            return new XnsSource(filePath, null);
        }

        public static XnsSource FromSource(string source)
        {
            return new XnsSource(null, source);
        }

        public void Parse()
        {
            if (IsParsed)
            {
                return;
            }

            _content = _parser.Parse();

            IsParsed = true;
        }

        public XnsNode GetContent() => _content;

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
            var target = StyleClassTarget.Any;

            if (name.StartsWith("."))
            {
                target = StyleClassTarget.ElementName;
                name = name.Substring(1);
            }
            else if (name.StartsWith("#"))
            {
                target = StyleClassTarget.ElementType;
                name = name.Substring(1);
            }

            return new StyleClass(target, GetScope(node), name, ToStyles(node));
        }

        public ClassesSet ToClassesSet()
        {
            if (!IsLoaded || !IsParsed)
            {
                return null;
            }

            _set = new ClassesSet();
            _set.Classes = new List<StyleClass>();

            AddClass(_content, _set.Classes);

            return _set;
        }

        private void AddClass(XnsNode node, List<StyleClass> list)
        {
            List<Style> styles = ToStyles(node);


            if (styles.Count > 0)
            {
                list.Add(GetClass(node));
            }

            foreach (var child in node.Children)
            {
                AddClass(child, list);
            }
        }

        private List<Style> ToStyles(XnsNode xnsNode)
        {
            var styles = new List<Style>();

            foreach (var xnsStyle in xnsNode.Styles)
            {
                styles.Add(ToStyle(xnsStyle));
            }

            return styles;
        }

        private Style ToStyle(XnsStyle xnsStyle)
        {
            switch (xnsStyle.Name.ToLower())
            {
                case StyleIdentifier.Background:
                    return new BackgroundStyle(xnsStyle.Value);

                case StyleIdentifier.Border:
                    return new BorderStyle(xnsStyle.Value);

                case StyleIdentifier.Height:
                    return new HeightStyle(xnsStyle.Value);

                case StyleIdentifier.Layout:
                    return new LayoutStyle(xnsStyle.Value);

                case StyleIdentifier.Margin:
                    return new MarginStyle(xnsStyle.Value);

                case StyleIdentifier.Mask:
                    return new MaskStyle(xnsStyle.Value);

                case StyleIdentifier.Padding:
                    return new PaddingStyle(xnsStyle.Value);

                case StyleIdentifier.Width:
                    return new WidthStyle(xnsStyle.Value);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
