using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class StyleComputer
    {
        internal void Compute(VisualElement element, StyleRegistry registry)
        {
            if (element.MustRefreshStyles)
            {
                ApplyBaseStyle(element);
                ApplyClasses(element, registry);

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                Compute(child, registry);
            }
        }

        // priority order :
        // - Element name
        // - Global element name
        // - Class
        // - Global class
        // - Type
        // - Global type
        private void ApplyClasses(VisualElement element, StyleRegistry registry)
        {
            string scope = registry.HasScopedClasses ? GetScope(element) : null;
            VisualElementStylesHandlers handlers = element.StylesHandlers;

            ApplyClass(handlers, registry.GetGlobalTypeClass(element.TypeName));
            ApplyClass(handlers, registry.GetGlobalTypeClass(element.TypeName, scope));

            foreach (string c in element.Classes)
            {
                ApplyClass(handlers, registry.GetGlobalClass(c));
                ApplyClass(handlers, registry.GetGlobalClass(c, scope));
            }

            if (element.Name != null)
            {
                ApplyClass(handlers, registry.GetGlobalElementClass(element.Name));
                ApplyClass(handlers, registry.GetGlobalElementClass(element.Name, scope));
            }
        }

        private void ApplyClass(VisualElementStylesHandlers handlers, StyleClass styleClass)
        {
            if (styleClass?.Styles == null)
            {
                return;
            }

            foreach (StyleDescriptor style in styleClass.Styles)
            {
                ApplyStyle(handlers, style);
            }
        }

        private void ApplyBaseStyle(VisualElement element)
        {
            VisualElementStylesDescriptors styles = element.Styles;
            VisualElementStylesHandlers handlers = element.StylesHandlers;

            handlers.Background = IsPainting(styles.Background)
                ? new BackgroundStyleHandler(styles.Background)
                : VisualElementStylesHandlers.DefaultBackground;

            handlers.Border = IsPainting(styles.Border)
                ? new BorderStyleHandler(styles.Border)
                : VisualElementStylesHandlers.DefaultBorder;

            if (handlers.Color.Descriptor != styles.Color)
            {
                handlers.Color = styles.Color != null
                    ? new ColorStyleHandler(styles.Color)
                    : VisualElementStylesHandlers.DefaultColor;
            }

            if (handlers.FontFamily.Descriptor != styles.FontFamily)
            {
                handlers.FontFamily = styles.FontFamily != null
                    ? new FontFamilyStyleHandler(styles.FontFamily)
                    : VisualElementStylesHandlers.DefaultFontFamily;
            }

            if (handlers.FontSize.Descriptor != styles.FontSize)
            {
                handlers.FontSize = styles.FontSize != null
                    ? new FontSizeStyleHandler(styles.FontSize)
                    : VisualElementStylesHandlers.DefaultFontSize;
            }

            if (handlers.ColumnTemplate.Descriptor != styles.ColumnTemplate)
            {
                handlers.ColumnTemplate = styles.ColumnTemplate != null
                    ? new ColumnTemplateStyleHandler(styles.ColumnTemplate)
                    : VisualElementStylesHandlers.DefaultColumnTemplate;
            }

            if (handlers.RowTemplate.Descriptor != styles.RowTemplate)
            {
                handlers.RowTemplate = styles.RowTemplate != null
                    ? new RowTemplateStyleHandler(styles.RowTemplate)
                    : VisualElementStylesHandlers.DefaultRowTemplate;
            }

            if (handlers.Height.Descriptor != styles.Height)
            {
                handlers.Height = new HeightStyleHandler(styles.Height);
            }

            if (handlers.Layout.Descriptor != styles.Layout)
            {
                handlers.Layout = new LayoutStyleHandler(styles.Layout);
            }

            if (handlers.Margin.Descriptor != styles.Margin)
            {
                handlers.Margin = new MarginStyleHandler(styles.Margin);
            }

            if (handlers.Padding.Descriptor != styles.Padding)
            {
                handlers.Padding = new PaddingStyleHandler(styles.Padding);
            }

            if (handlers.Width.Descriptor != styles.Width)
            {
                handlers.Width = new WidthStyleHandler(styles.Width);
            }
        }

        private static bool IsPainting(BackgroundStyleDescriptor descriptor)
            => descriptor != null
                && (!string.IsNullOrWhiteSpace(descriptor.Color) || !string.IsNullOrWhiteSpace(descriptor.ImageUrl));

        private static bool IsPainting(BorderStyleDescriptor descriptor)
            => descriptor != null
                && descriptor.Thickness > 0
                && !string.IsNullOrWhiteSpace(descriptor.Color);

        private void ApplyStyle(VisualElementStylesHandlers handlers, StyleDescriptor style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.BACKGROUND:
                    var background = (BackgroundStyleDescriptor)style;
                    handlers.Background = IsPainting(background)
                        ? new BackgroundStyleHandler(background)
                        : VisualElementStylesHandlers.DefaultBackground;
                    break;

                case StyleIdentifier.BORDER:
                    var border = (BorderStyleDescriptor)style;
                    handlers.Border = IsPainting(border)
                        ? new BorderStyleHandler(border)
                        : VisualElementStylesHandlers.DefaultBorder;
                    break;

                case StyleIdentifier.COLOR:
                    handlers.Color = new ColorStyleHandler((ColorStyleDescriptor)style);
                    break;

                case StyleIdentifier.COLUMN_TEMPLATE:
                    handlers.ColumnTemplate = new ColumnTemplateStyleHandler((ColumnTemplateStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_FAMILY:
                    handlers.FontFamily = new FontFamilyStyleHandler((FontFamilyStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_SIZE:
                    handlers.FontSize = new FontSizeStyleHandler((FontSizeStyleDescriptor)style);
                    break;

                case StyleIdentifier.HEIGHT:
                    handlers.Height = new HeightStyleHandler((HeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.LAYOUT:
                    handlers.Layout = new LayoutStyleHandler((LayoutStyleDescriptor)style);
                    break;

                case StyleIdentifier.MARGIN:
                    handlers.Margin = new MarginStyleHandler((MarginStyleDescriptor)style);
                    break;

                case StyleIdentifier.PADDING:
                    handlers.Padding = new PaddingStyleHandler((PaddingStyleDescriptor)style);
                    break;

                case StyleIdentifier.ROW_TEMPLATE:
                    handlers.RowTemplate = new RowTemplateStyleHandler((RowTemplateStyleDescriptor)style);
                    break;

                case StyleIdentifier.WIDTH:
                    handlers.Width = new WidthStyleHandler((WidthStyleDescriptor)style);
                    break;

                default:
                    break;
            }
        }

        private string GetScope(VisualElement element)
            => StyleScope.Build(element, e => e.Parent, e => e.Name);
    }
}
