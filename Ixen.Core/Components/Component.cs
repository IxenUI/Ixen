using Ixen.Core.Visual;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Ixen.Core.Components
{
    public abstract class Component : IBoundModel
    {
        private bool _initialized;
        private bool _isStateDirty;
        private bool _rendering;
        private VisualElement _content;

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

        public VisualElement Content
        {
            get
            {
                if (_content == null)
                {
                    _content = FindSlot();
                }

                return _content;
            }
        }

        private VisualElement FindSlot()
        {
            Slot found = null;

            Collect(GetVisualElement(), ref found);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} was given content, but its view declares no <Slot> to receive it. "
                    + "Declare an element such as 'content<Slot> {}' where the content should go.");
            }

            return found;
        }

        private void Collect(VisualElement element, ref Slot found)
        {
            if (element == null)
            {
                return;
            }

            if (element is Slot slot)
            {
                if (found != null)
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name}'s view declares more than one <Slot>, so there is no way to tell "
                        + "which one its content belongs to.");
                }

                found = slot;
            }

            foreach (VisualElement child in element.Children)
            {
                if (child.Owner != null && child.Owner != this)
                {
                    continue;
                }

                Collect(child, ref found);
            }
        }

        protected virtual void OnInitialized()
        { }

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
