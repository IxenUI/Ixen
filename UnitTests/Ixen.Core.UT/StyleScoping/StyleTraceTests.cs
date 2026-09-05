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
    public class StyleTraceTests
    {
        private const int VIEWPORT = 200;

        private IxenSurface _surface;
        private VisualElement _card;
        private VisualElement _row;
        private VisualElement _other;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private StyleTrace Explain(string xns, int width = VIEWPORT, params string[] classes)
        {
            var registry = new StyleRegistry();
            registry.Add(Compile(xns));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card = new VisualElement { Name = "card" };
            _card.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };

            _row = new VisualElement { Name = "row", TypeName = "Button" };

            foreach (string c in classes)
            {
                _row.Classes.Add(c);
            }

            _other = new VisualElement { Name = "row" };

            _card.AddChild(_row);
            _card.AddChild(_other);
            root.AddChild(_card);

            _surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            _surface.ComputeLayout(width, VIEWPORT);

            return _surface.ExplainStyles(_row);
        }

        [TestMethod]
        public void TheRulesThatAppliedAreListedInTheOrderTheyWereApplied()
        {
            _row = null;

            StyleTrace trace = Explain(
                "#Button { background: #111111 }\r\n"
                + ".chip { background: #222222 }\r\n"
                + "row { background: #333333 }", VIEWPORT, "chip");

            CollectionAssert.AreEqual(
                new[] { "#Button", ".chip", "row" },
                trace.Applied.Select(a => a.Selector).ToArray(),
                "type, then class, then element name - the order the cascade actually walks");
        }

        [TestMethod]
        public void TheWinnerOfAPropertyIsTheLastRuleThatSetIt()
        {
            StyleTrace trace = Explain(
                "row { background: #111111  color: #AAAAAA }\r\n"
                + "row:hover { background: #222222 }");

            _row.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            trace = _surface.ExplainStyles(_row);

            Assert.AreEqual("row:hover", trace.WinnerOf(StyleIdentifier.BACKGROUND).Selector);
            Assert.AreEqual("row", trace.WinnerOf(StyleIdentifier.COLOR).Selector,
                "the state rule says nothing about the colour, so the base rule still holds it");
        }

        [TestMethod]
        public void APropertyNobodySetHasNoWinner()
        {
            StyleTrace trace = Explain("row { background: #111111 }");

            Assert.IsNull(trace.WinnerOf(StyleIdentifier.MARGIN));
        }

        [TestMethod]
        public void EveryPropertyAnyRuleSetIsListedOnce()
        {
            StyleTrace trace = Explain(
                "row { background: #111111  color: #AAAAAA }\r\n"
                + "#Button { background: #222222 }");

            CollectionAssert.AreEquivalent(
                new[] { StyleIdentifier.BACKGROUND, StyleIdentifier.COLOR },
                trace.Properties.ToArray());
        }

        [TestMethod]
        public void AScopedRuleReportsItsScope()
        {
            StyleTrace trace = Explain("card {\r\n    row { background: #111111 }\r\n}");

            Assert.AreEqual("card", trace.Applied.Single().Scope);
        }

        [TestMethod]
        public void AConditionalRuleReportsItsQuery()
        {
            StyleTrace trace = Explain(
                "@media (max-width: 300px) {\r\n    row { background: #111111 }\r\n}", 200);

            Assert.AreEqual("(max-width: 300px)", trace.Applied.Single().Media);
            Assert.IsNull(trace.Applied.Single().Container);

            trace = Explain("card {\r\n"
                + "    @container (max-width: 400px) {\r\n"
                + "        row { background: #111111 }\r\n"
                + "    }\r\n"
                + "}");

            Assert.AreEqual("(max-width: 400px)", trace.Applied.Single().Container);
        }

        [TestMethod]
        public void ANegatedSelectorIsWrittenBackTheWayItReads()
        {
            StyleTrace trace = Explain("row:not(.wide):not(:last-child) { background: #111111 }");

            Assert.AreEqual("row:not(.wide):not(:last-child)", trace.Applied.Single().Selector);
        }

        [TestMethod]
        public void ASelectorKeepsItsSigil()
        {
            Assert.AreEqual(".chip",
                Explain(".chip { background: #111111 }", VIEWPORT, "chip").Applied.Single().Selector);

            Assert.AreEqual("#Button",
                Explain("#Button { background: #111111 }").Applied.Single().Selector);
        }

        [TestMethod]
        public void TheElementsOwnFactsComeWithIt()
        {
            StyleTrace trace = Explain("row { background: #111111 }", VIEWPORT, "chip");

            _row.AddState("hover");
            trace = _surface.ExplainStyles(_row);

            Assert.AreEqual("row", trace.Name);
            Assert.AreEqual("Button", trace.TypeName,
                "a null type name is the usual reason a #Type rule matched nothing");
            CollectionAssert.AreEqual(new[] { "chip" }, trace.Classes.ToArray());
            CollectionAssert.AreEqual(new[] { "hover" }, trace.States.ToArray());
            Assert.AreEqual(0, trace.ChildIndex);
            Assert.AreEqual(2, trace.ChildCount);
        }

        [TestMethod]
        public void ARuleFromTheDefaultsLayerSaysSo()
        {
            var registry = new StyleRegistry();
            registry.AddDefaults(Compile("#Button { background: #111111  padding: 4px }"));
            registry.Add(Compile("#Button { background: #222222 }"));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var button = new VisualElement { Name = "press", TypeName = "Button" };
            root.AddChild(button);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            StyleTrace trace = surface.ExplainStyles(button);

            Assert.AreEqual(2, trace.Applied.Count);
            Assert.IsTrue(trace.Applied[0].IsDefault,
                "two rules on one selector are unreadable unless the theme's is named as such");
            Assert.IsFalse(trace.Applied[1].IsDefault);

            Assert.IsFalse(trace.WinnerOf(StyleIdentifier.BACKGROUND).IsDefault);
            Assert.IsTrue(trace.WinnerOf(StyleIdentifier.PADDING).IsDefault,
                "the application said nothing about the padding, so the theme still holds it");
        }

        [TestMethod]
        public void ARuleThatDidNotApplyIsNotListed()
        {
            StyleTrace trace = Explain(
                "row { background: #111111 }\r\n"
                + "row:not(#Button) { color: #AAAAAA }\r\n"
                + "sep { background: #222222 }");

            CollectionAssert.AreEqual(new[] { "row" },
                trace.Applied.Select(a => a.Selector).ToArray(),
                "a trace is what happened, not what could have");
        }

        [TestMethod]
        public void ADirtyTreeDoesNotLeakOtherElementsRulesIn()
        {
            Explain("row { background: #111111 }\r\ncard { padding: 4px }");

            _card.Invalidate();

            StyleTrace trace = _surface.ExplainStyles(_row);

            CollectionAssert.AreEqual(new[] { "row" },
                trace.Applied.Select(a => a.Selector).ToArray(),
                "the pass resolves whatever else is dirty in the same walk, so the trace has to "
                + "ask which element it is looking at rather than assume it is alone");
        }

        [TestMethod]
        public void TracingOneElementSaysNothingAboutItsSibling()
        {
            Explain("row { background: #111111 }");

            StyleTrace other = _surface.ExplainStyles(_other);

            Assert.AreEqual(1, other.Applied.Count);
            Assert.AreEqual(1, other.ChildIndex, "and it captures that sibling's own place");
        }

        [TestMethod]
        public void ExplainingTwiceGivesTheSameAnswer()
        {
            Explain("row { background: #111111 }\r\n#Button { color: #AAAAAA }");

            Assert.AreEqual(2, _surface.ExplainStyles(_row).Applied.Count);
            Assert.AreEqual(2, _surface.ExplainStyles(_row).Applied.Count,
                "the trace is taken down after the pass, so nothing accumulates");
        }

        [TestMethod]
        public void AnOrdinaryFrameCannotGoOnWritingIntoTheTrace()
        {
            StyleTrace trace = Explain("row { background: #111111 }");

            Assert.AreEqual(1, trace.Applied.Count);

            _row.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, trace.Applied.Count,
                "the trace is taken down in a finally, so the next frame records into nothing");
        }

        [TestMethod]
        public void ExplainingChangesNothingOnScreen()
        {
            Explain("row { background: #111111 }\r\nrow:hover { background: #222222 }");

            _row.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            string before = _row.StylesHandlers.Background.Descriptor?.Color;

            _surface.ExplainStyles(_row);

            Assert.AreEqual(before, _row.StylesHandlers.Background.Descriptor?.Color,
                "asking why must not change the answer");
        }

        [TestMethod]
        public void AnElementOutsideTheTreeExplainsNothing()
        {
            Explain("row { background: #111111 }");

            StyleTrace trace = _surface.ExplainStyles(new VisualElement { Name = "row" });

            Assert.AreEqual(0, trace.Applied.Count,
                "the pass walks the tree, so an element that is not in it is never reached");
        }
    }
}
