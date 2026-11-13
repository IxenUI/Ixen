using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class Tooltip : VisualElement
    {
        public const string PANEL = "TooltipPanel";

        private const int DEFAULT_DELAY = 500;

        private readonly VisualElement _panel;

        private VisualElement _target;
        private string _lent;
        private IDisposable _pending;
        private bool _shown;
        private bool _suppressed;

        public Tooltip()
        {
            TypeName = nameof(Tooltip);
            Role = AccessibleRole.Presentation;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = AnchorSide.Above,
                Align = AnchorAlign.Center
            };

            var panel = new VisualElement { TypeName = PANEL };

            panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            panel.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            panel.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            AddChild(panel);

            _panel = panel;

            Apply();
        }

        public string Caption
        {
            get => _panel.Text;
            set
            {
                _panel.Text = value;

                if (_target != null && OwnsDescription(_target))
                {
                    Lend(_target);
                }
            }
        }

        private bool OwnsDescription(VisualElement target)
            => string.IsNullOrEmpty(target.Description) || (_lent != null && target.Description == _lent);

        private void Lend(VisualElement target)
        {
            _lent = Caption;
            target.Description = _lent;
        }

        public int Delay { get; set; } = DEFAULT_DELAY;

        public VisualElement Panel => _panel;

        public bool IsShown => _shown;

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            Release();

            if (Host == null)
            {
                Hide();

                return;
            }

            Attach(Parent);
        }

        private void Attach(VisualElement target)
        {
            if (target == null || target == _target)
            {
                return;
            }

            _target = target;

            AnchorElement = target;

            if (OwnsDescription(target))
            {
                Lend(target);
            }

            target.PointerEnter += OnEnter;
            target.PointerLeave += OnLeave;
            target.PointerDown += OnDown;
            target.GotFocus += OnGotFocus;
            target.LostFocus += OnLostFocus;
        }

        private void Release()
        {
            Cancel();

            if (_target == null)
            {
                return;
            }

            _target.PointerEnter -= OnEnter;
            _target.PointerLeave -= OnLeave;
            _target.PointerDown -= OnDown;
            _target.GotFocus -= OnGotFocus;
            _target.LostFocus -= OnLostFocus;

            if (_lent != null && _target.Description == _lent)
            {
                _target.Description = null;
            }

            _lent = null;
            _target = null;
        }

        public void Show()
        {
            Cancel();

            if (_shown || _suppressed)
            {
                return;
            }

            _shown = true;

            Apply();
        }

        public void Hide()
        {
            Cancel();

            if (!_shown)
            {
                return;
            }

            _shown = false;

            Apply();
        }

        private void Cancel()
        {
            _pending?.Dispose();
            _pending = null;
        }

        private void Schedule()
        {
            Cancel();

            if (_shown)
            {
                return;
            }

            IScheduler scheduler = Host?.Scheduler;

            if (scheduler == null || Delay <= 0)
            {
                Show();

                return;
            }

            _pending = scheduler.Schedule(Delay, false, Show);
        }

        private void Apply()
        {
            Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _shown ? Visibility.Visible : Visibility.Hidden
            };

            Invalidate();
        }

        private void OnEnter(object sender, PointerEventArgs args) => Schedule();

        private void OnLeave(object sender, PointerEventArgs args)
        {
            _suppressed = false;

            Hide();
        }

        private void OnDown(object sender, PointerEventArgs args)
        {
            Hide();

            _suppressed = true;
        }

        private void OnGotFocus(object sender, EventArgs args) => Show();

        private void OnLostFocus(object sender, EventArgs args) => Hide();
    }
}
