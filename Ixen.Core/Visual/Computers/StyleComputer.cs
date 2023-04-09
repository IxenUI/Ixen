using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;
using Ixen.Core.Visual.Styles;
using System.Text;
using System.Threading.Tasks;

namespace Ixen.Core.Visual.Computers
{
    internal class StyleComputer
    {
        internal void Compute(VisualElement element)
        {
            StyleClass sc;

            if (element.MustRefreshStyles)
            {
                //element.Styles = new();

                foreach (var c in element.Classes)
                {
                    sc = StyleSheet.GetClass(c);

                    if (sc != null)
                    {
                        foreach (var style in sc.Styles)
                        {
                            ApplyStyle(element.Styles, style);
                        }
                    }
                }

                if (element.Name != null)
                {
                    sc = StyleSheet.GetElementClass(element.Name);

                    if (sc != null)
                    {
                        foreach (var style in sc.Styles)
                        {
                            ApplyStyle(element.Styles, style);
                        }
                    }
                }

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                Compute(child);
            }
        }

        private void ApplyStyle(VisualElementStyles styles, StyleDescriptor style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.Background:
                    styles.Background = new BackgroundStyleHandler((BackgroundStyleDescriptor)style);
                    break;

                case StyleIdentifier.Border:
                    styles.Border = new BorderStyleHandler((BorderStyleDescriptor)style);
                    break;

                case StyleIdentifier.Height:
                    styles.Height = new HeightStyleHandler((HeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.Layout:
                    styles.Layout = new LayoutStyleHandler((LayoutStyleDescriptor)style);
                    break;

                case StyleIdentifier.Margin:
                    styles.Margin = new MarginStyleHandler((MarginStyleDescriptor)style);
                    break;

                case StyleIdentifier.Mask:
                    styles.Mask = new MaskStyleHandler((MaskStyleDescriptor)style);
                    break;

                case StyleIdentifier.Padding:
                    styles.Padding = new PaddingStyleHandler((PaddingStyleDescriptor)style);
                    break;

                case StyleIdentifier.Width:
                    styles.Width = new WidthStyleHandler((WidthStyleDescriptor)style);
                    break;

                default:
                    break;
            }
        }

        private string GetScope(VisualElement element)
        {
            var sb = new StringBuilder();

            while (element.Parent != null)
            {
                if (!string.IsNullOrEmpty(element.Parent.Name))
                {
                    sb.Insert(0, element.Parent.Name);
                }

                element = element.Parent;
            }

            return sb.Length > 0
                ? sb.ToString()
                : null;
        }
    }
}
