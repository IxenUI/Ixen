using Ixen.Core;
using Ixen.Core.Accessibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ixen.Platform.Windows.Accessibility
{
    internal sealed class UiaBridge
    {
        private const long UIA_ROOT_OBJECT_ID = -25;

        private readonly IxenSurface _surface;
        private readonly Action _invalidate;
        private readonly Func<IntPtr> _handle;

        private readonly AccessibilityIds _ids = new AccessibilityIds();
        private readonly List<int> _dropped = new List<int>();
        private readonly List<AccessibilityChange> _changes = new List<AccessibilityChange>();

        private readonly ConcurrentDictionary<int, UiaProvider> _providers
            = new ConcurrentDictionary<int, UiaProvider>();

        private readonly ConcurrentQueue<(int Id, AccessibleActions Action, string Value)> _pending
            = new ConcurrentQueue<(int, AccessibleActions, string)>();

        private AccessibilitySnapshot _snapshot = AccessibilitySnapshot.Empty;
        private IRawElementProviderSimple _host;

        internal UiaBridge(IxenSurface surface, Func<IntPtr> handle, Action invalidate)
        {
            _surface = surface;
            _handle = handle;
            _invalidate = invalidate;
        }

        internal IRawElementProviderFragmentRoot Root
            => ProviderFor(_snapshot.RootId) as IRawElementProviderFragmentRoot;

        internal IRawElementProviderSimple HostProvider
        {
            get
            {
                if (_host == null)
                {
                    UiaNative.UiaHostProviderFromHwnd(_handle(), out _host);
                }

                return _host;
            }
        }

        internal IntPtr Answer(IntPtr wParam, IntPtr lParam)
        {
            if (lParam.ToInt64() != UIA_ROOT_OBJECT_ID)
            {
                return IntPtr.Zero;
            }

            Refresh();

            if (!(Root is IRawElementProviderSimple provider))
            {
                return IntPtr.Zero;
            }

            return UiaNative.UiaReturnRawElementProvider(_handle(), wParam, lParam, provider);
        }

        internal void Sync()
        {
            Drain();

            if (!UiaNative.UiaClientsAreListening())
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            AccessibleNode root = _surface.BuildAccessibilityTree();

            if (root == null)
            {
                return;
            }

            _dropped.Clear();

            AccessibilitySnapshot snapshot = AccessibilitySnapshot.Take(root, _ids, _dropped);

            for (int index = 0; index < _dropped.Count; index++)
            {
                _providers.TryRemove(_dropped[index], out _);
            }

            AccessibilitySnapshot previous = _snapshot;

            _snapshot = snapshot;

            _changes.Clear();

            AccessibilitySnapshot.Diff(previous, snapshot, _changes);

            Announce();
        }

        private void Announce()
        {
            for (int index = 0; index < _changes.Count; index++)
            {
                AccessibilityChange change = _changes[index];

                switch (change.Kind)
                {
                    case AccessibilityChangeKind.Name:
                        Raise(change.Id, UiaProperty.NAME, change.Previous.Name,
                            change.Current.Name);
                        break;

                    case AccessibilityChangeKind.Value:
                        Raise(change.Id, UiaProperty.VALUE_VALUE, change.Previous.Value,
                            change.Current.Value);
                        break;

                    case AccessibilityChangeKind.LiveRegion:
                        Event(change.Id, UiaEvent.LIVE_REGION_CHANGED);
                        break;

                    case AccessibilityChangeKind.Focus:
                        Raise(change.Id, UiaProperty.HAS_KEYBOARD_FOCUS, !change.TookFocus,
                            change.TookFocus);

                        if (change.TookFocus)
                        {
                            Event(change.Id, UiaEvent.FOCUS_CHANGED);
                        }

                        break;

                    case AccessibilityChangeKind.Structure:
                        Structure(change.Id);
                        break;
                }
            }
        }

        private void Raise(int id, int property, object was, object now)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseAutomationPropertyChangedEvent(provider, property, was, now);
            }
        }

        private void Event(int id, int eventId)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseAutomationEvent(provider, eventId);
            }
        }

        private void Structure(int id)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseStructureChangedEvent(provider,
                    StructureChangeType.ChildrenInvalidated,
                    new[] { UiaNative.UIA_APPEND_RUNTIME_ID, id },
                    2);
            }
        }

        internal AccessibleNode NodeOf(int id) => _snapshot.NodeOf(id);

        private UiaProvider ProviderFor(int id)
        {
            if (id < 0)
            {
                return null;
            }

            return _providers.GetOrAdd(id,
                key => key == _snapshot.RootId
                    ? new UiaRootProvider(this, key)
                    : (UiaProvider)new UiaProvider(this, key));
        }

        internal IRawElementProviderFragment Navigate(int id, NavigateDirection direction)
        {
            AccessibilitySnapshot snapshot = _snapshot;

            if (!snapshot.Contains(id))
            {
                return null;
            }

            switch (direction)
            {
                case NavigateDirection.Parent:
                    return ProviderFor(snapshot.ParentOf(id));

                case NavigateDirection.FirstChild:
                    return ProviderFor(snapshot.ChildOf(id, 0));

                case NavigateDirection.LastChild:
                    return ProviderFor(snapshot.ChildOf(id, -1));

                case NavigateDirection.NextSibling:
                    return ProviderFor(snapshot.SiblingOf(id, 1));

                case NavigateDirection.PreviousSibling:
                    return ProviderFor(snapshot.SiblingOf(id, -1));

                default:
                    return null;
            }
        }

        internal UiaRect RectangleOf(int id)
        {
            AccessibleNode node = NodeOf(id);

            if (node == null)
            {
                return default;
            }

            float scale = _surface.Scale;

            var origin = new UiaNative.Point
            {
                X = (int)Math.Round(node.X * scale),
                Y = (int)Math.Round(node.Y * scale)
            };

            if (!UiaNative.ClientToScreen(_handle(), ref origin))
            {
                return default;
            }

            return new UiaRect
            {
                Left = origin.X,
                Top = origin.Y,
                Width = node.Width * scale,
                Height = node.Height * scale
            };
        }

        internal IRawElementProviderFragment FromPoint(double x, double y)
        {
            AccessibilitySnapshot snapshot = _snapshot;

            if (snapshot.RootId < 0)
            {
                return null;
            }

            var point = new UiaNative.Point { X = 0, Y = 0 };

            if (!UiaNative.ClientToScreen(_handle(), ref point))
            {
                return null;
            }

            float scale = _surface.Scale;

            return ProviderFor(snapshot.FindAt((x - point.X) / scale, (y - point.Y) / scale));
        }

        internal IRawElementProviderFragment Focused() => ProviderFor(_snapshot.FocusedId());

        internal void Post(int id, AccessibleActions action, string value)
        {
            _pending.Enqueue((id, action, value));
            _invalidate();
        }

        private void Drain()
        {
            while (_pending.TryDequeue(out (int Id, AccessibleActions Action, string Value) work))
            {
                AccessibleNode node = NodeOf(work.Id);

                if (node == null)
                {
                    continue;
                }

                _surface.Perform(node, work.Action, work.Value);
            }
        }
    }
}
