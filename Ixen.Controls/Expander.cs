using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class Expander : VisualElement
    {
        public const string HEADER = "ExpanderHeader";
        public const string MARK = "ExpanderMark";
        public const string TITLE = "ExpanderTitle";
        public const string CONTENT = "ExpanderContent";
        public const string EXPANDED = "expanded";

        public event EventHandler<EventArgs> ExpandedChanged;

        private readonly ExpanderHeader _header;
        private readonly VisualElement _mark;
        private readonly VisualElement _title;
        private readonly VisualElement _content;

        private bool _expanded;

        public Expander()
        {
            TypeName = nameof(Expander);
            Role = AccessibleRole.Group;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _header = new ExpanderHeader(this) { TypeName = HEADER };
            _header.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _mark = new Path
            {
                TypeName = MARK,
                Role = AccessibleRole.Presentation,
                Data = Marks.DOWN
            };

            _title = new VisualElement { TypeName = TITLE };

            _header.AddChildren(_mark, _title);

            var content = new VisualElement { TypeName = CONTENT };
            content.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            AddChild(_header);
            AddChild(content);

            _content = content;

            Apply();
        }

        protected override VisualElement ContentHost => _content ?? this;

        public VisualElement HeaderElement => _header;

        public VisualElement Content => _content;

        public string Header
        {
            get => _title.Text;
            set => _title.Text = value;
        }

        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded == value)
                {
                    return;
                }

                _expanded = value;

                Apply();
            }
        }

        private void Apply()
        {
            foreach (VisualElement element in new[] { this, _header, _mark })
            {
                element.ToggleState(EXPANDED, _expanded);
            }

            _content.Styles.Height = _expanded
                ? new HeightStyleDescriptor { Unit = SizeUnit.Content }
                : new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _content.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _expanded ? Visibility.Visible : Visibility.Hidden
            };

            _content.Invalidate();
        }

        internal void Interact(bool expanded)
        {
            if (!IsEnabled || expanded == _expanded)
            {
                return;
            }

            Expanded = expanded;

            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
