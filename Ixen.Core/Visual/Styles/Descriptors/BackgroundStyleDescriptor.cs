using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BackgroundStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BACKGROUND;

        public const float UNSET_POSITION = BackgroundLayer.UNSET_POSITION;

        public string Color { get; set; } = null;

        public List<BackgroundLayer> Layers { get; set; } = new List<BackgroundLayer>();

        internal BackgroundLayer First => Layers.Count > 0 ? Layers[0] : null;

        internal bool HasLayers => Layers.Count > 0;

        private BackgroundLayer Ensure()
        {
            if (Layers.Count == 0)
            {
                Layers.Add(new BackgroundLayer());
            }

            return Layers[0];
        }

        public string ImageUrl
        {
            get => First?.ImageUrl;
            set => Ensure().ImageUrl = value;
        }

        public Gradient Gradient
        {
            get => First?.Gradient;
            set => Ensure().Gradient = value;
        }

        public bool RepeatX
        {
            get => First != null && First.RepeatX;
            set => Ensure().RepeatX = value;
        }

        public bool RepeatY
        {
            get => First != null && First.RepeatY;
            set => Ensure().RepeatY = value;
        }

        public ObjectFit Fit
        {
            get => First == null ? ObjectFit.None : First.Fit;
            set => Ensure().Fit = value;
        }

        public float PositionX
        {
            get => First == null ? UNSET_POSITION : First.PositionX;
            set => Ensure().PositionX = value;
        }

        public float PositionY
        {
            get => First == null ? UNSET_POSITION : First.PositionY;
            set => Ensure().PositionY = value;
        }

        public bool IsScaled => First != null && First.IsScaled;

        public bool HasPosition => First != null && First.HasPosition;

        public float AnchorX => First == null ? 0f : First.AnchorX;

        public float AnchorY => First == null ? 0f : First.AnchorY;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
        {
            string color = string.IsNullOrWhiteSpace(Color)
                ? ""
                : $"{nameof(Color)} = {SourceOf(Color)}, ";

            string layers = Layers.Count == 0
                ? ""
                : $"{nameof(Layers)} = new() {{ "
                    + string.Join(", ", Layers.Select(l => l.ToSource()))
                    + "} ";

            return $"new {nameof(BackgroundStyleDescriptor)} " +
                "{ " +
                    color +
                    layers +
                "}";
        }
    }
}
