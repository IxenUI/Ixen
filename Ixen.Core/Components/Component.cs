using Ixen.Core.Visual;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Ixen.Core.Components
{
    public abstract class Component : IBoundModel
    {
        private bool _initialized;
        private bool _isStateDirty;
        private bool _rendering;
        private List<Slot> _slots;
        private bool _attached;

        internal abstract VisualElement GetVisualElement();

        internal bool IsStateDirty => _isStateDirty;

        public VisualElement Initialize()
        {
            VisualElement element = GetVisualElement();

            if (_initialized)
            {
                return element;
            }

            _initialized = true;

            if (element != null)
            {
                element.Owner = this;
            }

            OnInitialized();
            RenderPass();

            return element;
        }

        public VisualElement Content => ContentFor(null);

        public VisualElement ContentFor(string name)
        {
            if (_slots == null)
            {
                _slots = new List<Slot>();
                Collect(GetVisualElement());
            }

            if (!string.IsNullOrEmpty(name))
            {
                foreach (Slot named in _slots)
                {
                    if (named.Name == name)
                    {
                        return named;
                    }
                }

                throw new InvalidOperationException(
                    $"{GetType().Name}'s view declares no <Slot> named '{name}'. "
                    + $"Declare an element such as '{name}<Slot> {{}}' where that content should go.");
            }

            if (_slots.Count == 1)
            {
                return _slots[0];
            }

            foreach (Slot unnamed in _slots)
            {
                if (string.IsNullOrEmpty(unnamed.Name))
                {
                    return unnamed;
                }
            }

            if (_slots.Count > 1)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}'s view declares several named <Slot>s and no unnamed one, so there is "
                    + "nowhere for content that names no slot. Add an unnamed '<Slot> {}' for it.");
            }

            throw new InvalidOperationException(
                $"{GetType().Name} was given content, but its view declares no <Slot> to receive it. "
                + "Declare an element such as 'content<Slot> {}' where the content should go.");
        }

        private void Collect(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (element is Slot slot)
            {
                foreach (Slot known in _slots)
                {
                    if (known.Name == slot.Name)
                    {
                        string which = string.IsNullOrEmpty(slot.Name)
                            ? "more than one unnamed <Slot>"
                            : $"more than one <Slot> named '{slot.Name}'";

                        throw new InvalidOperationException(
                            $"{GetType().Name}'s view declares {which}, so there is no way to tell "
                            + "which one a piece of content belongs to.");
                    }
                }

                _slots.Add(slot);
            }

            foreach (VisualElement child in element.Children)
            {
                if (child.Owner != null && child.Owner != this)
                {
                    continue;
                }

                Collect(child);
            }
        }

        protected virtual void OnInitialized()
        { }

        protected virtual void OnAttached()
        { }

        protected virtual void OnDetached()
        { }

        internal void HostChanged()
        {
            bool attached = GetVisualElement()?.Host != null;

            if (attached == _attached)
            {
                return;
            }

            _attached = attached;

            if (attached)
            {
                OnAttached();
                return;
            }

            OnDetached();
        }

        protected virtual void Render()
        { }

        protected void SetState(Action change)
        {
            EnsureNotRendering();
            change?.Invoke();
            SetState();
        }

        protected void SetState()
        {
            EnsureNotRendering();

            _isStateDirty = true;
            GetVisualElement()?.InvalidateLayout();
        }

        private void EnsureNotRendering()
        {
            if (!_rendering)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{GetType().Name}.SetState() was called while the component was rendering. "
                + "That marks the component dirty again on every pass, so it would repaint forever. "
                + "Compute the value inside Render, or raise the change from an event handler instead.");
        }

        void IBoundModel.SetState() => SetState();

        internal void RenderIfDirty()
        {
            if (!_isStateDirty)
            {
                return;
            }

            _isStateDirty = false;
            RenderPass();
        }

        private void RenderPass()
        {
            _rendering = true;

            try
            {
                ApplyBindings();
                Render();
            }
            finally
            {
                _rendering = false;
            }
        }

        private void ApplyBindings()
        {
            if (GetVisualElement() is IBoundView bound)
            {
                bound.Bind(this);
            }
        }
    }

    public class Component<TView> : Component
        where TView : VisualElement, new()
    {
        protected internal TView View { get; private set; } = CreateView();

        internal override VisualElement GetVisualElement() => View;

        private static TView CreateView()
        {
            try
            {
                return new TView { TypeName = typeof(TView).Name };
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
