using CoreGraphics;
using Foundation;
using Ixen.Core;
using Ixen.Core.Accessibility;
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
                Refresh();

                IxenAccessibilityElement root = ElementOf(_snapshot.RootId);

                return root == null ? new NSObject[0] : new NSObject[] { root };
            }
        }

        internal void Sync()
        {
            if (!UIAccessibility.IsVoiceOverRunning)
            {
                return;
            }

            Refresh();

            _changes.Clear();
            AccessibilitySnapshot.Diff(_spoken, _snapshot, _changes);
            _spoken = _snapshot;

            Announce();
        }

        private void Refresh()
        {
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

            element.AccessibilityContainer = parent == AccessibilitySnapshot.NONE
                ? (NSObject)_view
                : ElementOf(parent);

            IReadOnlyList<int> children = _snapshot.ChildrenOf(id);
            bool container = children.Count > 0;

            element.IsAccessibilityElement = !container;
            element.AccessibilityLabel = node.Name;
            element.AccessibilityValue = node.Value;
            element.AccessibilityHint = node.Description;
            element.AccessibilityTraits = (ulong)TraitsOf(node);
            element.AccessibilityFrame = _view.ScreenFrame(new CGRect(node.X, node.Y, node.Width, node.Height));

            if (container)
            {
                var elements = new NSObject[children.Count];

                for (int index = 0; index < children.Count; index++)
                {
                    elements[index] = ElementOf(children[index]);
                }

                element.Children = elements;
                element.ContainerType = node.Role == AccessibleRole.List || node.Role == AccessibleRole.Tree
                    ? UIAccessibilityContainerType.List
                    : node.Role == AccessibleRole.Table
                        ? UIAccessibilityContainerType.DataTable
                        : UIAccessibilityContainerType.SemanticGroup;
            }
            else
            {
                element.Children = null;
                element.ContainerType = UIAccessibilityContainerType.None;
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
            Refresh();

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

        internal NSObject[] Children { get; set; }

        internal UIAccessibilityContainerType ContainerType { get; set; }

        [Export("accessibilityElements")]
        public NSArray ExposedElements => Children == null ? null : NSArray.FromNSObjects(Children);

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType ExposedContainerType => ContainerType;

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
