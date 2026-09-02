using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class MediaTests
    {
        private static StyleRegistry Registry(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            return registry;
        }

        private static VisualElement Element(string name, params string[] classes)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (string c in classes)
            {
                element.Classes.Add(c);
            }

            return element;
        }

        private static IxenSurface Surface(string xns, VisualElement root, int width, int height)
        {
            var surface = new IxenSurface(root) { Styles = Registry(xns) };
            root.Invalidate();
            surface.ComputeLayout(width, height);

            return surface;
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        private const string BREAKPOINT =
            "box {\r\n    background: #111111\r\n}\r\n"
            + "@media (max-width: 600px) {\r\n    box {\r\n        background: #222222\r\n    }\r\n}";

        [TestMethod]
        public void ATopLevelBlockOverridesTheBaseRuleWhenItMatches()
        {
            VisualElement box = Element("box");
            Surface(BREAKPOINT, box, 500, 400);

            Assert.AreEqual("#222222", BackgroundOf(box));
        }

        [TestMethod]
        public void TheBaseRuleStandsWhenTheQueryDoesNotMatch()
        {
            VisualElement box = Element("box");
            Surface(BREAKPOINT, box, 900, 400);

            Assert.AreEqual("#111111", BackgroundOf(box));
        }

        [TestMethod]
        public void CrossingTheBreakpointRestyles()
        {
            VisualElement box = Element("box");
            IxenSurface surface = Surface(BREAKPOINT, box, 900, 400);

            Assert.AreEqual("#111111", BackgroundOf(box));

            surface.ComputeLayout(500, 400);

            Assert.AreEqual("#222222", BackgroundOf(box),
                "a resize across a breakpoint must invalidate, or the style pass skips a clean element");

            surface.ComputeLayout(900, 400);

            Assert.AreEqual("#111111", BackgroundOf(box), "and back again");
        }

        [TestMethod]
        public void AResizeThatChangesNothingDoesNotNeedToRestyle()
        {
            VisualElement box = Element("box");
            IxenSurface surface = Surface(BREAKPOINT, box, 900, 400);

            surface.ComputeLayout(950, 420);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "still on the same side of the breakpoint");
        }

        [TestMethod]
        public void ANestedBlockAppliesToTheEnclosingSelector()
        {
            VisualElement box = Element("box");
            Surface(
                "box {\r\n    background: #111111\r\n\r\n"
                + "    @media (max-width: 600px) {\r\n        background: #333333\r\n    }\r\n}",
                box, 500, 400);

            Assert.AreEqual("#333333", BackgroundOf(box),
                "bare declarations inside a nested block belong to the selector around it");
        }

        [TestMethod]
        public void ANestedBlockIsInertOutsideItsRange()
        {
            VisualElement box = Element("box");
            Surface(
                "box {\r\n    background: #111111\r\n\r\n"
                + "    @media (max-width: 600px) {\r\n        background: #333333\r\n    }\r\n}",
                box, 900, 400);

            Assert.AreEqual("#111111", BackgroundOf(box));
        }

        [TestMethod]
        public void ABlockDoesNotCreateAScope()
        {
            VisualElement root = Element("root");
            VisualElement box = Element("box");
            root.AddChild(box);

            Surface(
                "@media (max-width: 600px) {\r\n    box {\r\n        background: #222222\r\n    }\r\n}",
                root, 500, 400);

            Assert.AreEqual("#222222", BackgroundOf(box),
                "nesting inside @media must not scope the selector under it");
        }

        [TestMethod]
        public void AScopeInsideABlockStillScopes()
        {
            VisualElement card = Element("card");
            VisualElement label = Element("label");
            card.AddChild(label);

            VisualElement loose = Element("label");
            var root = Element("root");
            root.AddChildren(card, loose);

            Surface(
                "@media (max-width: 600px) {\r\n    card {\r\n        label {\r\n"
                + "            background: #444444\r\n        }\r\n    }\r\n}",
                root, 500, 400);

            Assert.AreEqual("#444444", BackgroundOf(label));
            Assert.IsNull(BackgroundOf(loose), "the one outside the card is untouched");
        }

        [TestMethod]
        public void NestedBlocksCombineTheirConditions()
        {
            const string xns =
                "@media (max-width: 800px) {\r\n"
                + "    @media (orientation: portrait) {\r\n"
                + "        box {\r\n            background: #555555\r\n        }\r\n"
                + "    }\r\n}";

            VisualElement portrait = Element("box");
            Surface(xns, portrait, 400, 900);
            Assert.AreEqual("#555555", BackgroundOf(portrait), "narrow and portrait");

            VisualElement landscape = Element("box");
            Surface(xns, landscape, 700, 400);
            Assert.IsNull(BackgroundOf(landscape), "narrow but landscape");

            VisualElement wide = Element("box");
            Surface(xns, wide, 900, 1200);
            Assert.IsNull(BackgroundOf(wide), "portrait but wide");
        }

        [TestMethod]
        public void AMediaRuleBeatsAStateRuleOnTheSameSelector()
        {
            VisualElement box = Element("box");
            IxenSurface surface = Surface(
                "box {\r\n    background: #111111\r\n}\r\n"
                + "box:hover {\r\n    background: #222222\r\n}\r\n"
                + "@media (max-width: 600px) {\r\n    box {\r\n        background: #333333\r\n    }\r\n}",
                box, 500, 400);

            box.AddState("hover");
            surface.ComputeLayout(500, 400);

            Assert.AreEqual("#333333", BackgroundOf(box),
                "media variants are applied after the states of the same selector");
        }

        [TestMethod]
        public void AStateInsideABlockStillWins()
        {
            VisualElement box = Element("box");
            IxenSurface surface = Surface(
                "box {\r\n    background: #111111\r\n}\r\n"
                + "@media (max-width: 600px) {\r\n"
                + "    box {\r\n        background: #333333\r\n    }\r\n"
                + "    box:hover {\r\n        background: #444444\r\n    }\r\n}",
                box, 500, 400);

            box.AddState("hover");
            surface.ComputeLayout(500, 400);

            Assert.AreEqual("#444444", BackgroundOf(box));
        }

        [TestMethod]
        public void ASheetWithoutAnyQueryPaysNothing()
        {
            StyleRegistry registry = Registry("box {\r\n    background: #111111\r\n}");

            Assert.IsFalse(registry.HasMediaClasses,
                "the gate is what keeps every other application free of the media path");
        }

        [TestMethod]
        public void AMalformedQueryIsReportedRatherThanIgnored()
        {
            var source = new XnsSource(
                "@media (max-girth: 600px) {\r\n    box {\r\n        background: #222222\r\n    }\r\n}");

            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void ATopLevelBlockHoldingBareStylesIsReported()
        {
            var source = new XnsSource("@media (max-width: 600px) {\r\n    background: #222222\r\n}");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "there is no selector for those declarations to belong to");
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void TwoQueriesOnTheSameSelectorBothSurvive()
        {
            const string xns =
                "box {\r\n    background: #111111\r\n}\r\n"
                + "@media (max-width: 600px) {\r\n    box {\r\n        background: #222222\r\n    }\r\n}\r\n"
                + "@media (min-width: 900px) {\r\n    box {\r\n        background: #333333\r\n    }\r\n}";

            VisualElement narrow = Element("box");
            Surface(xns, narrow, 500, 400);
            Assert.AreEqual("#222222", BackgroundOf(narrow),
                "the second block must not have overwritten the first in the registry");

            VisualElement wide = Element("box");
            Surface(xns, wide, 1000, 400);
            Assert.AreEqual("#333333", BackgroundOf(wide));

            VisualElement middle = Element("box");
            Surface(xns, middle, 700, 400);
            Assert.AreEqual("#111111", BackgroundOf(middle));
        }
        [TestMethod]
        public void AListOfConditionsComesFromXnsEndToEnd()
        {
            VisualElement box = Element("box");

            IxenSurface surface = Surface(
                "box { background: #111111 }\r\n"
                + "@media (max-width: 400px), (min-width: 800px) {\r\n"
                + "    box { background: #222222 }\r\n"
                + "}", box, 300, 100);

            Assert.AreEqual("#222222", BackgroundOf(box), "the narrow side");

            surface.ComputeLayout(600, 100);

            Assert.AreEqual("#111111", BackgroundOf(box), "neither side in the middle");

            surface.ComputeLayout(900, 100);

            Assert.AreEqual("#222222", BackgroundOf(box), "the wide side");
        }

        [TestMethod]
        public void NotComesFromXnsEndToEnd()
        {
            VisualElement box = Element("box");

            IxenSurface surface = Surface(
                "box { background: #111111 }\r\n"
                + "@media not (max-width: 600px) {\r\n"
                + "    box { background: #222222 }\r\n"
                + "}", box, 500, 100);

            Assert.AreEqual("#111111", BackgroundOf(box));

            surface.ComputeLayout(700, 100);

            Assert.AreEqual("#222222", BackgroundOf(box));
        }

        [TestMethod]
        public void TwoNamedBreakpointsCompose()
        {
            VisualElement box = Element("box");

            IxenSurface surface = Surface(
                "\u0024phone:  (max-width: 400px)\r\n"
                + "\u0024tablet: (min-width: 800px)\r\n"
                + "box { background: #111111 }\r\n"
                + "@media \u0024phone or \u0024tablet {\r\n"
                + "    box { background: #222222 }\r\n"
                + "}", box, 300, 100);

            Assert.AreEqual("#222222", BackgroundOf(box),
                "a variable already worked as a whole condition; or is what finally lets two of "
                + "them compose, which is the other half of what this item asked for");

            surface.ComputeLayout(600, 100);

            Assert.AreEqual("#111111", BackgroundOf(box));

            surface.ComputeLayout(900, 100);

            Assert.AreEqual("#222222", BackgroundOf(box));
        }

        [TestMethod]
        public void ANestedBlockNarrowsADisjunction()
        {
            VisualElement box = Element("box");

            IxenSurface surface = Surface(
                "box { background: #111111 }\r\n"
                + "@media (max-width: 400px), (min-width: 800px) {\r\n"
                + "    @media (orientation: landscape) {\r\n"
                + "        box { background: #222222 }\r\n"
                + "    }\r\n"
                + "}", box, 300, 100);

            Assert.AreEqual("#222222", BackgroundOf(box), "narrow and landscape");

            surface.ComputeLayout(300, 500);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "still narrow, but portrait now, so the inner block narrows the whole list "
                + "rather than being added to it");
        }

    }
}
