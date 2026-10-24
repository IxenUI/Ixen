using Ixen.Core;
using Ixen.Core.Accessibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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
        private nint _host;

        internal UiaBridge(IxenSurface surface, Func<IntPtr> handle, Action invalidate)
        {
            _surface = surface;
            _handle = handle;
            _invalidate = invalidate;
        }

        internal IRawElementProviderFragmentRoot Root
            => ProviderFor(_snapshot.RootId) as IRawElementProviderFragmentRoot;

        internal nint HostProvider()
        {
            if (_host == 0)
            {
                UiaNative.UiaHostProviderFromHwnd(_handle(), out _host);
            }

            if (_host != 0)
            {
                Marshal.AddRef(_host);
            }

            return _host;
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

            nint pointer = UiaNative.ProviderPointer(provider);

            if (pointer == 0)
            {
                return IntPtr.Zero;
            }

            try
            {
                return UiaNative.UiaReturnRawElementProvider(_handle(), wParam, lParam, pointer);
            }
            finally
            {
                UiaNative.Release(pointer);
            }
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
                        if (change.Current.HasRange)
                        {
                            Raise(change.Id, UiaProperty.RANGE_VALUE_VALUE, change.Previous.RangeValue,
                                change.Current.RangeValue);
                        }
                        else
                        {
                            Raise(change.Id, UiaProperty.VALUE_VALUE, change.Previous.Value,
                                change.Current.Value);
                        }

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

                    case AccessibilityChangeKind.States:
                        States(change);
                        break;
                }
            }
        }

        private void States(AccessibilityChange change)
        {
            AccessibleNode was = change.Previous;
            AccessibleNode now = change.Current;

            if (UiaProvider.IsToggle(now) && UiaProvider.ToggleOf(was) != UiaProvider.ToggleOf(now))
            {
                Raise(change.Id, UiaProperty.TOGGLE_STATE, (int)UiaProvider.ToggleOf(was),
                    (int)UiaProvider.ToggleOf(now));
            }

            if (UiaProvider.IsExpandCollapse(now)
                && UiaProvider.ExpansionOf(was) != UiaProvider.ExpansionOf(now))
            {
                Raise(change.Id, UiaProperty.EXPAND_COLLAPSE_STATE, (int)UiaProvider.ExpansionOf(was),
                    (int)UiaProvider.ExpansionOf(now));
            }

            if (UiaProvider.IsSelectionItem(now.Role)
                && UiaProvider.IsSelected(was) != UiaProvider.IsSelected(now))
            {
                Raise(change.Id, UiaProperty.SELECTION_ITEM_IS_SELECTED, UiaProvider.IsSelected(was),
                    UiaProvider.IsSelected(now));

                if (UiaProvider.IsSelected(now))
                {
                    Event(change.Id, UiaEvent.ELEMENT_SELECTED);
                }
            }
        }

        private void Raise(int id, int property, object was, object now)
        {
            if (UiaNative.CanPassVariants && ProviderFor(id) is IRawElementProviderSimple provider)
            {
                nint pointer = UiaNative.ProviderPointer(provider);
                ComVariant before = UiaNative.Variant(was);
                ComVariant after = UiaNative.Variant(now);

                try
                {
                    UiaNative.UiaRaiseAutomationPropertyChangedEvent(pointer, property,
                        UiaNative.Raw(before), UiaNative.Raw(after));
                }
                finally
                {
                    before.Dispose();
                    after.Dispose();
                    UiaNative.Release(pointer);
                }
            }
        }

        private void Event(int id, int eventId)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                nint pointer = UiaNative.ProviderPointer(provider);

                UiaNative.UiaRaiseAutomationEvent(pointer, eventId);
                UiaNative.Release(pointer);
            }
        }

        private void Structure(int id)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                nint pointer = UiaNative.ProviderPointer(provider);

                UiaNative.UiaRaiseStructureChangedEvent(pointer,
                    StructureChangeType.ChildrenInvalidated,
                    new[] { UiaNative.UIA_APPEND_RUNTIME_ID, id },
                    2);
                UiaNative.Release(pointer);
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

        internal IRawElementProviderSimple ContainerOf(int id)
        {
            AccessibilitySnapshot snapshot = _snapshot;

            for (int at = snapshot.ParentOf(id); at >= 0; at = snapshot.ParentOf(at))
            {
                AccessibleNode node = snapshot.NodeOf(at);

                if (node != null && UiaProvider.IsSelectionContainer(node.Role))
                {
                    return ProviderFor(at);
                }
            }

            return null;
        }

        internal IRawElementProviderSimple[] SelectionOf(int id)
        {
            AccessibilitySnapshot snapshot = _snapshot;
            var selected = new List<IRawElementProviderSimple>();

            if (snapshot.Contains(id))
            {
                CollectSelected(snapshot, id, selected);
            }

            return selected.ToArray();
        }

        private void CollectSelected(AccessibilitySnapshot snapshot, int id,
            List<IRawElementProviderSimple> into)
        {
            IReadOnlyList<int> children = snapshot.ChildrenOf(id);

            for (int index = 0; index < children.Count; index++)
            {
                int child = children[index];
                AccessibleNode node = snapshot.NodeOf(child);

                if (node == null || UiaProvider.IsSelectionContainer(node.Role))
                {
                    continue;
                }

                if (UiaProvider.IsSelectionItem(node.Role) && UiaProvider.IsSelected(node))
                {
                    into.Add(ProviderFor(child));
                }

                CollectSelected(snapshot, child, into);
            }
        }

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
