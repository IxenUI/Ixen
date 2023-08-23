using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Visual.Computers
{
    internal class StyleComputer
    {
        internal void Compute(VisualElement element)
        {
            if (element.MustRefreshStyles)
            {
                ApplyBaseStyle(element);

                foreach (var c in GetApplyingClasses(element))
                {
                    foreach (var style in c.Styles)
                    {
                        ApplyStyle(element.StylesHandlers, style);
                    }
                }

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                Compute(child);
            }
        }

        private void AddClassToList(List<StyleClass> list, StyleClass c)
        {
            if (c != null)
            {
                list.Add(c);
            }
        }

        // priority order :
        // - Element name 
        // - Global element name
        // - Class
        // - Global class
        // - Type
        // - Global type
        private List<StyleClass> GetApplyingClasses(VisualElement element)
        {
            var list = new List<StyleClass>();
            var scope = GetScope(element);

            AddClassToList(list, StyleSheet.GetGlobalTypeClass(element.TypeName));
            AddClassToList(list, StyleSheet.GetGlobalTypeClass(element.TypeName, scope));

            foreach (var c in element.Classes)
            {
                AddClassToList(list, StyleSheet.GetGlobalClass(c));
                AddClassToList(list, StyleSheet.GetGlobalClass(c, scope));
            }

            if (element.Name != null)
            {
                AddClassToList(list, StyleSheet.GetGlobalElementClass(element.Name));
                AddClassToList(list, StyleSheet.GetGlobalElementClass(element.Name, scope));
            }

            return list;
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

                case StyleIdentifier.ColumnTemplate:
                    handlers.ColumnTemplate = new ColumnTemplateStyleHandler((ColumnTemplateStyleDescriptor)style);
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

                case StyleIdentifier.Padding:
                    handlers.Padding = new PaddingStyleHandler((PaddingStyleDescriptor)style);
                    break;

                case StyleIdentifier.RowTemplate:
                    handlers.RowTemplate = new RowTemplateStyleHandler((RowTemplateStyleDescriptor)style);
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
