using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ProgressTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private ProgressBar _bar;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _bar = new ProgressBar { Name = "loading" };
            _bar.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _bar.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 6 };

            _root.AddChild(_bar);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private VisualElement Fill()
            => _bar.ChildElements.Single(c => c.TypeName == ProgressBar.FILL);

        [TestMethod]
        public void TheFillFollowsTheValue()
        {
            _bar.Value = 30;

            Assert.AreEqual(0.3f, _bar.Fraction, 0.001f);
            Assert.AreEqual(30f, Fill().Styles.Width.Value, 0.001f);
        }

        [TestMethod]
        public void AValueOutsideTheRangeIsClamped()
        {
            _bar.Value = 400;
            Assert.AreEqual(100f, _bar.Value);

            _bar.Value = -10;
            Assert.AreEqual(0f, _bar.Value);
        }

        [TestMethod]
        public void ARangeOfNothingIsNotADivisionByZero()
        {
            _bar.Maximum = 0;

            Assert.AreEqual(0f, _bar.Fraction);
        }

        [TestMethod]
        public void BusyIsAStateAndHasNoValue()
        {
            _bar.Value = 40;
            _bar.Busy = true;

            Assert.IsTrue(_bar.HasState(ProgressBar.BUSY),
                "so #ProgressBar:busy drives the sweep from the stylesheet, with a keyframe "
                + "animation over left and width - no C# and no second element");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Single(c => c.Role == AccessibleRole.ProgressBar);

            Assert.IsNull(node.Value,
                "an indeterminate bar must not claim a percentage it does not know");
        }

        [TestMethod]
        public void ADeterminateBarReportsItsValue()
        {
            _bar.Value = 40;
            _bar.Label = "loading";

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Single(c => c.Role == AccessibleRole.ProgressBar);

            Assert.AreEqual("loading", node.Name);
            Assert.AreEqual("40", node.Value);
            Assert.AreEqual(0, node.Children.Count, "the fill is decoration");
        }

        [TestMethod]
        public void ASpinnerSaysWhatItIsWithoutDrawingAnything()
        {
            var spinner = new Spinner { Name = "busy" };
            spinner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 16 };
            spinner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 16 };

            _root.AddChild(spinner);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Last();

            Assert.AreEqual(AccessibleRole.ProgressBar, node.Role);
            Assert.AreEqual("Busy", node.Name);
            Assert.IsNull(node.Value, "it is indeterminate by nature");
            Assert.AreEqual(1, spinner.ChildElements.Count,
                "a spinner is one dot that the theme orbits with a keyframe animation over a "
                + "rotate - a RING that rotates is indistinguishable from a ring standing still");
            Assert.AreEqual(0, node.Children.Count, "and the dot is decoration");
        }
    }
}
