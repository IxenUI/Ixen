using Ixen.Core.Rendering;
using System;

namespace Ixen.Core.Visual
{
    public class Image : VisualElement
    {
        public const string LOADING = "loading";
        public const string FAILED = "failed";

        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> Failed;

        private string _source;
        private ImageStatus _status;

        public string Source
        {
            get => _source;
            set
            {
                if (_source == value)
                {
                    return;
                }

                _source = value;
                _status = ImageStatus.None;
                InvalidateLayout();
            }
        }

        public bool IsLoading => _status == ImageStatus.Loading;

        public bool HasFailed => _status == ImageStatus.Failed;

        internal float NaturalWidth { get; set; }
        internal float NaturalHeight { get; set; }

        protected internal override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            base.OnPrepass(viewportWidth, viewportHeight);

            ImageStatus status = string.IsNullOrEmpty(_source) || !(Host is IxenSurface surface)
                ? ImageStatus.None
                : surface.ImageStatusOf(_source);

            if (status == _status)
            {
                return;
            }

            _status = status;

            ToggleState(LOADING, status == ImageStatus.Loading);
            ToggleState(FAILED, status == ImageStatus.Failed);

            EventHandler<EventArgs> handler = status == ImageStatus.Ready ? Loaded
                : status == ImageStatus.Failed ? Failed
                : null;

            if (handler != null)
            {
                Host?.Post(() => handler(this, EventArgs.Empty));
            }
        }
    }
}
