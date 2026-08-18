namespace Ixen.Core.Visual
{
    public class Image : VisualElement
    {
        private string _source;

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
                InvalidateLayout();
            }
        }

        internal float NaturalWidth { get; set; }
        internal float NaturalHeight { get; set; }
    }
}
