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
                ApplyBaseStyle(element);

                foreach (var c in element.Classes)
                {
                    sc = StyleSheet.GetClass(c);

                    if (sc != null)
                    {
                        foreach (var style in sc.Styles)
                        {
                            ApplyStyle(element.StylesHandlers, style);
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
                            ApplyStyle(element.StylesHandlers, style);
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

        private void ApplyBaseStyle(VisualElement element)
        {
            element.StylesHandlers = new();

            if (element.Styles.Background != null)
            {
                element.StylesHandlers.Background = new BackgroundStyleHandler(element.Styles.Background);
            }
            
            if (element.Styles.Border != null)
            {
                element.StylesHandlers.Border = new BorderStyleHandler(element.Styles.Border);
            }

            element.StylesHandlers.Height = new HeightStyleHandler(element.Styles.Height);
            element.StylesHandlers.Layout = new LayoutStyleHandler(element.Styles.Layout);
            element.StylesHandlers.Margin = new MarginStyleHandler(element.Styles.Margin);
            element.StylesHandlers.Mask = new MaskStyleHandler(element.Styles.Mask);
            element.StylesHandlers.Padding = new PaddingStyleHandler(element.Styles.Padding);
            element.StylesHandlers.Width = new WidthStyleHandler(element.Styles.Width);
        }

        private void ApplyStyle(VisualElementStylesHandlers handlers, StyleDescriptor style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.Background:
                    handlers.Background = new BackgroundStyleHandler((BackgroundStyleDescriptor)style);
                    break;

                case StyleIdentifier.Border:
                    handlers.Border = new BorderStyleHandler((BorderStyleDescriptor)style);
                    break;

                case StyleIdentifier.Height:
                    handlers.Height = new HeightStyleHandler((HeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.Layout:
                    handlers.Layout = new LayoutStyleHandler((LayoutStyleDescriptor)style);
                    break;

                case StyleIdentifier.Margin:
                    handlers.Margin = new MarginStyleHandler((MarginStyleDescriptor)style);
                    break;

                case StyleIdentifier.Mask:
                    handlers.Mask = new MaskStyleHandler((MaskStyleDescriptor)style);
                    break;

                case StyleIdentifier.Padding:
                    handlers.Padding = new PaddingStyleHandler((PaddingStyleDescriptor)style);
                    break;

                case StyleIdentifier.Width:
                    handlers.Width = new WidthStyleHandler((WidthStyleDescriptor)style);
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
