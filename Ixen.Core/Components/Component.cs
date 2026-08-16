using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Components
{
    public abstract class Component : IBoundModel
    {
        private bool _initialized;
        private bool _isStateDirty;

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
            ApplyBindings();
            Render();

            return element;
        }

        protected virtual void OnInitialized()
        { }

        protected virtual void Render()
        { }

        protected void SetState(Action change)
        {
            change?.Invoke();
            SetState();
        }

        protected void SetState()
        {
            _isStateDirty = true;
            GetVisualElement()?.InvalidateLayout();
        }

        void IBoundModel.SetState() => SetState();

        internal void RenderIfDirty()
        {
            if (!_isStateDirty)
            {
                return;
            }

            _isStateDirty = false;
            ApplyBindings();
            Render();
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
