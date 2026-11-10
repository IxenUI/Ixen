using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class Toaster : VisualElement
    {
        public const string TOAST = "Toast";
        public const string MESSAGE = "ToastMessage";
        public const string ACTION = "ToastAction";
        public const string SHOWN = "shown";

        public event EventHandler<EventArgs> Dismissed;

        private readonly VisualElement _toast;
        private readonly VisualElement _message;
        private readonly Button _action;
        private readonly Queue<Entry> _queue = new();

        private Entry _current;
        private IDisposable _timer;
        private bool _hovered;

        private sealed class Entry
        {
            internal string Message;
            internal string Action;
            internal Action OnAction;
        }

        public Toaster()
        {
            TypeName = nameof(Toaster);

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _toast = new VisualElement { TypeName = TOAST, Role = AccessibleRole.Presentation };
            _toast.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            _toast.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _toast.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _toast.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 50 };

            _message = new VisualElement { TypeName = MESSAGE, LiveRegion = LiveRegionKind.Polite };
            _action = new Button { TypeName = ACTION, Focusable = false };
            _action.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };

            _toast.AddChildren(_message, _action);
            AddChild(_toast);

            _toast.PointerEnter += OnEnter;
            _toast.PointerLeave += OnLeave;
            _action.PointerClick += OnAction;
        }

        public int Duration { get; set; } = 4000;

        public bool IsShowing => _current != null;

        public string Message => _current?.Message;

        public int Pending => _queue.Count;

        public VisualElement Toast => _toast;

        public void Show(string message, string action = null, Action onAction = null)
        {
            var entry = new Entry { Message = message, Action = action, OnAction = onAction };

            if (_current != null)
            {
                _queue.Enqueue(entry);

                return;
            }

            Present(entry);
        }

        public void Dismiss()
        {
            if (_current == null)
            {
                return;
            }

            Cancel();

            _current = null;

            if (_queue.Count > 0)
            {
                Present(_queue.Dequeue());
            }
            else
            {
                Hide();
            }

            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        private void Present(Entry entry)
        {
            _current = entry;

            _message.Text = entry.Message;
            _message.Invalidate();

            bool acts = !string.IsNullOrEmpty(entry.Action);

            _action.Text = entry.Action;
            _action.Focusable = acts;
            _action.Styles.Visibility = new VisibilityStyleDescriptor { Value = acts ? Visibility.Visible : Visibility.Hidden };
            _action.Invalidate();

            _toast.AddState(SHOWN);
            _toast.Role = AccessibleRole.Group;
            _toast.Invalidate();

            Schedule();
        }

        private void Hide()
        {
            _action.Focusable = false;
            _toast.ToggleState(SHOWN, false);
            _toast.Role = AccessibleRole.Presentation;
            _toast.Invalidate();
        }

        private void Schedule()
        {
            Cancel();

            IScheduler scheduler = Host?.Scheduler;

            if (scheduler == null || Duration <= 0 || _current == null || _hovered)
            {
                return;
            }

            _timer = scheduler.Schedule(Duration, false, Expire);
        }

        private void Expire()
        {
            _timer = null;

            if (_current == null)
            {
                return;
            }

            if (Host?.FocusedElement == _action)
            {
                Schedule();

                return;
            }

            Dismiss();
        }

        private void Cancel()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void OnEnter(object sender, PointerEventArgs args)
        {
            _hovered = true;

            Cancel();
        }

        private void OnLeave(object sender, PointerEventArgs args)
        {
            _hovered = false;

            Schedule();
        }

        private void OnAction(object sender, PointerEventArgs args)
        {
            Action invoked = _current?.OnAction;

            args.Handled = true;

            Dismiss();

            invoked?.Invoke();
        }

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host == null)
            {
                Cancel();
            }
            else if (_current != null && _timer == null)
            {
                Schedule();
            }
        }
    }
}
