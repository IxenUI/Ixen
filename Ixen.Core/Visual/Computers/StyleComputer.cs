using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class StyleComputer
    {
        private readonly List<StyleClass> _matches = new();

        internal void Compute(VisualElement element, StyleRegistry registry)
        {
            if (element.MustRefreshStyles)
            {
                ApplyBaseStyle(element);
                ApplyClasses(element, registry);
                Inherit(element);
                SyncTransitions(element, registry);

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                Compute(child, registry);
            }

            if (!element.HasChrome)
            {
                return;
            }

            foreach (VisualElement chrome in element.Chrome)
            {
                Compute(chrome, registry);
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
            VisualElementStylesHandlers handlers = element.StylesHandlers;
            bool scoped = registry.HasScopedClasses;

            ApplySelector(handlers, registry, StyleClassTarget.ElementType, element.TypeName, element, scoped);

            foreach (string c in element.Classes)
            {
                ApplySelector(handlers, registry, StyleClassTarget.ClassName, c, element, scoped);
            }

            ApplySelector(handlers, registry, StyleClassTarget.ElementName, element.Name, element, scoped);
        }

        private void ApplySelector(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, VisualElement element, bool scoped)
        {
            if (name == null)
            {
                return;
            }

            ApplyClass(handlers, registry.GetGlobal(target, name));
            ApplyScopedClasses(handlers, registry, target, name, element, scoped);

            for (int i = 0; i < element.States.Count; i++)
            {
                string stated = name + StyleScope.STATE_SEPARATOR + element.States[i];

                ApplyClass(handlers, registry.GetGlobal(target, stated));
                ApplyScopedClasses(handlers, registry, target, stated, element, scoped);
            }
        }

        private void ApplyScopedClasses(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, VisualElement element, bool scoped)
        {
            if (!scoped || name == null)
            {
                return;
            }

            _matches.Clear();
            registry.CollectMatchingScopedClasses(target, name, element, _matches);

            foreach (StyleClass styleClass in _matches)
            {
                ApplyClass(handlers, styleClass);
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

        private void Inherit(VisualElement element)
        {
            VisualElement parent = element.Parent;

            if (parent == null)
            {
                return;
            }

            VisualElementStylesHandlers handlers = element.StylesHandlers;
            VisualElementStylesHandlers from = parent.StylesHandlers;

            if (handlers.Color.Descriptor.Value == null)
            {
                handlers.Color = from.Color;
            }

            if (handlers.FontFamily.Descriptor.Value == null)
            {
                handlers.FontFamily = from.FontFamily;
            }

            if (handlers.FontSize.Descriptor.Value <= 0)
            {
                handlers.FontSize = from.FontSize;
            }

            if (handlers.FontWeight.Descriptor.Value == FontWeight.Unset)
            {
                handlers.FontWeight = from.FontWeight;
            }

            if (handlers.FontStyle.Descriptor.Value == FontStyle.Unset)
            {
                handlers.FontStyle = from.FontStyle;
            }

            if (handlers.Cursor.Descriptor.Value == CursorKind.Unset)
            {
                handlers.Cursor = from.Cursor;
            }
        }

        private void SyncTransitions(VisualElement element, StyleRegistry registry)
        {
            VisualElementStylesHandlers handlers = element.StylesHandlers;
            TransitionStyleDescriptor transitions = handlers.Transition.Descriptor;
            AnimationStyleDescriptor animation = handlers.Animation.Descriptor;

            bool declared = (transitions != null && transitions.Specs.Count > 0)
                || (animation != null && animation.IsDeclared);

            if (!declared && !element.HasAnimations)
            {
                return;
            }

            SyncAnimation(element, registry, animation);

            KeyframeAnimation keyframes = element.Animations.Keyframes;

            Retarget(element, transitions, StyleIdentifier.BACKGROUND, handlers.Background.Color, keyframes);
            Retarget(element, transitions, StyleIdentifier.COLOR, handlers.Color.Brush.Color, keyframes);
            Retarget(element, transitions, StyleIdentifier.BORDER, handlers.Border.Color, keyframes);

            RetargetSize(element, transitions, StyleIdentifier.WIDTH, handlers.Width.Descriptor, keyframes);
            RetargetSize(element, transitions, StyleIdentifier.HEIGHT, handlers.Height.Descriptor, keyframes);
            RetargetSize(element, transitions, StyleIdentifier.LEFT, handlers.Left.Descriptor, keyframes);
            RetargetSize(element, transitions, StyleIdentifier.TOP, handlers.Top.Descriptor, keyframes);
            RetargetSize(element, transitions, StyleIdentifier.RIGHT, handlers.Right.Descriptor, keyframes);
            RetargetSize(element, transitions, StyleIdentifier.BOTTOM, handlers.Bottom.Descriptor, keyframes);

            element.Animations.Sync();
        }

        private void SyncAnimation(VisualElement element, StyleRegistry registry,
            AnimationStyleDescriptor spec)
        {
            if (spec == null || !spec.IsDeclared)
            {
                if (element.HasAnimations)
                {
                    element.Animations.StopKeyframes();
                }

                return;
            }

            KeyframeAnimation animation = element.Animations.Keyframes;

            if (animation.StartedWith(spec))
            {
                return;
            }

            animation.Start(registry.GetKeyframes(spec.Name), spec);
        }

        private void Retarget(VisualElement element, TransitionStyleDescriptor transitions,
            string identifier, Color target, KeyframeAnimation keyframes)
        {
            if (keyframes.Drives(identifier))
            {
                return;
            }

            ColorTransition transition = element.Animations.For(identifier);

            if (!transition.HasValue)
            {
                transition.Jump(target);
                return;
            }

            if (transition.To.Equals(target))
            {
                return;
            }

            TransitionSpec spec = transitions == null ? default : transitions.SpecOf(identifier);

            if (spec.Duration <= 0)
            {
                transition.Jump(target);
                return;
            }

            transition.Start(target, Math.Max(1, spec.Duration / ElementAnimations.TICK),
                spec.Delay / ElementAnimations.TICK, spec.Easing);
        }

        private void RetargetSize(VisualElement element, TransitionStyleDescriptor transitions,
            string identifier, SizeStyleDescriptor target, KeyframeAnimation keyframes)
        {
            if (keyframes.Drives(identifier))
            {
                return;
            }

            SizeTransition transition = element.Animations.SizeFor(identifier);

            if (!transition.HasValue)
            {
                transition.Jump(target.Unit, target.Value);
                return;
            }

            if (transition.Unit == target.Unit && transition.To == target.Value)
            {
                return;
            }

            TransitionSpec spec = transitions == null ? default : transitions.SpecOf(identifier);

            if (spec.Duration <= 0 || transition.Unit != target.Unit || !Interpolatable(target.Unit))
            {
                transition.Jump(target.Unit, target.Value);
                return;
            }

            transition.Start(target.Value, Math.Max(1, spec.Duration / ElementAnimations.TICK),
                spec.Delay / ElementAnimations.TICK, spec.Easing);
        }

        private static bool Interpolatable(SizeUnit unit)
            => SizeTransition.CanInterpolate(unit);

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
                handlers.Color = styles.Color != null && styles.Color.Value != null
                    ? new ColorStyleHandler(styles.Color)
                    : VisualElementStylesHandlers.DefaultColor;
            }

            if (handlers.FontFamily.Descriptor != styles.FontFamily)
            {
                handlers.FontFamily = styles.FontFamily != null && styles.FontFamily.Value != null
                    ? new FontFamilyStyleHandler(styles.FontFamily)
                    : VisualElementStylesHandlers.DefaultFontFamily;
            }

            if (handlers.FontSize.Descriptor != styles.FontSize)
            {
                handlers.FontSize = styles.FontSize != null && styles.FontSize.Value > 0
                    ? new FontSizeStyleHandler(styles.FontSize)
                    : VisualElementStylesHandlers.DefaultFontSize;
            }

            if (handlers.FontStyle.Descriptor != styles.FontStyle)
            {
                handlers.FontStyle = styles.FontStyle != null && styles.FontStyle.Value != FontStyle.Unset
                    ? new FontStyleStyleHandler(styles.FontStyle)
                    : VisualElementStylesHandlers.DefaultFontStyle;
            }

            if (handlers.FontWeight.Descriptor != styles.FontWeight)
            {
                handlers.FontWeight = styles.FontWeight != null && styles.FontWeight.Value != FontWeight.Unset
                    ? new FontWeightStyleHandler(styles.FontWeight)
                    : VisualElementStylesHandlers.DefaultFontWeight;
            }

            if (handlers.CornerRadius.Descriptor != styles.CornerRadius)
            {
                handlers.CornerRadius = styles.CornerRadius != null
                    ? new CornerRadiusStyleHandler(styles.CornerRadius)
                    : VisualElementStylesHandlers.DefaultCornerRadius;
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

            if (handlers.TextAlign.Descriptor != styles.TextAlign)
            {
                handlers.TextAlign = styles.TextAlign != null
                    ? new TextAlignStyleHandler(styles.TextAlign)
                    : VisualElementStylesHandlers.DefaultTextAlign;
            }

            if (handlers.TextOverflow.Descriptor != styles.TextOverflow)
            {
                handlers.TextOverflow = styles.TextOverflow != null
                    ? new TextOverflowStyleHandler(styles.TextOverflow)
                    : VisualElementStylesHandlers.DefaultTextOverflow;
            }

            if (handlers.TextWrap.Descriptor != styles.TextWrap)
            {
                handlers.TextWrap = styles.TextWrap != null
                    ? new TextWrapStyleHandler(styles.TextWrap)
                    : VisualElementStylesHandlers.DefaultTextWrap;
            }

            if (handlers.Width.Descriptor != styles.Width)
            {
                handlers.Width = new WidthStyleHandler(styles.Width);
            }

            if (handlers.Left.Descriptor != styles.Left)
            {
                handlers.Left = new LeftStyleHandler(styles.Left);
            }

            if (handlers.Top.Descriptor != styles.Top)
            {
                handlers.Top = new TopStyleHandler(styles.Top);
            }

            if (handlers.Right.Descriptor != styles.Right)
            {
                handlers.Right = new RightStyleHandler(styles.Right);
            }

            if (handlers.Bottom.Descriptor != styles.Bottom)
            {
                handlers.Bottom = new BottomStyleHandler(styles.Bottom);
            }

            if (handlers.Dock.Descriptor != styles.Dock)
            {
                handlers.Dock = styles.Dock != null
                    ? new DockStyleHandler(styles.Dock)
                    : VisualElementStylesHandlers.DefaultDock;
            }

            if (handlers.ColumnIndex.Descriptor != styles.ColumnIndex)
            {
                handlers.ColumnIndex = styles.ColumnIndex != null
                    ? new ColumnIndexStyleHandler(styles.ColumnIndex)
                    : VisualElementStylesHandlers.DefaultColumnIndex;
            }

            if (handlers.RowIndex.Descriptor != styles.RowIndex)
            {
                handlers.RowIndex = styles.RowIndex != null
                    ? new RowIndexStyleHandler(styles.RowIndex)
                    : VisualElementStylesHandlers.DefaultRowIndex;
            }

            if (handlers.ColumnSpan.Descriptor != styles.ColumnSpan)
            {
                handlers.ColumnSpan = styles.ColumnSpan != null
                    ? new ColumnSpanStyleHandler(styles.ColumnSpan)
                    : VisualElementStylesHandlers.DefaultColumnSpan;
            }

            if (handlers.RowSpan.Descriptor != styles.RowSpan)
            {
                handlers.RowSpan = styles.RowSpan != null
                    ? new RowSpanStyleHandler(styles.RowSpan)
                    : VisualElementStylesHandlers.DefaultRowSpan;
            }

            if (handlers.Cursor.Descriptor != styles.Cursor)
            {
                handlers.Cursor = styles.Cursor != null && styles.Cursor.Value != CursorKind.Unset
                    ? new CursorStyleHandler(styles.Cursor)
                    : VisualElementStylesHandlers.DefaultCursor;
            }

            if (handlers.Transition.Descriptor != styles.Transition)
            {
                handlers.Transition = styles.Transition != null
                    ? new TransitionStyleHandler(styles.Transition)
                    : VisualElementStylesHandlers.DefaultTransition;
            }

            if (handlers.Animation.Descriptor != styles.Animation)
            {
                handlers.Animation = styles.Animation != null && styles.Animation.IsDeclared
                    ? new AnimationStyleHandler(styles.Animation)
                    : VisualElementStylesHandlers.DefaultAnimation;
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

                case StyleIdentifier.CORNER_RADIUS:
                    handlers.CornerRadius = new CornerRadiusStyleHandler((CornerRadiusStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_FAMILY:
                    handlers.FontFamily = new FontFamilyStyleHandler((FontFamilyStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_SIZE:
                    handlers.FontSize = new FontSizeStyleHandler((FontSizeStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_STYLE:
                    handlers.FontStyle = new FontStyleStyleHandler((FontStyleStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_WEIGHT:
                    handlers.FontWeight = new FontWeightStyleHandler((FontWeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.HEIGHT:
                    handlers.Height = new HeightStyleHandler((HeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.LAYOUT:
                    handlers.Layout = new LayoutStyleHandler((LayoutStyleDescriptor)style);
                    break;

                case StyleIdentifier.LEFT:
                    handlers.Left = new LeftStyleHandler((LeftStyleDescriptor)style);
                    break;

                case StyleIdentifier.TOP:
                    handlers.Top = new TopStyleHandler((TopStyleDescriptor)style);
                    break;

                case StyleIdentifier.RIGHT:
                    handlers.Right = new RightStyleHandler((RightStyleDescriptor)style);
                    break;

                case StyleIdentifier.BOTTOM:
                    handlers.Bottom = new BottomStyleHandler((BottomStyleDescriptor)style);
                    break;

                case StyleIdentifier.CURSOR:
                    handlers.Cursor = new CursorStyleHandler((CursorStyleDescriptor)style);
                    break;

                case StyleIdentifier.TRANSITION:
                    handlers.Transition = new TransitionStyleHandler((TransitionStyleDescriptor)style);
                    break;

                case StyleIdentifier.ANIMATION:
                    handlers.Animation = new AnimationStyleHandler((AnimationStyleDescriptor)style);
                    break;

                case StyleIdentifier.DOCK:
                    handlers.Dock = new DockStyleHandler((DockStyleDescriptor)style);
                    break;

                case StyleIdentifier.COLUMN_INDEX:
                    handlers.ColumnIndex = new ColumnIndexStyleHandler((ColumnIndexStyleDescriptor)style);
                    break;

                case StyleIdentifier.ROW_INDEX:
                    handlers.RowIndex = new RowIndexStyleHandler((RowIndexStyleDescriptor)style);
                    break;

                case StyleIdentifier.COLUMN_SPAN:
                    handlers.ColumnSpan = new ColumnSpanStyleHandler((ColumnSpanStyleDescriptor)style);
                    break;

                case StyleIdentifier.ROW_SPAN:
                    handlers.RowSpan = new RowSpanStyleHandler((RowSpanStyleDescriptor)style);
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

                case StyleIdentifier.TEXT_ALIGN:
                    handlers.TextAlign = new TextAlignStyleHandler((TextAlignStyleDescriptor)style);
                    break;

                case StyleIdentifier.TEXT_OVERFLOW:
                    handlers.TextOverflow = new TextOverflowStyleHandler((TextOverflowStyleDescriptor)style);
                    break;

                case StyleIdentifier.TEXT_WRAP:
                    handlers.TextWrap = new TextWrapStyleHandler((TextWrapStyleDescriptor)style);
                    break;

                case StyleIdentifier.WIDTH:
                    handlers.Width = new WidthStyleHandler((WidthStyleDescriptor)style);
                    break;

                default:
                    break;
            }
        }

    }
}
