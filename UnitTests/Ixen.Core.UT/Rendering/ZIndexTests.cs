using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ZIndexTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private static VisualElement Layer(string name, int? depth = null)
        {
            VisualElement layer = Element(name);
            layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            layer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            layer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            if (depth.HasValue)
            {
                layer.Styles.ZIndex = new ZIndexStyleDescriptor { Value = depth.Value };
            }

            VisualElement sheet = Element(name + "_sheet");
            sheet.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            sheet.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            sheet.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sheet.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };

            layer.AddChild(sheet);

            return layer;
        }

        private string[] Order()
            => _root.Overlays.Select(l => l.Name).ToArray();

        [TestMethod]
        public void WithNoDepthTheDeclarationOrderIsKept()
        {
            _root.AddChildren(Layer("first"), Layer("second"), Layer("third"));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, Order());
        }

        [TestMethod]
        public void AHigherDepthIsPaintedLast()
        {
            _root.AddChildren(Layer("high", 5), Layer("low", 1));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(new[] { "low", "high" }, Order(),
                "render walks the list forwards, so the last entry is on top");
        }

        [TestMethod]
        public void AHigherDepthIsHitFirst()
        {
            VisualElement low = Layer("low", 1);
            VisualElement high = Layer("high", 5);

            _root.AddChildren(low, high);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(high.FindByName("high_sheet"), _surface.HitTest(100, 100),
                "the deeper layer answers the click even though the two overlap exactly");
        }

        [TestMethod]
        public void DeclarationOrderStillDecidesBetweenEqualDepths()
        {
            _root.AddChildren(Layer("a", 2), Layer("b", 2), Layer("c", 2));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Order(),
                "the sort is stable, so equal depths keep the order they were declared in");
        }

        [TestMethod]
        public void ANegativeDepthGoesUnderAnUndeclaredOne()
        {
            _root.AddChildren(Layer("plain"), Layer("under", -1));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(new[] { "under", "plain" }, Order());
        }

        [TestMethod]
        public void ANegativeLayerStillPaintsOverTheOrdinaryTree()
        {
            VisualElement content = Element("content");
            content.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = VIEWPORT };
            content.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = VIEWPORT };

            VisualElement layer = Layer("under", -5);

            _root.AddChildren(content, layer);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(layer.FindByName("under_sheet"), _surface.HitTest(100, 100),
                "every layer is above the tree; a depth only orders layers among themselves");
        }

        [TestMethod]
        public void TheSortSurvivesARelayout()
        {
            _root.AddChildren(Layer("high", 9), Layer("low", 0));

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(new[] { "low", "high" }, Order());
        }

        [TestMethod]
        public void ADepthOnSomethingThatIsNotALayerChangesNothing()
        {
            VisualElement first = Element("first");
            first.Styles.ZIndex = new ZIndexStyleDescriptor { Value = 50 };

            VisualElement second = Element("second");

            _root.AddChildren(first, second);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, _root.Children.IndexOf(second),
                "ordinary children are painted in Children order and a depth does not reorder them");
        }

        [TestMethod]
        public void TheStyleIsReadFromXns()
        {
            var xnsSource = new XnsSource("box { z-index: 4 }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(4, ((ZIndexStyleDescriptor)set.Classes.Single().Styles.Single()).Value);
        }

        [TestMethod]
        public void ANegativeDepthIsExpressibleInXns()
        {
            var xnsSource = new XnsSource("box { z-index: -2 }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(-2, ((ZIndexStyleDescriptor)set.Classes.Single().Styles.Single()).Value);
        }

        [TestMethod]
        public void ADepthMustBeAWholeNumber()
        {
            foreach (string value in new[] { "1.5", "top", "2px", "" })
            {
                var xnsSource = new XnsSource($"box {{ z-index: {value} }}");
                xnsSource.Compile();

                Assert.IsTrue(xnsSource.HasErrors, $"'z-index: {value}' should have been rejected");
            }
        }

        [TestMethod]
        public void ANegativeSizeIsRejectedRatherThanReadAsPositive()
        {
            foreach (string declaration in new[] { "width: -6px", "height: -1%", "margin: -4px", "left: -8px" })
            {
                var xnsSource = new XnsSource($"box {{ {declaration} }}");
                xnsSource.Compile();

                Assert.IsTrue(xnsSource.HasErrors,
                    $"'{declaration}' must be a diagnostic, not a silent positive");

                Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code,
                    $"'{declaration}' reached the tokenizer, so the parser is what must refuse it");
            }
        }

        [TestMethod]
        public void ATrailingCommentIsStillNotSwallowedAfterASignedValue()
        {
            var xnsSource = new XnsSource("box { z-index: -3 // behind\n color: #FF0000 }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(2, set.Classes.Single().Styles.Count);
        }
    }
}
