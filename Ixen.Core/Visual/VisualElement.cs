using Ixen.Core.Input;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public class VisualElement : BoxedElement
    {
        public event EventHandler<PointerEventArgs> PointerDown;
        public event EventHandler<PointerEventArgs> PointerUp;
        public event EventHandler<PointerEventArgs> PointerMove;
        public event EventHandler<PointerEventArgs> PointerClick;
        public event EventHandler<PointerEventArgs> PointerEnter;
        public event EventHandler<PointerEventArgs> PointerLeave;
        public event EventHandler<WheelEventArgs> PointerWheel;
        public event EventHandler<PointerEventArgs> PointerDoubleClick;
        public event EventHandler<PointerEventArgs> PointerLongPress;
        public event EventHandler<DragEventArgs> PointerDragStart;
        public event EventHandler<DragEventArgs> PointerDrag;
        public event EventHandler<DragEventArgs> PointerDragEnd;

        internal void RaisePointerDown(PointerEventArgs args) => PointerDown?.Invoke(this, args);
        internal void RaisePointerUp(PointerEventArgs args) => PointerUp?.Invoke(this, args);
        internal void RaisePointerMove(PointerEventArgs args) => PointerMove?.Invoke(this, args);
        internal void RaisePointerClick(PointerEventArgs args) => PointerClick?.Invoke(this, args);
        internal void RaisePointerEnter(PointerEventArgs args) => PointerEnter?.Invoke(this, args);
        internal void RaisePointerLeave(PointerEventArgs args) => PointerLeave?.Invoke(this, args);
        internal void RaisePointerWheel(WheelEventArgs args) => PointerWheel?.Invoke(this, args);
        internal void RaisePointerDoubleClick(PointerEventArgs args) => PointerDoubleClick?.Invoke(this, args);
        internal void RaisePointerLongPress(PointerEventArgs args) => PointerLongPress?.Invoke(this, args);
        internal void RaisePointerDragStart(DragEventArgs args) => PointerDragStart?.Invoke(this, args);
        internal void RaisePointerDrag(DragEventArgs args) => PointerDrag?.Invoke(this, args);
        internal void RaisePointerDragEnd(DragEventArgs args) => PointerDragEnd?.Invoke(this, args);

        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<TextInputEventArgs> TextInput;
        public event EventHandler<EventArgs> GotFocus;
        public event EventHandler<EventArgs> LostFocus;

        internal void RaiseKeyDown(KeyEventArgs args) => KeyDown?.Invoke(this, args);
        internal void RaiseKeyUp(KeyEventArgs args) => KeyUp?.Invoke(this, args);
        internal void RaiseTextInput(TextInputEventArgs args) => TextInput?.Invoke(this, args);
        internal void RaiseGotFocus() => GotFocus?.Invoke(this, EventArgs.Empty);
        internal void RaiseLostFocus() => LostFocus?.Invoke(this, EventArgs.Empty);

        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                ToggleState(Ixen.Core.Visual.Styles.StyleStates.DISABLED, !value);
            }
        }

        public bool IsEnabled
        {
            get
            {
                for (VisualElement element = this; element != null; element = element.Parent)
                {
                    if (!element._enabled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal bool IsHidden
            => StylesHandlers != null
                && StylesHandlers.Visibility.Descriptor.Value == Ixen.Core.Visual.Styles.Descriptors.Visibility.Hidden;

        public void Focus() => Host?.Focus(this);

        public void PerformClick() => Input.PointerDispatcher.Invoke(this);

        public bool Focusable { get; set; }
        public bool Modal { get; set; }

        public Accessibility.AccessibleRole Role { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        private bool _scrollable;

        public bool Scrollable
        {
            get => _scrollable;
            set
            {
                if (_scrollable == value)
                {
                    return;
                }

                _scrollable = value;

                if (value)
                {
                    EnsureScrollbar(true);
                    EnsureScrollbar(false);
                }

                InvalidateLayout();
            }
        }

        private float _scrollX;
        private float _scrollY;

        public float ScrollX
        {
            get => _scrollX;
            set => SetScroll(value, _scrollY);
        }

        public float ScrollY
        {
            get => _scrollY;
            set => SetScroll(_scrollX, value);
        }

        internal float ScrollExtentWidth { get; set; }
        internal float ScrollExtentHeight { get; set; }

        internal float LayoutOffsetX { get; set; }
        internal float LayoutOffsetY { get; set; }

        internal Components.Component Owner { get; set; }

        internal float MaxScrollX
        {
            get
            {
                float value = ScrollExtentWidth - ContentWidth;
                return value < 0 ? 0 : value;
            }
        }

        internal float MaxScrollY
        {
            get
            {
                float value = ScrollExtentHeight - ContentHeight;
                return value < 0 ? 0 : value;
            }
        }

        public void ScrollBy(float deltaX, float deltaY)
            => SetScroll(_scrollX + deltaX, _scrollY + deltaY);

        private void SetScroll(float x, float y)
        {
            if (x < 0)
            {
                x = 0;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (_scrollX == x && _scrollY == y)
            {
                return;
            }

            _scrollX = x;
            _scrollY = y;

            InvalidateLayout();
        }

        internal void ClampScroll()
        {
            _scrollX = Clamp(_scrollX, MaxScrollX);
            _scrollY = Clamp(_scrollY, MaxScrollY);
        }

        private static float Clamp(float value, float max)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > max ? max : value;
        }

        internal int ChildIndex { get; set; }
        internal List<VisualElement> Children { get; private set; } = new();

        public System.Collections.Generic.IReadOnlyList<VisualElement> ChildElements => Children;

        internal int GridColumn { get; set; }
        internal int GridRow { get; set; }
        internal int GridColumnSpan { get; set; } = 1;
        internal int GridRowSpan { get; set; } = 1;

        private float[] _gridColumns;
        private float[] _gridRows;
        private List<string> _textLines;

        internal List<string> TextLines => _textLines;

        internal List<string> EnsureTextLines()
        {
            if (_textLines == null)
            {
                _textLines = new List<string>();
            }
            else
            {
                _textLines.Clear();
            }

            return _textLines;
        }

        private TextLayoutCache _textLayout;

        internal TextLayoutCache TextLayout => _textLayout;

        internal TextLayoutCache EnsureTextLayout()
        {
            if (_textLayout == null)
            {
                _textLayout = new TextLayoutCache();
            }

            return _textLayout;
        }

        internal float[] GridColumns => _gridColumns;
        internal float[] GridRows => _gridRows;

        internal float[] EnsureGridColumns(int count)
        {
            if (_gridColumns == null || _gridColumns.Length != count)
            {
                _gridColumns = new float[count];
            }

            return _gridColumns;
        }

        internal float[] EnsureGridRows(int count)
        {
            if (_gridRows == null || _gridRows.Length != count)
            {
                _gridRows = new float[count];
            }

            return _gridRows;
        }

        public VisualElement Parent { get; private set; }
        internal DimensionalElement Clip { get; set; }
        internal bool MustRefreshStyles { get; set; } = true;
        internal bool IsLayoutDirty { get; private set; } = true;

        private string _text;

        public virtual string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                {
                    return;
                }

                _text = value;
                InvalidateLayout();
            }
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public VisualElementStylesDescriptors Styles { get; set; } = new();
        internal VisualElementStylesHandlers StylesHandlers { get; set; } = new();
        public List<string> Classes { get; set; } = new ();

        private List<VisualElement> _chrome;

        internal List<VisualElement> Chrome => _chrome;
        internal bool HasChrome => _chrome != null && _chrome.Count > 0;

        internal Scrollbar EnsureScrollbar(bool vertical)
        {
            if (_chrome != null)
            {
                foreach (VisualElement element in _chrome)
                {
                    if (element is Scrollbar existing && existing.IsVertical == vertical)
                    {
                        return existing;
                    }
                }
            }

            var bar = new Scrollbar(vertical);
            AddChrome(bar);

            return bar;
        }

        public IElementHost Host { get; private set; }

        internal void AttachHost(IElementHost host)
        {
            if (Host == host)
            {
                return;
            }

            IElementHost previous = Host;

            Host = host;
            OnHostChanged();

            if (host == null)
            {
                previous?.ElementDetached(this);
            }

            Owner?.HostChanged();

            foreach (VisualElement child in Children)
            {
                child.AttachHost(host);
            }

            if (_chrome == null)
            {
                return;
            }

            foreach (VisualElement chrome in _chrome)
            {
                chrome.AttachHost(host);
            }
        }

        internal void DetachHost() => AttachHost(null);

        internal virtual void OnHostChanged()
        {
            if (Host == null)
            {
                _animations?.Stop();
            }
        }

        internal bool IsOverlay
            => Parent != null
                && StylesHandlers != null
                && Computers.MeasureComputer.LayoutTypeOf(this)
                    == Ixen.Core.Visual.Styles.Descriptors.LayoutType.Fixed;

        public VisualElement AnchorElement { get; set; }

        internal bool IsAnchored
            => AnchorElement != null
                || (StylesHandlers != null
                    && !string.IsNullOrEmpty(StylesHandlers.Anchor.Descriptor.Name));

        internal bool HasFilter
            => StylesHandlers != null && StylesHandlers.Filter.Descriptor.IsDeclared;

        internal bool HasTransform
        {
            get
            {
                if (StylesHandlers == null)
                {
                    return false;
                }

                if (StylesHandlers.Transform.Descriptor.IsDeclared)
                {
                    return true;
                }

                Styles.Descriptors.TransformStyleDescriptor animated = AnimatedTransform();

                return animated != null && animated.IsDeclared;
            }
        }

        private List<VisualElement> _overlays;

        internal List<VisualElement> Overlays => _overlays ?? (_overlays = new List<VisualElement>());

        internal bool HasOverlays => _overlays != null && _overlays.Count > 0;

        private ElementAnimations _animations;

        internal bool HasAnimations => _animations != null;

        internal ElementAnimations Animations
            => _animations ?? (_animations = new ElementAnimations(this));

        internal Rendering.Brush AnimatedBrush(string identifier)
        {
            ColorTransition transition = _animations?.For(identifier);

            return transition != null && (transition.Running || transition.Held)
                ? transition.Brush
                : null;
        }

        public event EventHandler<TransitionEventArgs> TransitionEnded;

        internal bool HasTransitionEndedHandler => TransitionEnded != null;

        internal void RaiseTransitionEnded(TransitionEventArgs args) => TransitionEnded?.Invoke(this, args);

        internal Styles.Descriptors.SizeStyleDescriptor AnimatedSize(string identifier)
        {
            SizeTransition transition = _animations?.SizeIfAny(identifier);

            return transition != null && (transition.Running || transition.Held) ? transition.Descriptor : null;
        }

        internal Styles.Descriptors.TransformStyleDescriptor AnimatedTransform()
        {
            TransformTransition transition = _animations?.Transform;

            return transition != null && (transition.Running || transition.Held)
                ? transition.Descriptor
                : null;
        }

        internal Rendering.Pen AnimatedPen(string identifier, Rendering.Pen source)
        {
            ColorTransition transition = _animations?.For(identifier);

            return transition != null && (transition.Running || transition.Held)
                ? transition.PenLike(source)
                : null;
        }

        internal void AddChrome(VisualElement element)
        {
            if (_chrome == null)
            {
                _chrome = new List<VisualElement>();
            }

            element.Parent = this;
            element.MarkStylesDirty();
            element.AttachHost(Host);
            _chrome.Add(element);
        }

        protected virtual VisualElement ContentHost => this;

        public void AddChild(VisualElement element)
        {
            if (ContentHost != this && ContentHost != element)
            {
ContentHost.AddChild(element);
                return;
            }

            element.Parent = this;
            element.Invalidate();
            element.AttachHost(Host);
            Children.Add(element);
            ComputeChildrenIndexes();
        }

        public void AddChildren(params VisualElement[] elements)
        {
            if (ContentHost != this)
            {
                ContentHost.AddChildren(elements);
                return;
            }

            foreach (VisualElement element in elements)
            {
                element.Parent = this;
                element.Invalidate();
                element.AttachHost(Host);
                Children.Add(element);
            }

            ComputeChildrenIndexes();
        }

        internal void Adopt(VisualElement element)
        {
            element.Parent = this;
            element.Invalidate();
            element.AttachHost(Host);
        }

        internal void Release(VisualElement element)
        {
            element.Parent = null;
            element.DetachHost();
        }

        internal void SpliceChildren(int offset, int removeCount, List<VisualElement> insert)
        {
            if (ContentHost != this)
            {
                ContentHost.SpliceChildren(offset, removeCount, insert);
                return;
            }

            if (removeCount > 0)
            {
                Children.RemoveRange(offset, removeCount);
            }

            if (insert != null && insert.Count > 0)
            {
                Children.InsertRange(offset, insert);
            }

            ComputeChildrenIndexes();
            InvalidateLayout();
        }

        public void InsertChild(int index, VisualElement element)
        {
            if (ContentHost != this && ContentHost != element)
            {
ContentHost.InsertChild(index, element);
                return;
            }

            element.Parent = this;
            element.Invalidate();
            element.AttachHost(Host);

            Children.Insert(index < 0 || index > Children.Count ? Children.Count : index, element);
            ComputeChildrenIndexes();
        }

        public void RemoveChildAt(int index)
        {
            if (ContentHost != this)
            {
                ContentHost.RemoveChildAt(index);
                return;
            }

            if (index < 0 || index >= Children.Count)
            {
                return;
            }

            VisualElement element = Children[index];

            Children.RemoveAt(index);
            element.Parent = null;
            element.DetachHost();

            InvalidateLayout();
            ComputeChildrenIndexes();
        }

        public void RemoveChild(VisualElement element)
        {
            if (ContentHost != this)
            {
                ContentHost.RemoveChild(element);
                return;
            }

            if (Children.Remove(element))
            {
                element.Parent = null;
                element.DetachHost();
                InvalidateLayout();
            }

            ComputeChildrenIndexes();
        }

        private void ComputeChildrenIndexes()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].ChildIndex = i;
            }
        }

        public List<string> States { get; private set; } = new();

        public bool HasState(string name)
            => name != null && States.Contains(name);

        public void AddState(string name)
        {
            if (name == null || States.Contains(name))
            {
                return;
            }

            States.Add(name);
            Invalidate();
        }

        public void RemoveState(string name)
        {
            if (name == null || !States.Remove(name))
            {
                return;
            }

            Invalidate();
        }

        public void ToggleState(string name, bool present)
        {
            if (present)
            {
                AddState(name);
                return;
            }

            RemoveState(name);
        }

        public bool HasClass(string name)
            => name != null && Classes.Contains(name);

        public void AddClass(string name)
        {
            if (name == null || Classes.Contains(name))
            {
                return;
            }

            Classes.Add(name);
            Invalidate();
        }

        public void RemoveClass(string name)
        {
            if (name == null || !Classes.Remove(name))
            {
                return;
            }

            Invalidate();
        }

        public void ToggleClass(string name, bool present)
        {
            if (present)
            {
                AddClass(name);
                return;
            }

            RemoveClass(name);
        }

        public VisualElement FindByName(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (Name == name)
            {
                return this;
            }

            foreach (VisualElement child in Children)
            {
                VisualElement found = child.FindByName(name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public void Invalidate()
        {
            MarkStylesDirty();
            InvalidateLayout();
        }

        public void InvalidateLayout()
        {
            IsLayoutDirty = true;

            for (VisualElement parent = Parent; parent != null && !parent.IsLayoutDirty; parent = parent.Parent)
            {
                parent.IsLayoutDirty = true;
            }
        }

        internal void ClearLayoutDirty()
        {
            IsLayoutDirty = false;

            foreach (VisualElement child in Children)
            {
                child.ClearLayoutDirty();
            }
        }

        private void MarkStylesDirty()
        {
            MustRefreshStyles = true;

            foreach (VisualElement child in Children)
            {
                child.MarkStylesDirty();
            }
        }
    }
}
