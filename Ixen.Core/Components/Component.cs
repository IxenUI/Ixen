using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Components
{
    public abstract class Component : IBoundModel
    {
        private bool _initialized;
        private bool _isStateDirty;
        private bool _rendering;

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
        protected internal TView View { get; private set; } = new() { TypeName = typeof(TView).Name };
        internal override VisualElement GetVisualElement() => View;
     }
}
