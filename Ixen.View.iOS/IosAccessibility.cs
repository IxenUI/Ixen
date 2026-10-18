using CoreGraphics;
using Foundation;
using Ixen.Core;
using Ixen.Core.Accessibility;
using System;
using System.Collections.Generic;
using UIKit;

namespace Ixen.View.iOS
{
    internal sealed class IosAccessibility
    {
        private readonly IxenView _view;
        private readonly IxenSurface _surface;
        private readonly AccessibilityIds _ids = new AccessibilityIds();
        private readonly Dictionary<int, IxenAccessibilityElement> _elements
            = new Dictionary<int, IxenAccessibilityElement>();
        private readonly List<int> _dropped = new List<int>();
        private readonly List<AccessibilityChange> _changes = new List<AccessibilityChange>();

        private AccessibilitySnapshot _snapshot = AccessibilitySnapshot.Empty;
        private AccessibilitySnapshot _spoken = AccessibilitySnapshot.Empty;

        internal IosAccessibility(IxenView view, IxenSurface surface)
        {
            _view = view;
            _surface = surface;
        }

        internal NSObject[] Elements
        {
            get
            {
                RefreshIfStale();

                IxenAccessibilityElement root = ElementOf(_snapshot.RootId);

                return root == null ? new NSObject[0] : new NSObject[] { root };
            }
        }

        internal void Sync()
        {
            if (!UIAccessibility.IsVoiceOverRunning || !_stale)
            {
                return;
            }

            Refresh();

            _changes.Clear();
            AccessibilitySnapshot.Diff(_spoken, _snapshot, _changes);
            _spoken = _snapshot;

            Announce();
        }

        private bool _stale = true;

        internal void MarkStale() => _stale = true;

        private void RefreshIfStale()
        {
            if (_stale)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _stale = false;
            _dropped.Clear();
            _snapshot = AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), _ids, _dropped);

            for (int index = 0; index < _dropped.Count; index++)
            {
                _elements.Remove(_dropped[index]);
            }

            IReadOnlyList<int> order = _snapshot.Order;

            for (int index = 0; index < order.Count; index++)
            {
                Update(order[index]);
            }
        }

        private IxenAccessibilityElement ElementOf(int id)
        {
            if (id == AccessibilitySnapshot.NONE)
            {
                return null;
            }

            if (!_elements.TryGetValue(id, out IxenAccessibilityElement element))
            {
                element = new IxenAccessibilityElement(_view, this, id);
                _elements[id] = element;
            }

            return element;
        }

        private void Update(int id)
        {
            AccessibleNode node = _snapshot.NodeOf(id);
            IxenAccessibilityElement element = ElementOf(id);

            if (node == null || element == null)
            {
                return;
            }

            int parent = _snapshot.ParentOf(id);
            IReadOnlyList<int> children = _snapshot.ChildrenOf(id);

            element.Push(
                parent == AccessibilitySnapshot.NONE ? (NSObject)_view : ElementOf(parent),
                node,
                (ulong)TraitsOf(node),
                _view.ScreenFrame(new CGRect(node.X, node.Y, node.Width, node.Height)),
                children,
                ElementOf,
                ContainerTypeOf(node));
        }

        private static UIAccessibilityContainerType ContainerTypeOf(AccessibleNode node)
        {
            switch (node.Role)
            {
                case AccessibleRole.List:
                case AccessibleRole.Tree:
                    return UIAccessibilityContainerType.List;

                case AccessibleRole.Table:
                    return UIAccessibilityContainerType.DataTable;

                default:
                    return UIAccessibilityContainerType.SemanticGroup;
            }
        }

        internal static UIAccessibilityTrait TraitsOf(AccessibleNode node)
        {
            UIAccessibilityTrait traits = UIAccessibilityTrait.None;

            switch (node.Role)
            {
                case AccessibleRole.Button:
                case AccessibleRole.Tab:
                case AccessibleRole.MenuItem:
                case AccessibleRole.TreeItem:
                case AccessibleRole.ColumnHeader:
                case AccessibleRole.ComboBox:
                case AccessibleRole.CheckBox:
                case AccessibleRole.RadioButton:
                case AccessibleRole.Switch:
                    traits |= UIAccessibilityTrait.Button;
                    break;

                case AccessibleRole.Link:
                    traits |= UIAccessibilityTrait.Link;
                    break;

                case AccessibleRole.Heading:
                    traits |= UIAccessibilityTrait.Header;
                    break;

                case AccessibleRole.Image:
                    traits |= UIAccessibilityTrait.Image;
                    break;

                case AccessibleRole.Text:
                    traits |= UIAccessibilityTrait.StaticText;
                    break;

                case AccessibleRole.Slider:
                    traits |= UIAccessibilityTrait.Adjustable;
                    break;

                case AccessibleRole.ProgressBar:
                    traits |= UIAccessibilityTrait.UpdatesFrequently;
                    break;
            }

            if (node.HasState(AccessibleStates.Disabled))
            {
                traits |= UIAccessibilityTrait.NotEnabled;
            }

            if (node.HasState(AccessibleStates.Selected) || node.HasState(AccessibleStates.Checked))
            {
                traits |= UIAccessibilityTrait.Selected;
            }

            return traits;
        }

        private void Announce()
        {
            bool structure = false;
            IxenAccessibilityElement focused = null;

            for (int index = 0; index < _changes.Count; index++)
            {
                AccessibilityChange change = _changes[index];

                switch (change.Kind)
                {
                    case AccessibilityChangeKind.LiveRegion:
                        string spoken = Spoken(change.Current);

                        if (!string.IsNullOrEmpty(spoken))
                        {
                            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement,
                                new NSString(spoken));
                        }
                        break;

                    case AccessibilityChangeKind.Focus:
                        if (change.TookFocus)
                        {
                            focused = ElementOf(change.Id);
                        }
                        break;

                    case AccessibilityChangeKind.Structure:
                        structure = true;
                        break;
                }
            }

            if (structure || focused != null)
            {
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, focused);
            }
        }

        internal static string Spoken(AccessibleNode node)
        {
            if (node == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(node.Value) ? node.Name : node.Value;
        }

        internal NSObject HitTest(CGPoint point)
        {
            RefreshIfStale();

            int id = _snapshot.FindAt(point.X, point.Y);

            while (id != AccessibilitySnapshot.NONE)
            {
                IxenAccessibilityElement element = ElementOf(id);

                if (element != null && element.IsAccessibilityElement)
                {
                    return element;
                }

                id = _snapshot.ParentOf(id);
            }

            return null;
        }

        internal AccessibleNode NodeOf(int id) => _snapshot.NodeOf(id);

        internal bool Perform(int id, AccessibleActions action)
        {
            AccessibleNode node = _snapshot.NodeOf(id);

            if (node == null || !node.Supports(action))
            {
                return false;
            }

            bool done = _surface.Perform(node, action, null);

            _view.AccessibilityActed();

            return done;
        }
    }

    internal sealed class IxenAccessibilityElement : UIAccessibilityElement
    {
        private readonly IosAccessibility _bridge;
        private readonly int _id;

        internal IxenAccessibilityElement(NSObject container, IosAccessibility bridge, int id)
            : base(container)
        {
            _bridge = bridge;
            _id = id;
        }

        private NSObject[] _children;
        private int[] _childIds;
        private UIAccessibilityContainerType _containerType;
        private NSObject _pushedContainer;
        private bool _pushed;

        internal void Push(NSObject container, AccessibleNode node, ulong traits, CGRect frame,
            IReadOnlyList<int> childIds, Func<int, IxenAccessibilityElement> elementOf,
            UIAccessibilityContainerType containerType)
        {
            bool isContainer = childIds.Count > 0;

            if (!_pushed || !ReferenceEquals(_pushedContainer, container))
            {
                AccessibilityContainer = container;
                _pushedContainer = container;
            }

            if (!_pushed || IsAccessibilityElement == isContainer)
            {
                IsAccessibilityElement = !isContainer;
            }

            if (!_pushed || AccessibilityLabel != node.Name)
            {
                AccessibilityLabel = node.Name;
            }

            if (!_pushed || AccessibilityValue != node.Value)
            {
                AccessibilityValue = node.Value;
            }

            if (!_pushed || AccessibilityHint != node.Description)
            {
                AccessibilityHint = node.Description;
            }

            if (!_pushed || AccessibilityTraits != traits)
            {
                AccessibilityTraits = traits;
            }

            if (!_pushed || AccessibilityFrame != frame)
            {
                AccessibilityFrame = frame;
            }

            _containerType = isContainer ? containerType : UIAccessibilityContainerType.None;

            if (!SameIds(childIds))
            {
                _childIds = new int[childIds.Count];
                _children = isContainer ? new NSObject[childIds.Count] : null;

                for (int index = 0; index < childIds.Count; index++)
                {
                    _childIds[index] = childIds[index];
                    _children[index] = elementOf(childIds[index]);
                }
            }

            _pushed = true;
        }

        private bool SameIds(IReadOnlyList<int> ids)
        {
            if (_childIds == null || _childIds.Length != ids.Count)
            {
                return false;
            }

            for (int index = 0; index < ids.Count; index++)
            {
                if (_childIds[index] != ids[index])
                {
                    return false;
                }
            }

            return true;
        }

        [Export("accessibilityElements")]
        public NSArray ExposedElements => _children == null ? null : NSArray.FromNSObjects(_children);

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType ExposedContainerType => _containerType;

        [Export("accessibilityActivate")]
        public bool Activate()
        {
            AccessibleNode node = _bridge.NodeOf(_id);

            if (node == null)
            {
                return false;
            }

            if (node.Supports(AccessibleActions.Invoke))
            {
                return _bridge.Perform(_id, AccessibleActions.Invoke);
            }

            return _bridge.Perform(_id, AccessibleActions.Focus);
        }

        public override void AccessibilityElementDidBecomeFocused()
        {
            AccessibleNode node = _bridge.NodeOf(_id);

            if (node != null && node.HasState(AccessibleStates.Offscreen))
            {
                _bridge.Perform(_id, AccessibleActions.ScrollIntoView);
            }
        }
    }
}
