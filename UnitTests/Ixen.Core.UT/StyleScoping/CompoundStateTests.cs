using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class CompoundStateTests
    {
        private const int VIEWPORT = 200;

        private StyleRegistry _registry;
        private IxenSurface _surface;
        private VisualElement _card;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static VisualElement Element(string name, string type, params string[] classes)
        {
            var element = new VisualElement { Name = name, TypeName = type };

            foreach (string c in classes)
            {
                element.Classes.Add(c);
            }

            return element;
        }

        private void Build(string xns, bool asDefaults = false)
        {
            _registry = new StyleRegistry();

            if (asDefaults)
            {
                _registry.AddDefaults(Compile(xns));
            }
            else
            {
                _registry.Add(Compile(xns));
            }

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card = Element("card", null);
            _card.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card.AddChild(Element("row", "Box", "field"));
            _card.AddChild(Element("row", "Box", "field"));

            VisualElement label = Element("label", null);
            _card.AddChild(label);

            root.AddChild(_card);

            _surface = new IxenSurface(root) { Styles = _registry };

            root.Invalidate();
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement First => _card.ChildElements[0];
        private VisualElement Second => _card.ChildElements[1];
        private VisualElement Label => _card.ChildElements[2];

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        private void With(VisualElement element, params string[] states)
        {
            foreach (string state in states)
            {
                element.AddState(state);
            }

            Layout();
        }

        [TestMethod]
        public void ACompoundAppliesOnlyWhenEveryStateHolds()
        {
            Build(".field:invalid:focus { background: #333333 }");

            Assert.IsNull(BackgroundOf(First));

            With(First, "invalid");
            Assert.IsNull(BackgroundOf(First), "one of the two states is not a match");

            With(Second, "focus");
            Assert.IsNull(BackgroundOf(Second), "the other one alone is not a match either");

            With(First, "focus");
            Assert.AreEqual("#333333", BackgroundOf(First));
        }

        [TestMethod]
        public void TheOrderTheStatesArriveInDoesNotMatter()
        {
            Build(".field:invalid:focus { background: #333333 }");

            With(First, "invalid", "focus");
            With(Second, "focus", "invalid");

            Assert.AreEqual("#333333", BackgroundOf(First));
            Assert.AreEqual("#333333", BackgroundOf(Second));
        }

        [TestMethod]
        public void TheOrderTheStatesAreWrittenInDoesNotMatter()
        {
            Build(".field:focus:invalid { background: #333333 }");

            With(First, "invalid", "focus");

            Assert.AreEqual("#333333", BackgroundOf(First));
        }

        [TestMethod]
        public void ACompoundBeatsEachOfItsStatesWhicheverWasAddedLast()
        {
            Build(".field:invalid:focus { background: #333333 }\r\n"
                + ".field:invalid { background: #111111 }\r\n"
                + ".field:focus { background: #222222 }");

            With(First, "invalid", "focus");
            With(Second, "focus", "invalid");

            Assert.AreEqual("#333333", BackgroundOf(First),
                "a single state is decided by the order of element.States; the compound is not");
            Assert.AreEqual("#333333", BackgroundOf(Second));
        }

        [TestMethod]
        public void ACompoundLeavesWhatItDoesNotDeclareToTheSingleStates()
        {
            Build(".field:invalid { background: #111111  color: #ABCDEF }\r\n"
                + ".field:invalid:focus { background: #333333 }");

            With(First, "invalid", "focus");

            Assert.AreEqual("#333333", BackgroundOf(First));
            Assert.AreEqual("#ABCDEF", First.StylesHandlers.Color.Descriptor?.Value);
        }

        [TestMethod]
        public void MoreStatesBeatFewerWhicheverIsDeclaredFirst()
        {
            Build(".field:invalid:focus:hover { background: #444444 }\r\n"
                + ".field:invalid:focus { background: #333333 }");

            With(First, "hover", "invalid", "focus");
            With(Second, "invalid", "focus");

            Assert.AreEqual("#444444", BackgroundOf(First));
            Assert.AreEqual("#333333", BackgroundOf(Second));
        }

        [TestMethod]
        public void ItWorksOnEveryTarget()
        {
            Build("#Box:hover:focus { background: #111111 }\r\n"
                + "row:hover:invalid { color: #222222 }");

            With(First, "hover", "focus", "invalid");

            Assert.AreEqual("#111111", BackgroundOf(First));
            Assert.AreEqual("#222222", First.StylesHandlers.Color.Descriptor?.Value);
        }

        [TestMethod]
        public void AStructuralPseudoClassCanBePartOfACompound()
        {
            Build("row:first-child:hover { background: #111111 }");

            With(First, "hover");
            With(Second, "hover");

            Assert.AreEqual("#111111", BackgroundOf(First));
            Assert.IsNull(BackgroundOf(Second), "hovered, but not the first child");
        }

        [TestMethod]
        public void AScopeSegmentCanCarryACompound()
        {
            Build("card:hover:focus { label { background: #111111 } }");

            With(_card, "hover");
            Assert.IsNull(BackgroundOf(Label), "the card is hovered but not focused");

            With(_card, "focus");
            Assert.AreEqual("#111111", BackgroundOf(Label));
        }

        [TestMethod]
        public void ANegationCanNameACompound()
        {
            Build("row:not(:hover:focus) { background: #111111 }");

            With(First, "hover");
            With(Second, "hover", "focus");

            Assert.AreEqual("#111111", BackgroundOf(First), "hovered alone is not what is excluded");
            Assert.IsNull(BackgroundOf(Second));
        }

        [TestMethod]
        public void AMediaBlockCanHoldACompound()
        {
            Build("@media (min-width: 1px) { .field:invalid:focus { background: #111111 } }");

            With(First, "invalid", "focus");

            Assert.AreEqual("#111111", BackgroundOf(First));
            Assert.IsNull(BackgroundOf(Second));
        }

        [TestMethod]
        public void AContainerBlockCanHoldACompound()
        {
            Build("card { @container (min-width: 1px) { row:invalid:focus { background: #111111 } } }");

            With(First, "invalid", "focus");

            Assert.AreEqual("#111111", BackgroundOf(First));
            Assert.IsNull(BackgroundOf(Second));
        }

        [TestMethod]
        public void TheDefaultsLayerCanHoldACompound()
        {
            Build("#Box:invalid:focus { background: #111111 }", asDefaults: true);

            With(First, "invalid", "focus");

            Assert.AreEqual("#111111", BackgroundOf(First));
            Assert.IsNull(BackgroundOf(Second));
        }

        [TestMethod]
        public void OnlyACompoundIsIndexed()
        {
            Build(".field:hover { background: #111111 }\r\nrow:first-child { background: #222222 }");

            Assert.IsFalse(_registry.HasCompoundClasses,
                "a sheet with single states only must pay nothing for compounds");

            Build(".field:hover:focus { background: #111111 }");

            Assert.IsTrue(_registry.HasCompoundClasses);
            Assert.AreEqual(1, _registry.CompoundsOf(StyleClassTarget.ClassName, "field").Count);
        }

        [TestMethod]
        public void TheSameCompoundDeclaredTwiceIsIndexedOnce()
        {
            Build(".field:hover:focus { background: #111111 }\r\n.field:hover:focus { color: #222222 }");

            Assert.AreEqual(1, _registry.CompoundsOf(StyleClassTarget.ClassName, "field").Count);
        }
    }
}
