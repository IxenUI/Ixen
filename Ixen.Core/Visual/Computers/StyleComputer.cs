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

        private float _viewportWidth;
        private float _viewportHeight;

        internal void Compute(VisualElement element, StyleRegistry registry,
            float viewportWidth = 0, float viewportHeight = 0)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;

            ComputeTree(element, registry);
        }

        private void ComputeTree(VisualElement element, StyleRegistry registry)
        {
            if (element.MustRefreshStyles)
            {
                ApplyBaseStyle(element);
                ApplyClasses(element, registry);
                Inherit(element);
                SyncOverflow(element);
                SyncTransitions(element, registry);

                element.MustRefreshStyles = false;
            }

            foreach (VisualElement child in element.Children)
            {
                ComputeTree(child, registry);
            }

            if (!element.HasChrome)
            {
                return;
            }

            foreach (VisualElement chrome in element.Chrome)
            {
                ComputeTree(chrome, registry);
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

            bool defaults = registry.HasDefaultClasses;

            if (defaults)
            {
                ApplyClass(handlers, registry.GetDefault(target, name));
            }

            ApplyClass(handlers, registry.GetGlobal(target, name));
            ApplyScopedClasses(handlers, registry, target, name, element, scoped);

            for (int i = 0; i < element.States.Count; i++)
            {
                string stated = name + StyleScope.STATE_SEPARATOR + element.States[i];

                if (defaults)
                {
                    ApplyClass(handlers, registry.GetDefault(target, stated));
                }

                ApplyClass(handlers, registry.GetGlobal(target, stated));
                ApplyScopedClasses(handlers, registry, target, stated, element, scoped);
            }

            if (!registry.HasMediaClasses)
            {
                return;
            }

            ApplyMediaClasses(handlers, registry, target, name, element);

            for (int i = 0; i < element.States.Count; i++)
            {
                ApplyMediaClasses(handlers, registry, target,
                    name + StyleScope.STATE_SEPARATOR + element.States[i], element);
            }
        }

        private void ApplyMediaClasses(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, VisualElement element)
        {
            _matches.Clear();
            registry.CollectMatchingMediaClasses(target, name, element,
                _viewportWidth, _viewportHeight, _matches);

            foreach (StyleClass styleClass in _matches)
            {
                ApplyClass(handlers, styleClass);
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

            if (!handlers.LineHeight.Descriptor.IsDeclared)
            {
                handlers.LineHeight = from.LineHeight;
            }

            if (!handlers.LetterSpacing.Descriptor.IsDeclared)
            {
                handlers.LetterSpacing = from.LetterSpacing;
            }

            if (!handlers.PointerEvents.Descriptor.IsDeclared)
            {
                handlers.PointerEvents = from.PointerEvents;
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

            if (!handlers.TextShadow.Descriptor.IsDeclared)
            {
                handlers.TextShadow = from.TextShadow;
            }

            if (!handlers.TextDecoration.Descriptor.IsDeclared)
            {
                handlers.TextDecoration = from.TextDecoration;
            }
        }

        private void SyncOverflow(VisualElement element)
        {
            OverflowKind resolved = element.StylesHandlers.Overflow.Descriptor.Value;

            if (resolved == OverflowKind.Unset)
            {
                return;
            }

            element.Scrollable = resolved == OverflowKind.Scroll;
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

            RetargetTransform(element, transitions, handlers.Transform.Descriptor, keyframes);

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

        private void RetargetTransform(VisualElement element, TransitionStyleDescriptor transitions,
            TransformStyleDescriptor target, KeyframeAnimation keyframes)
        {
            if (keyframes.Drives(StyleIdentifier.TRANSFORM))
            {
                return;
            }

            TransformTransition transition = element.Animations.TransformFor();

            if (!transition.HasValue)
            {
                transition.Jump(target);
                return;
            }

            if (target.Matches(transition.To))
            {
                return;
            }

            TransitionSpec spec = transitions == null
                ? default
                : transitions.SpecOf(StyleIdentifier.TRANSFORM);

            if (spec.Duration <= 0 || !TransformStyleDescriptor.Compatible(transition.To, target))
            {
                transition.Jump(target);
                return;
            }

            transition.Start(target, Math.Max(1, spec.Duration / ElementAnimations.TICK),
                spec.Delay / ElementAnimations.TICK, spec.Easing);
        }

        private static bool Interpolatable(SizeUnit unit)
            => SizeTransition.CanInterpolate(unit);

        private void ApplyBaseStyle(VisualElement element)
        {
            VisualElementStylesDescriptors styles = element.Styles;
            VisualElementStylesHandlers handlers = element.StylesHandlers;

            handlers.Background = IsPainting(styles.Background)
                ? BackgroundStyleHandler.For(styles.Background)
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

            if (handlers.Filter.Descriptor != styles.Filter)
            {
                handlers.Filter = styles.Filter != null && styles.Filter.IsDeclared
                    ? FilterStyleHandler.For(styles.Filter)
                    : VisualElementStylesHandlers.DefaultFilter;
            }

            if (handlers.BackdropFilter.Descriptor != styles.BackdropFilter)
            {
                handlers.BackdropFilter = styles.BackdropFilter != null && styles.BackdropFilter.IsDeclared
                    ? FilterStyleHandler.For(styles.BackdropFilter)
                    : VisualElementStylesHandlers.DefaultBackdropFilter;
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

            if (handlers.LineHeight.Descriptor != styles.LineHeight)
            {
                handlers.LineHeight = styles.LineHeight != null && styles.LineHeight.IsDeclared
                    ? new LineHeightStyleHandler(styles.LineHeight)
                    : VisualElementStylesHandlers.DefaultLineHeight;
            }

            if (handlers.LetterSpacing.Descriptor != styles.LetterSpacing)
            {
                handlers.LetterSpacing = styles.LetterSpacing != null && styles.LetterSpacing.IsDeclared
                    ? new LetterSpacingStyleHandler(styles.LetterSpacing)
                    : VisualElementStylesHandlers.DefaultLetterSpacing;
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

            if (handlers.Overflow.Descriptor != styles.Overflow)
            {
                handlers.Overflow = styles.Overflow != null && styles.Overflow.Value != OverflowKind.Unset
                    ? new OverflowStyleHandler(styles.Overflow)
                    : VisualElementStylesHandlers.DefaultOverflow;
            }

            if (handlers.Overscroll.Descriptor != styles.Overscroll)
            {
                handlers.Overscroll = styles.Overscroll != null && styles.Overscroll.Value != OverscrollKind.Unset
                    ? new OverscrollStyleHandler(styles.Overscroll)
                    : VisualElementStylesHandlers.DefaultOverscroll;
            }

            if (handlers.PointerEvents.Descriptor != styles.PointerEvents)
            {
                handlers.PointerEvents = styles.PointerEvents != null && styles.PointerEvents.IsDeclared
                    ? new PointerEventsStyleHandler(styles.PointerEvents)
                    : VisualElementStylesHandlers.DefaultPointerEvents;
            }

            if (handlers.ObjectPosition.Descriptor != styles.ObjectPosition)
            {
                handlers.ObjectPosition = styles.ObjectPosition != null && !styles.ObjectPosition.IsDefault
                    ? new ObjectPositionStyleHandler(styles.ObjectPosition)
                    : VisualElementStylesHandlers.DefaultObjectPosition;
            }

            if (handlers.ObjectFit.Descriptor != styles.ObjectFit)
            {
                handlers.ObjectFit = styles.ObjectFit != null
                    ? new ObjectFitStyleHandler(styles.ObjectFit)
                    : VisualElementStylesHandlers.DefaultObjectFit;
            }

            if (handlers.Animation.Descriptor != styles.Animation)
            {
                handlers.Animation = styles.Animation != null && styles.Animation.IsDeclared
                    ? new AnimationStyleHandler(styles.Animation)
                    : VisualElementStylesHandlers.DefaultAnimation;
            }

            if (handlers.Anchor.Descriptor != styles.Anchor)
            {
                handlers.Anchor = styles.Anchor != null && !string.IsNullOrEmpty(styles.Anchor.Name)
                    ? new AnchorStyleHandler(styles.Anchor)
                    : VisualElementStylesHandlers.DefaultAnchor;
            }

            if (handlers.AnchorPlacement.Descriptor != styles.AnchorPlacement)
            {
                handlers.AnchorPlacement = styles.AnchorPlacement != null
                    ? new AnchorPlacementStyleHandler(styles.AnchorPlacement)
                    : VisualElementStylesHandlers.DefaultAnchorPlacement;
            }

            if (handlers.TextDecoration.Descriptor != styles.TextDecoration)
            {
                handlers.TextDecoration = styles.TextDecoration != null && styles.TextDecoration.IsDeclared
                    ? new TextDecorationStyleHandler(styles.TextDecoration)
                    : VisualElementStylesHandlers.DefaultTextDecoration;
            }

            if (handlers.Opacity.Descriptor != styles.Opacity)
            {
                handlers.Opacity = styles.Opacity != null && styles.Opacity.IsTransparent
                    ? new OpacityStyleHandler(styles.Opacity)
                    : VisualElementStylesHandlers.DefaultOpacity;
            }

            if (handlers.Transform.Descriptor != styles.Transform)
            {
                handlers.Transform = styles.Transform != null && styles.Transform.IsDeclared
                    ? new TransformStyleHandler(styles.Transform)
                    : VisualElementStylesHandlers.DefaultTransform;
            }

            if (handlers.TransformOrigin.Descriptor != styles.TransformOrigin)
            {
                handlers.TransformOrigin = styles.TransformOrigin != null && !styles.TransformOrigin.IsDefault
                    ? new TransformOriginStyleHandler(styles.TransformOrigin)
                    : VisualElementStylesHandlers.DefaultTransformOrigin;
            }

            if (handlers.ContentAlign.Descriptor != styles.ContentAlign)
            {
                handlers.ContentAlign = styles.ContentAlign != null && styles.ContentAlign.IsDeclared
                    ? new ContentAlignStyleHandler(styles.ContentAlign)
                    : VisualElementStylesHandlers.DefaultContentAlign;
            }

            if (handlers.Visibility.Descriptor != styles.Visibility)
            {
                handlers.Visibility = styles.Visibility != null && styles.Visibility.IsDeclared
                    ? new VisibilityStyleHandler(styles.Visibility)
                    : VisualElementStylesHandlers.DefaultVisibility;
            }

            if (handlers.Gap.Descriptor != styles.Gap)
            {
                handlers.Gap = styles.Gap != null && styles.Gap.IsDeclared
                    ? new GapStyleHandler(styles.Gap)
                    : VisualElementStylesHandlers.DefaultGap;
            }

            if (handlers.MinWidth.Descriptor != styles.MinWidth)
            {
                handlers.MinWidth = styles.MinWidth != null && styles.MinWidth.IsDeclared
                    ? new MinWidthStyleHandler(styles.MinWidth)
                    : VisualElementStylesHandlers.DefaultMinWidth;
            }

            if (handlers.MaxWidth.Descriptor != styles.MaxWidth)
            {
                handlers.MaxWidth = styles.MaxWidth != null && styles.MaxWidth.IsDeclared
                    ? new MaxWidthStyleHandler(styles.MaxWidth)
                    : VisualElementStylesHandlers.DefaultMaxWidth;
            }

            if (handlers.MinHeight.Descriptor != styles.MinHeight)
            {
                handlers.MinHeight = styles.MinHeight != null && styles.MinHeight.IsDeclared
                    ? new MinHeightStyleHandler(styles.MinHeight)
                    : VisualElementStylesHandlers.DefaultMinHeight;
            }

            if (handlers.MaxHeight.Descriptor != styles.MaxHeight)
            {
                handlers.MaxHeight = styles.MaxHeight != null && styles.MaxHeight.IsDeclared
                    ? new MaxHeightStyleHandler(styles.MaxHeight)
                    : VisualElementStylesHandlers.DefaultMaxHeight;
            }

            if (handlers.BoxShadow.Descriptor != styles.BoxShadow)
            {
                handlers.BoxShadow = styles.BoxShadow != null && styles.BoxShadow.IsDeclared
                    ? new BoxShadowStyleHandler(styles.BoxShadow)
                    : VisualElementStylesHandlers.DefaultBoxShadow;
            }

            if (handlers.TextShadow.Descriptor != styles.TextShadow)
            {
                handlers.TextShadow = styles.TextShadow != null && styles.TextShadow.IsDeclared
                    ? new TextShadowStyleHandler(styles.TextShadow)
                    : VisualElementStylesHandlers.DefaultTextShadow;
            }

            if (handlers.ZIndex.Descriptor != styles.ZIndex)
            {
                handlers.ZIndex = styles.ZIndex != null && styles.ZIndex.Value != 0
                    ? new ZIndexStyleHandler(styles.ZIndex)
                    : VisualElementStylesHandlers.DefaultZIndex;
            }
        }

        private static bool IsPainting(BackgroundStyleDescriptor descriptor)
            => descriptor != null
                && (!string.IsNullOrWhiteSpace(descriptor.Color) || !string.IsNullOrWhiteSpace(descriptor.ImageUrl)
                    || descriptor.Gradient != null);

        private static bool IsPainting(BorderStyleDescriptor descriptor)
            => descriptor != null
                && descriptor.HasBorder
                && !string.IsNullOrWhiteSpace(descriptor.Color);

        private void ApplyStyle(VisualElementStylesHandlers handlers, StyleDescriptor style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.BACKGROUND:
                    var background = (BackgroundStyleDescriptor)style;
                    handlers.Background = IsPainting(background)
                        ? BackgroundStyleHandler.For(background)
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

                case StyleIdentifier.FILTER:
                    handlers.Filter = FilterStyleHandler.For((FilterStyleDescriptor)style);
                    break;

                case StyleIdentifier.BACKDROP_FILTER:
                    handlers.BackdropFilter = FilterStyleHandler.For((FilterStyleDescriptor)style);
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

                case StyleIdentifier.ANCHOR:
                    handlers.Anchor = new AnchorStyleHandler((AnchorStyleDescriptor)style);
                    break;

                case StyleIdentifier.ANCHOR_PLACEMENT:
                    handlers.AnchorPlacement = new AnchorPlacementStyleHandler((AnchorPlacementStyleDescriptor)style);
                    break;

                case StyleIdentifier.TEXT_DECORATION:
                    handlers.TextDecoration = new TextDecorationStyleHandler((TextDecorationStyleDescriptor)style);
                    break;

                case StyleIdentifier.OPACITY:
                    handlers.Opacity = new OpacityStyleHandler((OpacityStyleDescriptor)style);
                    break;

                case StyleIdentifier.TRANSFORM:
                    handlers.Transform = new TransformStyleHandler((TransformStyleDescriptor)style);
                    break;

                case StyleIdentifier.TRANSFORM_ORIGIN:
                    handlers.TransformOrigin = new TransformOriginStyleHandler((TransformOriginStyleDescriptor)style);
                    break;

                case StyleIdentifier.CONTENT_ALIGN:
                    handlers.ContentAlign = new ContentAlignStyleHandler((ContentAlignStyleDescriptor)style);
                    break;

                case StyleIdentifier.VISIBILITY:
                    handlers.Visibility = new VisibilityStyleHandler((VisibilityStyleDescriptor)style);
                    break;

                case StyleIdentifier.GAP:
                    handlers.Gap = new GapStyleHandler((GapStyleDescriptor)style);
                    break;

                case StyleIdentifier.MIN_WIDTH:
                    handlers.MinWidth = new MinWidthStyleHandler((MinWidthStyleDescriptor)style);
                    break;

                case StyleIdentifier.MAX_WIDTH:
                    handlers.MaxWidth = new MaxWidthStyleHandler((MaxWidthStyleDescriptor)style);
                    break;

                case StyleIdentifier.MIN_HEIGHT:
                    handlers.MinHeight = new MinHeightStyleHandler((MinHeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.MAX_HEIGHT:
                    handlers.MaxHeight = new MaxHeightStyleHandler((MaxHeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.BOX_SHADOW:
                    handlers.BoxShadow = new BoxShadowStyleHandler((BoxShadowStyleDescriptor)style);
                    break;

                case StyleIdentifier.TEXT_SHADOW:
                    handlers.TextShadow = new TextShadowStyleHandler((TextShadowStyleDescriptor)style);
                    break;

                case StyleIdentifier.Z_INDEX:
                    handlers.ZIndex = new ZIndexStyleHandler((ZIndexStyleDescriptor)style);
                    break;

                case StyleIdentifier.OBJECT_FIT:
                    handlers.ObjectFit = new ObjectFitStyleHandler((ObjectFitStyleDescriptor)style);
                    break;

                case StyleIdentifier.OBJECT_POSITION:
                    handlers.ObjectPosition = new ObjectPositionStyleHandler((ObjectPositionStyleDescriptor)style);
                    break;

                case StyleIdentifier.OVERFLOW:
                    handlers.Overflow = new OverflowStyleHandler((OverflowStyleDescriptor)style);
                    break;

                case StyleIdentifier.OVERSCROLL_BEHAVIOR:
                    handlers.Overscroll = new OverscrollStyleHandler((OverscrollStyleDescriptor)style);
                    break;

                case StyleIdentifier.POINTER_EVENTS:
                    handlers.PointerEvents = new PointerEventsStyleHandler((PointerEventsStyleDescriptor)style);
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

                case StyleIdentifier.LETTER_SPACING:
                    handlers.LetterSpacing = new LetterSpacingStyleHandler((LetterSpacingStyleDescriptor)style);
                    break;

                case StyleIdentifier.LINE_HEIGHT:
                    handlers.LineHeight = new LineHeightStyleHandler((LineHeightStyleDescriptor)style);
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
