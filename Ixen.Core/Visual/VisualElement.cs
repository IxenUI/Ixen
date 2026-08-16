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

        public bool Focusable { get; set; }
        public bool Scrollable { get; set; }

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

        internal VisualElement Parent { get; private set; }
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

        public void AddChild(VisualElement element)
        {
            element.Parent = this;
            element.Invalidate();
            Children.Add(element);
            ComputeChildrenIndexes();
        }

        public void AddChildren(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                element.Parent = this;
                element.Invalidate();
                Children.Add(element);
            }

            ComputeChildrenIndexes();
        }

        public void RemoveChild(VisualElement element)
        {
            if (Children.Remove(element))
            {
                element.Parent = null;
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
