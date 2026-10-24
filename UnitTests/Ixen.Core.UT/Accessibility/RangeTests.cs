using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class RangeTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;

        private sealed class Dial : VisualElement, IAccessibleRange
        {
            public bool HasRange { get; set; } = true;

            public float Minimum => 0;

            public float Maximum => 40;

            public float Value { get; private set; } = 10;

            public float Step => 2;

            public bool CanSetValue { get; set; } = true;

            public void SetValue(float value) => Value = value;
        }

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private Dial AddDial()
        {
            var dial = new Dial { Name = "dial", Role = AccessibleRole.Slider, Label = "Volume" };

            dial.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            _root.AddChild(dial);

            return dial;
        }

        private AccessibleNode Node()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.BuildAccessibilityTree().Children[0];
        }

        [TestMethod]
        public void TheRangeTravelsIntoTheNode()
        {
            AddDial();

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasRange);
            Assert.AreEqual(0f, node.RangeMinimum);
            Assert.AreEqual(40f, node.RangeMaximum);
            Assert.AreEqual(10f, node.RangeValue);
            Assert.AreEqual(2f, node.RangeStep);
            Assert.IsTrue(node.Supports(AccessibleActions.SetRangeValue));
        }

        [TestMethod]
        public void SettingTheValueGoesThroughTheElement()
        {
            Dial dial = AddDial();

            Assert.IsTrue(_surface.Perform(Node(), AccessibleActions.SetRangeValue, "12.5"));
            Assert.AreEqual(12.5f, dial.Value,
                "the number is read the invariant way, whatever the machine's culture writes");
        }

        [TestMethod]
        public void SomethingThatIsNotANumberIsRefused()
        {
            Dial dial = AddDial();

            Assert.IsFalse(_surface.Perform(Node(), AccessibleActions.SetRangeValue, "loud"));
            Assert.AreEqual(10f, dial.Value);
        }

        [TestMethod]
        public void AReadOnlyRangeIsReportedButNotOffered()
        {
            AddDial().CanSetValue = false;

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasRange);
            Assert.IsFalse(node.Supports(AccessibleActions.SetRangeValue));
        }

        [TestMethod]
        public void ADisabledRangeOffersNothing()
        {
            AddDial().Enabled = false;

            Assert.IsFalse(Node().Supports(AccessibleActions.SetRangeValue));
        }

        [TestMethod]
        public void AnIndeterminateOneHasNoRange()
        {
            AddDial().HasRange = false;

            AccessibleNode node = Node();

            Assert.IsFalse(node.HasRange, "a busy bar has no position to report");
            Assert.IsFalse(node.Supports(AccessibleActions.SetRangeValue));
        }
    }
}
