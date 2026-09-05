using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Visual.Computers
{
    internal class StyleComputer
    {
        private readonly List<StyleClass> _matches = new();

        private StyleTrace _trace;
        private bool _tracing;

        internal StyleTrace Trace
        {
            set => _trace = value;
        }

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

            if (element.ChildrenChanged)
            {
                element.ChildrenChanged = false;

                if (registry.HasStructuralClasses)
                {
                    foreach (VisualElement child in element.Children)
                    {
                        child.MustRefreshStyles = true;
                    }
                }
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
            _tracing = _trace != null && _trace.Element == element;

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
                ApplyClass(handlers, registry.GetDefault(target, name), true);
            }

            ApplyClass(handlers, registry.GetGlobal(target, name));
            ApplyScopedClasses(handlers, registry, target, name, element, scoped);

            if (registry.HasStructuralClasses)
            {
                ApplyStructural(handlers, registry, target, name, element, scoped, defaults);
            }

            for (int i = 0; i < element.States.Count; i++)
            {
                string stated = name + StyleScope.STATE_SEPARATOR + element.States[i];

                if (defaults)
                {
                    ApplyClass(handlers, registry.GetDefault(target, stated), true);
                }

                ApplyClass(handlers, registry.GetGlobal(target, stated));
                ApplyScopedClasses(handlers, registry, target, stated, element, scoped);
            }

            if (registry.HasMediaClasses)
            {
                ApplyMediaClasses(handlers, registry, target, name, element);

                for (int i = 0; i < element.States.Count; i++)
                {
                    ApplyMediaClasses(handlers, registry, target,
                        name + StyleScope.STATE_SEPARATOR + element.States[i], element);
                }
            }

            if (!registry.HasContainerClasses)
            {
                return;
            }

            ApplyContainerClasses(handlers, registry, target, name, element);

            for (int i = 0; i < element.States.Count; i++)
            {
                ApplyContainerClasses(handlers, registry, target,
                    name + StyleScope.STATE_SEPARATOR + element.States[i], element);
            }
        }

        private void ApplyContainerClasses(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, VisualElement element)
        {
            _matches.Clear();
            registry.CollectMatchingContainerClasses(target, name, element,
                _viewportWidth, _viewportHeight, _matches);

            foreach (StyleClass styleClass in _matches)
            {
                ApplyClass(handlers, styleClass);
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

        private void ApplyStructural(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, VisualElement element, bool scoped, bool defaults)
        {
            if (!StyleStructural.Position(element, out int index, out int count))
            {
                return;
            }

            StructuralKinds kinds = registry.Structural;

            if (Wanted(kinds, StructuralKinds.First, index, count))
            {
                ApplyVariant(handlers, registry, target, name, StyleStructural.FIRST_CHILD,
                    element, scoped, defaults);
            }

            if (Wanted(kinds, StructuralKinds.Last, index, count))
            {
                ApplyVariant(handlers, registry, target, name, StyleStructural.LAST_CHILD,
                    element, scoped, defaults);
            }

            if (Wanted(kinds, StructuralKinds.Only, index, count))
            {
                ApplyVariant(handlers, registry, target, name, StyleStructural.ONLY_CHILD,
                    element, scoped, defaults);
            }

            if (Wanted(kinds, StructuralKinds.Odd, index, count))
            {
                ApplyVariant(handlers, registry, target, name, Nth(StyleStructural.ODD),
                    element, scoped, defaults);
            }

            if (Wanted(kinds, StructuralKinds.Even, index, count))
            {
                ApplyVariant(handlers, registry, target, name, Nth(StyleStructural.EVEN),
                    element, scoped, defaults);
            }

            if ((kinds & StructuralKinds.Nth) != 0)
            {
                ApplyVariant(handlers, registry, target, name,
                    Nth((index + 1).ToString(CultureInfo.InvariantCulture)),
                    element, scoped, defaults);
            }
        }

        private static bool Wanted(StructuralKinds declared, StructuralKinds kind,
            int index, int count)
            => (declared & kind) != 0 && StyleStructural.Holds(kind, index, count);

        private static string Nth(string argument)
            => StyleStructural.NTH_CHILD + "(" + argument + ")";

        private void ApplyVariant(VisualElementStylesHandlers handlers, StyleRegistry registry,
            StyleClassTarget target, string name, string pseudo, VisualElement element,
            bool scoped, bool defaults)
        {
            string candidate = name + StyleScope.STATE_SEPARATOR + pseudo;

            if (defaults)
            {
                ApplyClass(handlers, registry.GetDefault(target, candidate), true);
            }

            ApplyClass(handlers, registry.GetGlobal(target, candidate));
            ApplyScopedClasses(handlers, registry, target, candidate, element, scoped);
        }

        private void ApplyClass(VisualElementStylesHandlers handlers, StyleClass styleClass,
            bool isDefault = false)
        {
            if (styleClass?.Styles == null)
            {
                return;
            }

            if (_tracing)
            {
                _trace.Record(styleClass, isDefault);
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

            animation.Start(spec, registry);
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
                transition.Jump(target.Unit, target.Value, target.Offset);
                return;
            }

            if (transition.Unit == target.Unit && transition.To == target.Value
                && transition.Offset == target.Offset)
            {
                return;
            }

            TransitionSpec spec = transitions == null ? default : transitions.SpecOf(identifier);

            if (spec.Duration <= 0 || transition.Unit != target.Unit
                || transition.Offset != target.Offset || !Interpolatable(target.Unit))
            {
                transition.Jump(target.Unit, target.Value, target.Offset);
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
                ? BorderStyleHandler.For(styles.Border)
                : VisualElementStylesHandlers.DefaultBorder;

            if (handlers.Color.Descriptor != styles.Color)
            {
                handlers.Color = styles.Color != null && styles.Color.Value != null
                    ? ColorStyleHandler.For(styles.Color)
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
                    ? Cached<FontFamilyStyleDescriptor, FontFamilyStyleHandler>(styles.FontFamily, static d => new FontFamilyStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultFontFamily;
            }

            if (handlers.FontSize.Descriptor != styles.FontSize)
            {
                handlers.FontSize = styles.FontSize != null && styles.FontSize.Value > 0
                    ? Cached<FontSizeStyleDescriptor, FontSizeStyleHandler>(styles.FontSize, static d => new FontSizeStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultFontSize;
            }

            if (handlers.LineHeight.Descriptor != styles.LineHeight)
            {
                handlers.LineHeight = styles.LineHeight != null && styles.LineHeight.IsDeclared
                    ? Cached<LineHeightStyleDescriptor, LineHeightStyleHandler>(styles.LineHeight, static d => new LineHeightStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultLineHeight;
            }

            if (handlers.LetterSpacing.Descriptor != styles.LetterSpacing)
            {
                handlers.LetterSpacing = styles.LetterSpacing != null && styles.LetterSpacing.IsDeclared
                    ? Cached<LetterSpacingStyleDescriptor, LetterSpacingStyleHandler>(styles.LetterSpacing, static d => new LetterSpacingStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultLetterSpacing;
            }

            if (handlers.FontStyle.Descriptor != styles.FontStyle)
            {
                handlers.FontStyle = styles.FontStyle != null && styles.FontStyle.Value != FontStyle.Unset
                    ? Cached<FontStyleStyleDescriptor, FontStyleStyleHandler>(styles.FontStyle, static d => new FontStyleStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultFontStyle;
            }

            if (handlers.FontWeight.Descriptor != styles.FontWeight)
            {
                handlers.FontWeight = styles.FontWeight != null && styles.FontWeight.Value != FontWeight.Unset
                    ? Cached<FontWeightStyleDescriptor, FontWeightStyleHandler>(styles.FontWeight, static d => new FontWeightStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultFontWeight;
            }

            if (handlers.CornerRadius.Descriptor != styles.CornerRadius)
            {
                handlers.CornerRadius = styles.CornerRadius != null
                    ? Cached<CornerRadiusStyleDescriptor, CornerRadiusStyleHandler>(styles.CornerRadius, static d => new CornerRadiusStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultCornerRadius;
            }

            if (handlers.ColumnTemplate.Descriptor != styles.ColumnTemplate)
            {
                handlers.ColumnTemplate = styles.ColumnTemplate != null
                    ? Cached<ColumnTemplateStyleDescriptor, ColumnTemplateStyleHandler>(styles.ColumnTemplate, static d => new ColumnTemplateStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultColumnTemplate;
            }

            if (handlers.RowTemplate.Descriptor != styles.RowTemplate)
            {
                handlers.RowTemplate = styles.RowTemplate != null
                    ? Cached<RowTemplateStyleDescriptor, RowTemplateStyleHandler>(styles.RowTemplate, static d => new RowTemplateStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultRowTemplate;
            }

            if (handlers.Height.Descriptor != styles.Height)
            {
                handlers.Height = Cached<HeightStyleDescriptor, HeightStyleHandler>(styles.Height, static d => new HeightStyleHandler(d));
            }

            if (handlers.Layout.Descriptor != styles.Layout)
            {
                handlers.Layout = Cached<LayoutStyleDescriptor, LayoutStyleHandler>(styles.Layout, static d => new LayoutStyleHandler(d));
            }

            if (handlers.Margin.Descriptor != styles.Margin)
            {
                handlers.Margin = Cached<MarginStyleDescriptor, MarginStyleHandler>(styles.Margin, static d => new MarginStyleHandler(d));
            }

            if (handlers.Padding.Descriptor != styles.Padding)
            {
                handlers.Padding = Cached<PaddingStyleDescriptor, PaddingStyleHandler>(styles.Padding, static d => new PaddingStyleHandler(d));
            }

            if (handlers.TextAlign.Descriptor != styles.TextAlign)
            {
                handlers.TextAlign = styles.TextAlign != null
                    ? Cached<TextAlignStyleDescriptor, TextAlignStyleHandler>(styles.TextAlign, static d => new TextAlignStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTextAlign;
            }

            if (handlers.TextOverflow.Descriptor != styles.TextOverflow)
            {
                handlers.TextOverflow = styles.TextOverflow != null
                    ? Cached<TextOverflowStyleDescriptor, TextOverflowStyleHandler>(styles.TextOverflow, static d => new TextOverflowStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTextOverflow;
            }

            if (handlers.TextWrap.Descriptor != styles.TextWrap)
            {
                handlers.TextWrap = styles.TextWrap != null
                    ? Cached<TextWrapStyleDescriptor, TextWrapStyleHandler>(styles.TextWrap, static d => new TextWrapStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTextWrap;
            }

            if (handlers.Width.Descriptor != styles.Width)
            {
                handlers.Width = Cached<WidthStyleDescriptor, WidthStyleHandler>(styles.Width, static d => new WidthStyleHandler(d));
            }

            if (handlers.Left.Descriptor != styles.Left)
            {
                handlers.Left = Cached<LeftStyleDescriptor, LeftStyleHandler>(styles.Left, static d => new LeftStyleHandler(d));
            }

            if (handlers.Top.Descriptor != styles.Top)
            {
                handlers.Top = Cached<TopStyleDescriptor, TopStyleHandler>(styles.Top, static d => new TopStyleHandler(d));
            }

            if (handlers.Right.Descriptor != styles.Right)
            {
                handlers.Right = Cached<RightStyleDescriptor, RightStyleHandler>(styles.Right, static d => new RightStyleHandler(d));
            }

            if (handlers.Bottom.Descriptor != styles.Bottom)
            {
                handlers.Bottom = Cached<BottomStyleDescriptor, BottomStyleHandler>(styles.Bottom, static d => new BottomStyleHandler(d));
            }

            if (handlers.Dock.Descriptor != styles.Dock)
            {
                handlers.Dock = styles.Dock != null
                    ? Cached<DockStyleDescriptor, DockStyleHandler>(styles.Dock, static d => new DockStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultDock;
            }

            if (handlers.ColumnIndex.Descriptor != styles.ColumnIndex)
            {
                handlers.ColumnIndex = styles.ColumnIndex != null
                    ? Cached<ColumnIndexStyleDescriptor, ColumnIndexStyleHandler>(styles.ColumnIndex, static d => new ColumnIndexStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultColumnIndex;
            }

            if (handlers.RowIndex.Descriptor != styles.RowIndex)
            {
                handlers.RowIndex = styles.RowIndex != null
                    ? Cached<RowIndexStyleDescriptor, RowIndexStyleHandler>(styles.RowIndex, static d => new RowIndexStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultRowIndex;
            }

            if (handlers.ColumnSpan.Descriptor != styles.ColumnSpan)
            {
                handlers.ColumnSpan = styles.ColumnSpan != null
                    ? Cached<ColumnSpanStyleDescriptor, ColumnSpanStyleHandler>(styles.ColumnSpan, static d => new ColumnSpanStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultColumnSpan;
            }

            if (handlers.RowSpan.Descriptor != styles.RowSpan)
            {
                handlers.RowSpan = styles.RowSpan != null
                    ? Cached<RowSpanStyleDescriptor, RowSpanStyleHandler>(styles.RowSpan, static d => new RowSpanStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultRowSpan;
            }

            if (handlers.Cursor.Descriptor != styles.Cursor)
            {
                handlers.Cursor = styles.Cursor != null && styles.Cursor.Value != CursorKind.Unset
                    ? Cached<CursorStyleDescriptor, CursorStyleHandler>(styles.Cursor, static d => new CursorStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultCursor;
            }

            if (handlers.Transition.Descriptor != styles.Transition)
            {
                handlers.Transition = styles.Transition != null
                    ? Cached<TransitionStyleDescriptor, TransitionStyleHandler>(styles.Transition, static d => new TransitionStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTransition;
            }

            if (handlers.Overflow.Descriptor != styles.Overflow)
            {
                handlers.Overflow = styles.Overflow != null && styles.Overflow.Value != OverflowKind.Unset
                    ? Cached<OverflowStyleDescriptor, OverflowStyleHandler>(styles.Overflow, static d => new OverflowStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultOverflow;
            }

            if (handlers.Overscroll.Descriptor != styles.Overscroll)
            {
                handlers.Overscroll = styles.Overscroll != null && styles.Overscroll.IsDeclared
                    ? Cached<OverscrollStyleDescriptor, OverscrollStyleHandler>(styles.Overscroll, static d => new OverscrollStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultOverscroll;
            }

            if (handlers.PointerEvents.Descriptor != styles.PointerEvents)
            {
                handlers.PointerEvents = styles.PointerEvents != null && styles.PointerEvents.IsDeclared
                    ? Cached<PointerEventsStyleDescriptor, PointerEventsStyleHandler>(styles.PointerEvents, static d => new PointerEventsStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultPointerEvents;
            }

            if (handlers.ObjectPosition.Descriptor != styles.ObjectPosition)
            {
                handlers.ObjectPosition = styles.ObjectPosition != null && !styles.ObjectPosition.IsDefault
                    ? Cached<ObjectPositionStyleDescriptor, ObjectPositionStyleHandler>(styles.ObjectPosition, static d => new ObjectPositionStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultObjectPosition;
            }

            if (handlers.ObjectFit.Descriptor != styles.ObjectFit)
            {
                handlers.ObjectFit = styles.ObjectFit != null
                    ? Cached<ObjectFitStyleDescriptor, ObjectFitStyleHandler>(styles.ObjectFit, static d => new ObjectFitStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultObjectFit;
            }

            if (handlers.Animation.Descriptor != styles.Animation)
            {
                handlers.Animation = styles.Animation != null && styles.Animation.IsDeclared
                    ? Cached<AnimationStyleDescriptor, AnimationStyleHandler>(styles.Animation, static d => new AnimationStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultAnimation;
            }

            if (handlers.Anchor.Descriptor != styles.Anchor)
            {
                handlers.Anchor = styles.Anchor != null && !string.IsNullOrEmpty(styles.Anchor.Name)
                    ? Cached<AnchorStyleDescriptor, AnchorStyleHandler>(styles.Anchor, static d => new AnchorStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultAnchor;
            }

            if (handlers.AnchorPlacement.Descriptor != styles.AnchorPlacement)
            {
                handlers.AnchorPlacement = styles.AnchorPlacement != null
                    ? Cached<AnchorPlacementStyleDescriptor, AnchorPlacementStyleHandler>(styles.AnchorPlacement, static d => new AnchorPlacementStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultAnchorPlacement;
            }

            if (handlers.TextDecoration.Descriptor != styles.TextDecoration)
            {
                handlers.TextDecoration = styles.TextDecoration != null && styles.TextDecoration.IsDeclared
                    ? Cached<TextDecorationStyleDescriptor, TextDecorationStyleHandler>(styles.TextDecoration, static d => new TextDecorationStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTextDecoration;
            }

            if (handlers.Opacity.Descriptor != styles.Opacity)
            {
                handlers.Opacity = styles.Opacity != null && styles.Opacity.IsTransparent
                    ? Cached<OpacityStyleDescriptor, OpacityStyleHandler>(styles.Opacity, static d => new OpacityStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultOpacity;
            }

            if (handlers.Transform.Descriptor != styles.Transform)
            {
                handlers.Transform = styles.Transform != null && styles.Transform.IsDeclared
                    ? Cached<TransformStyleDescriptor, TransformStyleHandler>(styles.Transform, static d => new TransformStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTransform;
            }

            if (handlers.TransformOrigin.Descriptor != styles.TransformOrigin)
            {
                handlers.TransformOrigin = styles.TransformOrigin != null && !styles.TransformOrigin.IsDefault
                    ? Cached<TransformOriginStyleDescriptor, TransformOriginStyleHandler>(styles.TransformOrigin, static d => new TransformOriginStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTransformOrigin;
            }

            if (handlers.ContentAlign.Descriptor != styles.ContentAlign)
            {
                handlers.ContentAlign = styles.ContentAlign != null && styles.ContentAlign.IsDeclared
                    ? Cached<ContentAlignStyleDescriptor, ContentAlignStyleHandler>(styles.ContentAlign, static d => new ContentAlignStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultContentAlign;
            }

            if (handlers.ScrollBehavior.Descriptor != styles.ScrollBehavior)
            {
                handlers.ScrollBehavior = styles.ScrollBehavior != null && styles.ScrollBehavior.IsDeclared
                    ? Cached<ScrollBehaviorStyleDescriptor, ScrollBehaviorStyleHandler>(styles.ScrollBehavior, static d => new ScrollBehaviorStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultScrollBehavior;
            }

            if (handlers.Visibility.Descriptor != styles.Visibility)
            {
                handlers.Visibility = styles.Visibility != null && styles.Visibility.IsDeclared
                    ? Cached<VisibilityStyleDescriptor, VisibilityStyleHandler>(styles.Visibility, static d => new VisibilityStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultVisibility;
            }

            if (handlers.AspectRatio.Descriptor != styles.AspectRatio)
            {
                handlers.AspectRatio = styles.AspectRatio != null && styles.AspectRatio.IsDeclared
                    ? Cached<AspectRatioStyleDescriptor, AspectRatioStyleHandler>(styles.AspectRatio, static d => new AspectRatioStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultAspectRatio;
            }

            if (handlers.Gap.Descriptor != styles.Gap)
            {
                handlers.Gap = styles.Gap != null && styles.Gap.IsDeclared
                    ? Cached<GapStyleDescriptor, GapStyleHandler>(styles.Gap, static d => new GapStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultGap;
            }

            if (handlers.MinWidth.Descriptor != styles.MinWidth)
            {
                handlers.MinWidth = styles.MinWidth != null && styles.MinWidth.IsDeclared
                    ? Cached<MinWidthStyleDescriptor, MinWidthStyleHandler>(styles.MinWidth, static d => new MinWidthStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultMinWidth;
            }

            if (handlers.MaxWidth.Descriptor != styles.MaxWidth)
            {
                handlers.MaxWidth = styles.MaxWidth != null && styles.MaxWidth.IsDeclared
                    ? Cached<MaxWidthStyleDescriptor, MaxWidthStyleHandler>(styles.MaxWidth, static d => new MaxWidthStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultMaxWidth;
            }

            if (handlers.MinHeight.Descriptor != styles.MinHeight)
            {
                handlers.MinHeight = styles.MinHeight != null && styles.MinHeight.IsDeclared
                    ? Cached<MinHeightStyleDescriptor, MinHeightStyleHandler>(styles.MinHeight, static d => new MinHeightStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultMinHeight;
            }

            if (handlers.MaxHeight.Descriptor != styles.MaxHeight)
            {
                handlers.MaxHeight = styles.MaxHeight != null && styles.MaxHeight.IsDeclared
                    ? Cached<MaxHeightStyleDescriptor, MaxHeightStyleHandler>(styles.MaxHeight, static d => new MaxHeightStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultMaxHeight;
            }

            if (handlers.BoxShadow.Descriptor != styles.BoxShadow)
            {
                handlers.BoxShadow = styles.BoxShadow != null && styles.BoxShadow.IsDeclared
                    ? Cached<BoxShadowStyleDescriptor, BoxShadowStyleHandler>(styles.BoxShadow, static d => new BoxShadowStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultBoxShadow;
            }

            if (handlers.TextShadow.Descriptor != styles.TextShadow)
            {
                handlers.TextShadow = styles.TextShadow != null && styles.TextShadow.IsDeclared
                    ? Cached<TextShadowStyleDescriptor, TextShadowStyleHandler>(styles.TextShadow, static d => new TextShadowStyleHandler(d))
                    : VisualElementStylesHandlers.DefaultTextShadow;
            }

            if (handlers.ZIndex.Descriptor != styles.ZIndex)
            {
                handlers.ZIndex = styles.ZIndex != null && styles.ZIndex.Value != 0
                    ? Cached<ZIndexStyleDescriptor, ZIndexStyleHandler>(styles.ZIndex, static d => new ZIndexStyleHandler(d))
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

        private static THandler Cached<TDescriptor, THandler>(StyleDescriptor style,
            Func<TDescriptor, THandler> create)
            where TDescriptor : StyleDescriptor
            where THandler : class
            => HandlerCache<TDescriptor, THandler>.For((TDescriptor)style, create);

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
                        ? BorderStyleHandler.For(border)
                        : VisualElementStylesHandlers.DefaultBorder;
                    break;

                case StyleIdentifier.COLOR:
                    handlers.Color = ColorStyleHandler.For((ColorStyleDescriptor)style);
                    break;

                case StyleIdentifier.COLUMN_TEMPLATE:
                    handlers.ColumnTemplate = Cached<ColumnTemplateStyleDescriptor, ColumnTemplateStyleHandler>(style, static d => new ColumnTemplateStyleHandler(d));
                    break;

                case StyleIdentifier.CORNER_RADIUS:
                    handlers.CornerRadius = Cached<CornerRadiusStyleDescriptor, CornerRadiusStyleHandler>(style, static d => new CornerRadiusStyleHandler(d));
                    break;

                case StyleIdentifier.FILTER:
                    handlers.Filter = FilterStyleHandler.For((FilterStyleDescriptor)style);
                    break;

                case StyleIdentifier.BACKDROP_FILTER:
                    handlers.BackdropFilter = FilterStyleHandler.For((FilterStyleDescriptor)style);
                    break;

                case StyleIdentifier.FONT_FAMILY:
                    handlers.FontFamily = Cached<FontFamilyStyleDescriptor, FontFamilyStyleHandler>(style, static d => new FontFamilyStyleHandler(d));
                    break;

                case StyleIdentifier.FONT_SIZE:
                    handlers.FontSize = Cached<FontSizeStyleDescriptor, FontSizeStyleHandler>(style, static d => new FontSizeStyleHandler(d));
                    break;

                case StyleIdentifier.FONT_STYLE:
                    handlers.FontStyle = Cached<FontStyleStyleDescriptor, FontStyleStyleHandler>(style, static d => new FontStyleStyleHandler(d));
                    break;

                case StyleIdentifier.FONT_WEIGHT:
                    handlers.FontWeight = Cached<FontWeightStyleDescriptor, FontWeightStyleHandler>(style, static d => new FontWeightStyleHandler(d));
                    break;

                case StyleIdentifier.HEIGHT:
                    handlers.Height = Cached<HeightStyleDescriptor, HeightStyleHandler>(style, static d => new HeightStyleHandler(d));
                    break;

                case StyleIdentifier.LAYOUT:
                    handlers.Layout = Cached<LayoutStyleDescriptor, LayoutStyleHandler>(style, static d => new LayoutStyleHandler(d));
                    break;

                case StyleIdentifier.LEFT:
                    handlers.Left = Cached<LeftStyleDescriptor, LeftStyleHandler>(style, static d => new LeftStyleHandler(d));
                    break;

                case StyleIdentifier.TOP:
                    handlers.Top = Cached<TopStyleDescriptor, TopStyleHandler>(style, static d => new TopStyleHandler(d));
                    break;

                case StyleIdentifier.RIGHT:
                    handlers.Right = Cached<RightStyleDescriptor, RightStyleHandler>(style, static d => new RightStyleHandler(d));
                    break;

                case StyleIdentifier.BOTTOM:
                    handlers.Bottom = Cached<BottomStyleDescriptor, BottomStyleHandler>(style, static d => new BottomStyleHandler(d));
                    break;

                case StyleIdentifier.CURSOR:
                    handlers.Cursor = Cached<CursorStyleDescriptor, CursorStyleHandler>(style, static d => new CursorStyleHandler(d));
                    break;

                case StyleIdentifier.TRANSITION:
                    handlers.Transition = Cached<TransitionStyleDescriptor, TransitionStyleHandler>(style, static d => new TransitionStyleHandler(d));
                    break;

                case StyleIdentifier.ANIMATION:
                    handlers.Animation = Cached<AnimationStyleDescriptor, AnimationStyleHandler>(style, static d => new AnimationStyleHandler(d));
                    break;

                case StyleIdentifier.ANCHOR:
                    handlers.Anchor = Cached<AnchorStyleDescriptor, AnchorStyleHandler>(style, static d => new AnchorStyleHandler(d));
                    break;

                case StyleIdentifier.ANCHOR_PLACEMENT:
                    handlers.AnchorPlacement = Cached<AnchorPlacementStyleDescriptor, AnchorPlacementStyleHandler>(style, static d => new AnchorPlacementStyleHandler(d));
                    break;

                case StyleIdentifier.TEXT_DECORATION:
                    handlers.TextDecoration = Cached<TextDecorationStyleDescriptor, TextDecorationStyleHandler>(style, static d => new TextDecorationStyleHandler(d));
                    break;

                case StyleIdentifier.OPACITY:
                    handlers.Opacity = Cached<OpacityStyleDescriptor, OpacityStyleHandler>(style, static d => new OpacityStyleHandler(d));
                    break;

                case StyleIdentifier.TRANSFORM:
                    handlers.Transform = Cached<TransformStyleDescriptor, TransformStyleHandler>(style, static d => new TransformStyleHandler(d));
                    break;

                case StyleIdentifier.TRANSFORM_ORIGIN:
                    handlers.TransformOrigin = Cached<TransformOriginStyleDescriptor, TransformOriginStyleHandler>(style, static d => new TransformOriginStyleHandler(d));
                    break;

                case StyleIdentifier.CONTENT_ALIGN:
                    handlers.ContentAlign = Cached<ContentAlignStyleDescriptor, ContentAlignStyleHandler>(style, static d => new ContentAlignStyleHandler(d));
                    break;

                case StyleIdentifier.SCROLL_BEHAVIOR:
                    handlers.ScrollBehavior = Cached<ScrollBehaviorStyleDescriptor, ScrollBehaviorStyleHandler>(style, static d => new ScrollBehaviorStyleHandler(d));
                    break;

                case StyleIdentifier.VISIBILITY:
                    handlers.Visibility = Cached<VisibilityStyleDescriptor, VisibilityStyleHandler>(style, static d => new VisibilityStyleHandler(d));
                    break;

                case StyleIdentifier.ASPECT_RATIO:
                    handlers.AspectRatio = Cached<AspectRatioStyleDescriptor, AspectRatioStyleHandler>(style, static d => new AspectRatioStyleHandler(d));
                    break;

                case StyleIdentifier.GAP:
                    handlers.Gap = Cached<GapStyleDescriptor, GapStyleHandler>(style, static d => new GapStyleHandler(d));
                    break;

                case StyleIdentifier.MIN_WIDTH:
                    handlers.MinWidth = Cached<MinWidthStyleDescriptor, MinWidthStyleHandler>(style, static d => new MinWidthStyleHandler(d));
                    break;

                case StyleIdentifier.MAX_WIDTH:
                    handlers.MaxWidth = Cached<MaxWidthStyleDescriptor, MaxWidthStyleHandler>(style, static d => new MaxWidthStyleHandler(d));
                    break;

                case StyleIdentifier.MIN_HEIGHT:
                    handlers.MinHeight = Cached<MinHeightStyleDescriptor, MinHeightStyleHandler>(style, static d => new MinHeightStyleHandler(d));
                    break;

                case StyleIdentifier.MAX_HEIGHT:
                    handlers.MaxHeight = Cached<MaxHeightStyleDescriptor, MaxHeightStyleHandler>(style, static d => new MaxHeightStyleHandler(d));
                    break;

                case StyleIdentifier.BOX_SHADOW:
                    handlers.BoxShadow = Cached<BoxShadowStyleDescriptor, BoxShadowStyleHandler>(style, static d => new BoxShadowStyleHandler(d));
                    break;

                case StyleIdentifier.TEXT_SHADOW:
                    handlers.TextShadow = Cached<TextShadowStyleDescriptor, TextShadowStyleHandler>(style, static d => new TextShadowStyleHandler(d));
                    break;

                case StyleIdentifier.Z_INDEX:
                    handlers.ZIndex = Cached<ZIndexStyleDescriptor, ZIndexStyleHandler>(style, static d => new ZIndexStyleHandler(d));
                    break;

                case StyleIdentifier.OBJECT_FIT:
                    handlers.ObjectFit = Cached<ObjectFitStyleDescriptor, ObjectFitStyleHandler>(style, static d => new ObjectFitStyleHandler(d));
                    break;

                case StyleIdentifier.OBJECT_POSITION:
                    handlers.ObjectPosition = Cached<ObjectPositionStyleDescriptor, ObjectPositionStyleHandler>(style, static d => new ObjectPositionStyleHandler(d));
                    break;

                case StyleIdentifier.OVERFLOW:
                    handlers.Overflow = Cached<OverflowStyleDescriptor, OverflowStyleHandler>(style, static d => new OverflowStyleHandler(d));
                    break;

                case StyleIdentifier.OVERSCROLL_BEHAVIOR:
                    handlers.Overscroll = Cached<OverscrollStyleDescriptor, OverscrollStyleHandler>(style, static d => new OverscrollStyleHandler(d));
                    break;

                case StyleIdentifier.POINTER_EVENTS:
                    handlers.PointerEvents = Cached<PointerEventsStyleDescriptor, PointerEventsStyleHandler>(style, static d => new PointerEventsStyleHandler(d));
                    break;

                case StyleIdentifier.DOCK:
                    handlers.Dock = Cached<DockStyleDescriptor, DockStyleHandler>(style, static d => new DockStyleHandler(d));
                    break;

                case StyleIdentifier.COLUMN_INDEX:
                    handlers.ColumnIndex = Cached<ColumnIndexStyleDescriptor, ColumnIndexStyleHandler>(style, static d => new ColumnIndexStyleHandler(d));
                    break;

                case StyleIdentifier.ROW_INDEX:
                    handlers.RowIndex = Cached<RowIndexStyleDescriptor, RowIndexStyleHandler>(style, static d => new RowIndexStyleHandler(d));
                    break;

                case StyleIdentifier.COLUMN_SPAN:
                    handlers.ColumnSpan = Cached<ColumnSpanStyleDescriptor, ColumnSpanStyleHandler>(style, static d => new ColumnSpanStyleHandler(d));
                    break;

                case StyleIdentifier.ROW_SPAN:
                    handlers.RowSpan = Cached<RowSpanStyleDescriptor, RowSpanStyleHandler>(style, static d => new RowSpanStyleHandler(d));
                    break;

                case StyleIdentifier.LETTER_SPACING:
                    handlers.LetterSpacing = Cached<LetterSpacingStyleDescriptor, LetterSpacingStyleHandler>(style, static d => new LetterSpacingStyleHandler(d));
                    break;

                case StyleIdentifier.LINE_HEIGHT:
                    handlers.LineHeight = Cached<LineHeightStyleDescriptor, LineHeightStyleHandler>(style, static d => new LineHeightStyleHandler(d));
                    break;

                case StyleIdentifier.MARGIN:
                    handlers.Margin = Cached<MarginStyleDescriptor, MarginStyleHandler>(style, static d => new MarginStyleHandler(d));
                    break;

                case StyleIdentifier.PADDING:
                    handlers.Padding = Cached<PaddingStyleDescriptor, PaddingStyleHandler>(style, static d => new PaddingStyleHandler(d));
                    break;

                case StyleIdentifier.ROW_TEMPLATE:
                    handlers.RowTemplate = Cached<RowTemplateStyleDescriptor, RowTemplateStyleHandler>(style, static d => new RowTemplateStyleHandler(d));
                    break;

                case StyleIdentifier.TEXT_ALIGN:
                    handlers.TextAlign = Cached<TextAlignStyleDescriptor, TextAlignStyleHandler>(style, static d => new TextAlignStyleHandler(d));
                    break;

                case StyleIdentifier.TEXT_OVERFLOW:
                    handlers.TextOverflow = Cached<TextOverflowStyleDescriptor, TextOverflowStyleHandler>(style, static d => new TextOverflowStyleHandler(d));
                    break;

                case StyleIdentifier.TEXT_WRAP:
                    handlers.TextWrap = Cached<TextWrapStyleDescriptor, TextWrapStyleHandler>(style, static d => new TextWrapStyleHandler(d));
                    break;

                case StyleIdentifier.WIDTH:
                    handlers.Width = Cached<WidthStyleDescriptor, WidthStyleHandler>(style, static d => new WidthStyleHandler(d));
                    break;

                default:
                    break;
            }
        }

    }
}
