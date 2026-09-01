using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StructuralPseudoClassTests
    {
        private const int VIEWPORT = 200;

        private StyleRegistry _registry;
        private IxenSurface _surface;
        private VisualElement _list;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static VisualElement Row(string name, params string[] classes)
        {
            var row = new VisualElement { Name = name };

            foreach (string c in classes)
            {
                row.Classes.Add(c);
            }

            return row;
        }

        private IxenSurface Build(string xns, int rows, params string[] classes)
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile(xns));

            _list = new VisualElement { Name = "list" };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < rows; index++)
            {
                _list.AddChild(Row("row", classes));
            }

            _surface = new IxenSurface(_list) { Styles = _registry };

            _list.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface;
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        private VisualElement At(int index) => _list.ChildElements[index];

        private VisualElement Groups(string xns, int groups)
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile(xns));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < groups; index++)
            {
                var group = new VisualElement { Name = "group" };
                group.AddChild(new VisualElement { Name = "label" });
                root.AddChild(group);
            }

            _surface = new IxenSurface(root) { Styles = _registry };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return root;
        }

        private static VisualElement LabelOf(VisualElement root, int group)
            => root.ChildElements[group].ChildElements[0];

        [TestMethod]
        public void AScopeSegmentAnswersLastChildToo()
        {
            VisualElement root = Groups(
                "group:last-child {\r\n    label { background: #222222 }\r\n}", 3);

            Assert.IsNull(BackgroundOf(LabelOf(root, 0)));
            Assert.AreEqual("#222222", BackgroundOf(LabelOf(root, 2)));
        }

        [TestMethod]
        public void AScopeSegmentAnswersANumberedChild()
        {
            VisualElement root = Groups(
                "group:nth-child(2) {\r\n    label { background: #222222 }\r\n}", 3);

            Assert.IsNull(BackgroundOf(LabelOf(root, 0)));

            Assert.AreEqual("#222222", BackgroundOf(LabelOf(root, 1)),
                "the selector builds the name from the index and the segment matcher parses it "
                + "back, so both directions of the one-based convention need pinning");

            Assert.IsNull(BackgroundOf(LabelOf(root, 2)));
        }

        [TestMethod]
        public void AScopeSegmentAnswersOddAndEven()
        {
            VisualElement root = Groups(
                "group:nth-child(even) {\r\n    label { background: #222222 }\r\n}", 4);

            Assert.IsNull(BackgroundOf(LabelOf(root, 0)));
            Assert.AreEqual("#222222", BackgroundOf(LabelOf(root, 1)));
            Assert.IsNull(BackgroundOf(LabelOf(root, 2)));
            Assert.AreEqual("#222222", BackgroundOf(LabelOf(root, 3)));
        }

        [TestMethod]
        public void TheFirstChildIsTheOnlyOneMatched()
        {
            Build("row { background: #111111 }\r\nrow:first-child { background: #222222 }", 3);

            Assert.AreEqual("#222222", BackgroundOf(At(0)));
            Assert.AreEqual("#111111", BackgroundOf(At(1)));
            Assert.AreEqual("#111111", BackgroundOf(At(2)));
        }

        [TestMethod]
        public void TheLastChildIsTheOnlyOneMatched()
        {
            Build("row { background: #111111 }\r\nrow:last-child { background: #222222 }", 3);

            Assert.AreEqual("#111111", BackgroundOf(At(0)));
            Assert.AreEqual("#222222", BackgroundOf(At(2)));
        }

        [TestMethod]
        public void OnlyChildNeedsToBeAlone()
        {
            Build("row { background: #111111 }\r\nrow:only-child { background: #222222 }", 1);

            Assert.AreEqual("#222222", BackgroundOf(At(0)));

            Build("row { background: #111111 }\r\nrow:only-child { background: #222222 }", 2);

            Assert.AreEqual("#111111", BackgroundOf(At(0)),
                "a first child that is not alone is not an only child");
        }

        [TestMethod]
        public void NthChildCountsFromOne()
        {
            Build("row { background: #111111 }\r\nrow:nth-child(2) { background: #222222 }", 3);

            Assert.AreEqual("#111111", BackgroundOf(At(0)));
            Assert.AreEqual("#222222", BackgroundOf(At(1)), "CSS counts from one, not from zero");
            Assert.AreEqual("#111111", BackgroundOf(At(2)));
        }

        [TestMethod]
        public void OddAndEvenStripeTheList()
        {
            Build("row:nth-child(odd) { background: #111111 }\r\n"
                + "row:nth-child(even) { background: #222222 }", 4);

            Assert.AreEqual("#111111", BackgroundOf(At(0)));
            Assert.AreEqual("#222222", BackgroundOf(At(1)));
            Assert.AreEqual("#111111", BackgroundOf(At(2)));
            Assert.AreEqual("#222222", BackgroundOf(At(3)));
        }

        [TestMethod]
        public void ItWorksOnAClassSelector()
        {
            Build(".item { background: #111111 }\r\n.item:first-child { background: #222222 }",
                3, "item");

            Assert.AreEqual("#222222", BackgroundOf(At(0)));
            Assert.AreEqual("#111111", BackgroundOf(At(1)));
        }

        [TestMethod]
        public void StructureLosesToState()
        {
            Build("row { background: #111111 }\r\n"
                + "row:first-child { background: #222222 }\r\n"
                + "row:hover { background: #333333 }", 3);

            Assert.AreEqual("#222222", BackgroundOf(At(0)));

            At(0).AddState(StyleStates.HOVER);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#333333", BackgroundOf(At(0)),
                "structure is where an element sits and state is what the pointer is doing to it, "
                + "so the transient one has to win or hovering the first row would do nothing");
        }

        [TestMethod]
        public void ItWorksInAScopeSegment()
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile("group:first-child {\r\n    label { background: #222222 }\r\n}"));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var first = new VisualElement { Name = "group" };
            var second = new VisualElement { Name = "group" };

            var inside = new VisualElement { Name = "label" };
            var outside = new VisualElement { Name = "label" };

            first.AddChild(inside);
            second.AddChild(outside);
            root.AddChildren(first, second);

            _surface = new IxenSurface(root) { Styles = _registry };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(inside));
            Assert.IsNull(BackgroundOf(outside),
                "the segment matcher has to answer a structural pseudo-class the same way "
                + "ApplySelector does, or a scoped rule would silently never match");
        }

        [TestMethod]
        public void AddingASiblingRestylesTheOneThatStoppedBeingLast()
        {
            Build("row { background: #111111 }\r\nrow:last-child { background: #222222 }", 2);

            Assert.AreEqual("#222222", BackgroundOf(At(1)));

            _list.AddChild(Row("row"));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(At(1)),
                "the element that lost the pseudo-class is not the one that was added, so nothing "
                + "would have marked it dirty");

            Assert.AreEqual("#222222", BackgroundOf(At(2)));
        }

        [TestMethod]
        public void RemovingTheLastChildPromotesTheOneBefore()
        {
            Build("row { background: #111111 }\r\nrow:last-child { background: #222222 }", 3);

            _list.RemoveChild(At(2));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(At(1)));
        }

        [TestMethod]
        public void InsertingInTheMiddleRenumbersEveryoneAfter()
        {
            Build("row { background: #111111 }\r\nrow:nth-child(3) { background: #222222 }", 3);

            Assert.AreEqual("#222222", BackgroundOf(At(2)));

            _list.InsertChild(0, Row("row"));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(At(2)),
                "an insertion shifts every index after it, which is why the whole sibling list is "
                + "marked rather than the neighbours of the change");

            Assert.AreEqual("#111111", BackgroundOf(At(3)));
        }

        [TestMethod]
        public void TheRootMatchesNothing()
        {
            Build("list:first-child { background: #222222 }", 1);

            Assert.IsNull(BackgroundOf(_list), "an element with no parent has no position");
        }

        [TestMethod]
        public void ChromeIsNotAChild()
        {
            Build("row { background: #111111 }\r\n#Scrollbar:first-child { background: #222222 }", 2);

            _list.Scrollable = true;
            _list.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            foreach (VisualElement chrome in _list.Chrome)
            {
                Assert.AreNotEqual("#222222", BackgroundOf(chrome),
                    "chrome is a second list the layout ignores, so it has no position among "
                    + "the children either");
            }
        }

        [TestMethod]
        public void ASheetThatSaysNothingPaysNothing()
        {
            Build("row { background: #111111 }", 2);

            Assert.IsFalse(_registry.HasStructuralClasses,
                "the whole path is gated on this, the way scoped, state and media classes are");
        }

        [TestMethod]
        public void OnlyTheKindsTheSheetUsesAreTried()
        {
            Build("row:last-child { background: #222222 }", 2);

            Assert.AreEqual(StructuralKinds.Last, _registry.Structural,
                "a candidate name is a string built per element, so only the kinds actually "
                + "declared may be assembled");
        }

        [TestMethod]
        public void TheKindsAreReadOutOfAScopeToo()
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile("group:only-child {\r\n    label { background: #222222 }\r\n}"));

            Assert.AreEqual(StructuralKinds.Only, _registry.Structural);
        }

        [TestMethod]
        public void ParenthesesReachTheSelectorTokenizer()
        {
            var source = new XnsSource("row:nth-child(12) { background: #222222 }");

            source.Compile();

            Assert.IsFalse(source.HasErrors,
                "the selector character set had no parenthesis, so this was XN001");
        }
    }
}
