using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class AccessibleInvokerTests
    {
        private const int VIEWPORT = 300;

        private sealed class Playlist : VisualElement, IAccessibleInvoker
        {
            internal readonly List<VisualElement> Played = new List<VisualElement>();
            internal bool Answers = true;
            internal bool RefusesAll;

            public Playlist()
            {
                Role = AccessibleRole.List;
                Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            }

            public bool CanInvoke(VisualElement item) => !RefusesAll && item.Text != "locked";

            public bool Invoke(VisualElement item)
            {
                Played.Add(item);

                return Answers;
            }
        }

        private VisualElement _root;
        private Playlist _list;
        private IxenSurface _surface;
        private int _clicks;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new Playlist();
            _root.AddChild(_list);

            foreach (string name in new[] { "first", "locked" })
            {
                var item = new VisualElement { Role = AccessibleRole.ListItem, Text = name };
                item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
                item.PointerClick += (sender, e) => _clicks++;
                _list.AddChild(item);
            }

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private AccessibleNode Item(string name)
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            foreach (AccessibleNode node in _surface.BuildAccessibilityTree().Children[0].Children)
            {
                if (node.Name == name)
                {
                    return node;
                }
            }

            return null;
        }

        [TestMethod]
        public void AnItemItsContainerCanInvokeOffersInvoke()
        {
            Assert.IsTrue(Item("first").Supports(AccessibleActions.Invoke),
                "a list item has no invoke by its role; the container says it has one");
            Assert.IsFalse(Item("locked").Supports(AccessibleActions.Invoke), "and the container may refuse an item");
        }

        [TestMethod]
        public void InvokingGoesToTheContainerNotToAClick()
        {
            Assert.IsTrue(_surface.Perform(Item("first"), AccessibleActions.Invoke, null));

            Assert.AreEqual(1, _list.Played.Count);
            Assert.AreEqual("first", _list.Played[0].Text);
            Assert.AreEqual(0, _clicks, "a click on a row selects it; invoking it is the double-click, not the click");
        }

        [TestMethod]
        public void TheContainersAnswerIsTheResult()
        {
            _list.Answers = false;

            Assert.IsFalse(_surface.Perform(Item("first"), AccessibleActions.Invoke, null));
        }

        [TestMethod]
        public void TheNearestInvokerDecides()
        {
            var inner = new Playlist { RefusesAll = true };
            var nested = new VisualElement { Role = AccessibleRole.ListItem, Text = "first" };
            nested.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            inner.AddChild(nested);
            _list.AddChild(inner);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = null;

            foreach (AccessibleNode child in _surface.BuildAccessibilityTree().Children[0].Children)
            {
                if (child.Element == inner)
                {
                    node = child.Children[0];
                }
            }

            Assert.IsNotNull(node);
            Assert.IsFalse(node.Supports(AccessibleActions.Invoke),
                "the inner list refuses it, and an outer one does not get a say over another list's rows");
        }
    }
}
