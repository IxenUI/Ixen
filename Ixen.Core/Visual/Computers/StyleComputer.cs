using Ixen.Core.Visual.Classes;
using System.Text;

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
                if (element.Name != null)
                {
                    sc = StyleSheet.GetElementClass(element.Name);

                    if (sc != null)
                    {
                        foreach (var style in sc.Styles)
                        {
                            element.Styles.ApplyStyle(style);
                        }
                    }
                }
                
                foreach (var c in element.Classes)
                {
                    sc = StyleSheet.GetClass(c);

                    if (sc != null)
                    {
                        foreach (var style in sc.Styles)
                        {
                            element.Styles.ApplyStyle(style);
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
