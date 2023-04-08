using Ixen.Core.Visual.Classes;

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

                    if (sc == null)
                    {
                        continue;
                    }

                    foreach (var style in sc.Styles)
                    {
                        element.Styles.ApplyStyle(style);
                    }
                }

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                Compute(child);
            }
        }
    }
}
