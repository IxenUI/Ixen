using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class ChildCombinatorTests
    {
        private const int VIEWPORT = 200;

        private IxenSurface _surface;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static VisualElement Box(string name, params string[] classes)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (string c in classes)
            {
                box.Classes.Add(c);
            }

            return box;
        }

        private void Apply(string xns, VisualElement root)
        {
            var registry = new StyleRegistry();
            registry.Add(Compile(xns));

            _surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        [TestMethod]
        public void ADirectChildMatchesAndADeeperOneDoesNot()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement direct = Box("label");
            VisualElement wrapper = Box("wrapper");
            VisualElement deep = Box("label");

            wrapper.AddChild(deep);
            card.AddChildren(direct, wrapper);
            root.AddChild(card);

            Apply("card {\r\n    > label { background: #222222 }\r\n}", root);

            Assert.AreEqual("#222222", BackgroundOf(direct));

            Assert.IsNull(BackgroundOf(deep),
                "a scope is a descendant match by default, and this is the only way to say that "
                + "nothing may sit in between");
        }

        [TestMethod]
        public void WithoutTheMarkerBothStillMatch()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement direct = Box("label");
            VisualElement wrapper = Box("wrapper");
            VisualElement deep = Box("label");

            wrapper.AddChild(deep);
            card.AddChildren(direct, wrapper);
            root.AddChild(card);

            Apply("card {\r\n    label { background: #222222 }\r\n}", root);

            Assert.AreEqual("#222222", BackgroundOf(direct));
            Assert.AreEqual("#222222", BackgroundOf(deep), "the default is unchanged");
        }

        [TestMethod]
        public void ItChainsThroughSeveralLevels()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement card = Box("card");
            VisualElement label = Box("label");

            card.AddChild(label);
            page.AddChild(card);
            root.AddChild(page);

            Apply("page {\r\n    > card {\r\n        > label { background: #222222 }\r\n    }\r\n}", root);

            Assert.AreEqual("#222222", BackgroundOf(label));
        }

        [TestMethod]
        public void OneLooseLevelBreaksTheChain()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement filler = Box("filler");
            VisualElement card = Box("card");
            VisualElement label = Box("label");

            card.AddChild(label);
            filler.AddChild(card);
            page.AddChild(filler);
            root.AddChild(page);

            Apply("page {\r\n    > card {\r\n        > label { background: #222222 }\r\n    }\r\n}", root);

            Assert.IsNull(BackgroundOf(label),
                "the card is no longer a direct child of the page, so the whole chain fails even "
                + "though the label is still a direct child of the card");
        }

        [TestMethod]
        public void OnlyTheMarkedHopIsTight()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement filler = Box("filler");
            VisualElement card = Box("card");
            VisualElement label = Box("label");

            card.AddChild(label);
            filler.AddChild(card);
            page.AddChild(filler);
            root.AddChild(page);

            Apply("page {\r\n    card {\r\n        > label { background: #222222 }\r\n    }\r\n}", root);

            Assert.AreEqual("#222222", BackgroundOf(label),
                "the marker binds one hop, the one below the selector that carries it, and leaves "
                + "the rest of the chain a descendant match");
        }

        [TestMethod]
        public void ItWorksOnAClassAndOnAType()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement chip = Box("chip", "tag");

            card.AddChild(chip);
            root.AddChild(card);

            Apply("card {\r\n    > .tag { background: #222222 }\r\n}", root);

            Assert.AreEqual("#222222", BackgroundOf(chip));

            Apply(".holder {\r\n    > chip { background: #333333 }\r\n}", root);

            Assert.IsNull(BackgroundOf(chip), "the card is not a holder");

            card.AddClass("holder");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#333333", BackgroundOf(chip),
                "and the marker is about the hop, not about what kind of selector sits above it");
        }

        [TestMethod]
        public void TheMarkerSurvivesAStateOnTheParent()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement label = Box("label");

            card.AddChild(label);
            root.AddChild(card);

            Apply("card:hover {\r\n    > label { background: #222222 }\r\n}", root);

            Assert.IsNull(BackgroundOf(label));

            card.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(label),
                "a segment carries a state and a marker at once, and they are read from opposite "
                + "ends of the same string");
        }

        [TestMethod]
        public void ASpaceAfterTheMarkerIsAllowed()
        {
            var spaced = new XnsSource("card {\r\n    >    label { background: #222222 }\r\n}");
            var tight = new XnsSource("card {\r\n    >label { background: #222222 }\r\n}");

            spaced.Compile();
            tight.Compile();

            Assert.IsFalse(spaced.HasErrors);
            Assert.IsFalse(tight.HasErrors);
        }

        [TestMethod]
        public void AMarkerAtTopLevelIsASyntaxError()
        {
            var source = new XnsSource("> label { background: #222222 }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "there is no scope above a top-level selector, so the marker has nothing to bind "
                + "and saying so beats registering a rule whose marker is silently lost");
        }

        [TestMethod]
        public void TheNameLoosesTheMarkerAndTheScopeKeepsIt()
        {
            ClassesSet set = Compile("card {\r\n    > label { background: #222222 }\r\n}");

            StyleClass rule = set.Classes.Single();

            Assert.AreEqual("label", rule.Name,
                "the marker is a property of the scope, not part of the name anything looks up");

            Assert.AreEqual(">card", rule.Scope,
                "it moves to the segment above, which is the one the hop is measured from");
        }
    }
}
