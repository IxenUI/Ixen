using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Visual.Computers
{
    internal class LayoutComputer
    {
        private ColumnLayoutComputer _columnComputer = new();
        private RowLayoutComputer _rowComputer = new();

        internal void Compute(VisualElement element)
        {
            LayoutStyle layoutStyle = element.Styles.Layout;
            if (layoutStyle != null)
            {
                switch (element.Styles.Layout.Type)
                {
                    case LayoutType.Column:
                        _columnComputer.Compute(element);
                        break;

                    case LayoutType.Row:
                        _rowComputer.Compute(element);
                        break;

                    case LayoutType.Grid:
                        break;

                    case LayoutType.Absolute:
                        break;

                    case LayoutType.Fixed:
                        break;

                    case LayoutType.Dock:
                        break;
                }
            }
            
            foreach (var child in element.Children)
            {
                Compute(child);
            }
        }
    }
}
