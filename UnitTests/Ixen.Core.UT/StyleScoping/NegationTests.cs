using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class NegationTests
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

        private static VisualElement Row(string name, string type, params string[] classes)
        {
            var row = new VisualElement { Name = name, TypeName = type };

            foreach (string c in classes)
            {
                row.Classes.Add(c);
            }

            return row;
        }

        private void Build(string xns, int width = VIEWPORT, params string[] cardClasses)
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile(xns));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card = Row("card", null, cardClasses);
            _card.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card.AddChild(Row("row", null));
            _card.AddChild(Row("row", null, "wide"));
            _card.AddChild(Row("row", "Button"));
            _card.AddChild(Row("sep", null));

            root.AddChild(_card);

            _surface = new IxenSurface(root) { Styles = _registry };

            root.Invalidate();
            _surface.ComputeLayout(width, VIEWPORT);
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        private VisualElement Plain => _card.ChildElements[0];
        private VisualElement Wide => _card.ChildElements[1];
        private VisualElement Typed => _card.ChildElements[2];
        private VisualElement Sep => _card.ChildElements[3];

        [TestMethod]
        public void ANegationExcludesWhatItNames()
        {
            Build("row:not(.wide) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide), "the only row the rule has to skip");
            Assert.AreEqual("#111111", BackgroundOf(Typed));
        }

        [TestMethod]
        public void TheBareRuleStillReachesWhatTheNegatedOneSkips()
        {
            Build("row { background: #000000 }\r\nrow:not(.wide) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.AreEqual("#000000", BackgroundOf(Wide),
                "a negation narrows one rule, it does not remove the element from the others");
        }

        [TestMethod]
        public void ANegationCanNameAStructuralPseudoClass()
        {
            Build("row:not(:last-child) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.AreEqual("#111111", BackgroundOf(Wide));
            Assert.AreEqual("#111111", BackgroundOf(Typed));
            Assert.IsNull(BackgroundOf(Sep), "the last child, which is what the rule excludes");
        }

        [TestMethod]
        public void ANegationCanNameAType()
        {
            Build("row:not(#Button) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Typed));
        }

        [TestMethod]
        public void ANegationCanNameAnElementName()
        {
            Build("row:not(sep) { background: #111111 }\r\nsep { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain),
                "the negated name is tested against the element itself, not against a sibling");
        }

        [TestMethod]
        public void SeveralNegationsAllHaveToHold()
        {
            Build("row:not(.wide):not(#Button) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide));
            Assert.IsNull(BackgroundOf(Typed));
        }

        [TestMethod]
        public void ACommaInsideOneNegationSaysTheSameThing()
        {
            Build("row:not(.wide, #Button) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide), "not(a, b) is neither a nor b, as in CSS");
            Assert.IsNull(BackgroundOf(Typed));
        }

        [TestMethod]
        public void AStateAndANegationReadTheSameInEitherOrder()
        {
            Build("row:hover:not(.wide) { background: #111111 }");

            Plain.AddState("hover");
            Wide.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide));

            Build("row:not(.wide):hover { background: #111111 }");

            Plain.AddState("hover");
            Wide.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(Plain),
                "a negation is stripped off the selector, so where it was written cannot matter");
            Assert.IsNull(BackgroundOf(Wide));
        }

        [TestMethod]
        public void ANegationCanNameAStateOnItsOwn()
        {
            Build("row:not(:hover) { background: #111111 }");

            Wide.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide), "not(:hover) names no element, only a state");
        }

        [TestMethod]
        public void ANegationWorksInsideAScope()
        {
            Build("card {\r\n    row:not(.wide) { background: #111111 }\r\n}");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide));
        }

        [TestMethod]
        public void AScopeSegmentCarriesItsOwnNegation()
        {
            Build("card:not(.narrow) {\r\n    row { background: #111111 }\r\n}");

            Assert.AreEqual("#111111", BackgroundOf(Plain));

            Build("card:not(.narrow) {\r\n    row { background: #111111 }\r\n}",
                VIEWPORT, "narrow");

            Assert.IsNull(BackgroundOf(Plain),
                "the ancestor is what the negation excludes, so nothing inside it matches");
        }

        [TestMethod]
        public void ANegationWorksOnAClassTarget()
        {
            Build(".wide:not(#Button) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Wide));
        }

        [TestMethod]
        public void ANegationWorksOnATypeTarget()
        {
            Build("#Button:not(.wide) { background: #111111 }");

            Assert.AreEqual("#111111", BackgroundOf(Typed));
            Assert.IsNull(BackgroundOf(Wide));
        }

        [TestMethod]
        public void ANegationWorksInsideAMediaBlock()
        {
            const string xns = "@media (max-width: 300px) {\r\n"
                + "    row:not(.wide) { background: #111111 }\r\n"
                + "}";

            Build(xns, 200);

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide));

            Build(xns, 400);

            Assert.IsNull(BackgroundOf(Plain), "the breakpoint still decides whether the rule runs");
        }

        [TestMethod]
        public void TwoRulesThatDifferOnlyByTheirNegationAreTwoRules()
        {
            Build("row:not(.wide) { background: #111111 }\r\nrow:not(#Button) { color: #222222 }");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.AreEqual("#222222", Plain.StylesHandlers.Color.Descriptor?.Value);
            Assert.AreEqual("#222222", Wide.StylesHandlers.Color.Descriptor?.Value,
                "the second rule excludes the button, not the wide row");
        }

        [TestMethod]
        public void TwoMediaRulesThatDifferOnlyByTheirNegationAreTwoRules()
        {
            Build("@media (max-width: 300px) {\r\n"
                + "    row:not(.wide) { background: #111111 }\r\n"
                + "    row:not(#Button) { color: #222222 }\r\n"
                + "}", 200);

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.AreEqual("#222222", Plain.StylesHandlers.Color.Descriptor?.Value,
                "two conditional rules on one selector differ by their negation, so neither replaces the other");
        }

        [TestMethod]
        public void ANegationWorksInsideAContainerQuery()
        {
            Build("card {\r\n"
                + "    width: 300px\r\n"
                + "\r\n"
                + "    @container (max-width: 400px) {\r\n"
                + "        row:not(.wide) { background: #111111 }\r\n"
                + "        row:not(#Button) { color: #222222 }\r\n"
                + "    }\r\n"
                + "}");

            Assert.AreEqual("#111111", BackgroundOf(Plain));
            Assert.IsNull(BackgroundOf(Wide), "the container decides whether the rule runs, the negation who it reaches");
            Assert.AreEqual("#222222", Plain.StylesHandlers.Color.Descriptor?.Value);
            Assert.IsNull(Typed.StylesHandlers.Color.Descriptor?.Value);
        }

        [TestMethod]
        public void AStateMentionedOnlyInsideANegationStillTurnsTrackingOn()
        {
            var registry = new StyleRegistry();
            registry.Add(Compile("row:not(:hover) { background: #111111 }"));

            Assert.IsTrue(registry.HasStateClasses,
                "nothing would maintain the hover state, so the negation would always hold");
        }

        [TestMethod]
        public void AFocusMentionedOnlyInsideANegationSuppressesTheDefaultRing()
        {
            var registry = new StyleRegistry();
            registry.Add(Compile("row:not(:focus) { background: #111111 }"));

            Assert.IsTrue(registry.HasFocusClasses,
                "a sheet that names focus at all has decided how focus looks, negation included");
        }

        [TestMethod]
        public void ANegationRemovesTheDependenceOnClassOrder()
        {
            const string xns = ".chip:hover { background: #111111 }\r\n"
                + ".chip_on { background: #222222 }";

            const string guarded = ".chip:not(.chip_on):hover { background: #111111 }\r\n"
                + ".chip_on { background: #222222 }";

            Assert.AreEqual("#222222", Hovered(xns, "chip", "chip_on"),
                "written in that order the later class wins, which is the rule the demo relied on");
            Assert.AreEqual("#111111", Hovered(xns, "chip_on", "chip"),
                "and reversing the two silently breaks it");

            Assert.AreEqual("#222222", Hovered(guarded, "chip", "chip_on"));
            Assert.AreEqual("#222222", Hovered(guarded, "chip_on", "chip"),
                "a negation says it outright, so the order stops mattering");
        }

        private static string Hovered(string xns, params string[] classes)
        {
            var registry = new StyleRegistry();
            registry.Add(Compile(xns));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            VisualElement chip = Row("chip", null, classes);
            root.AddChild(chip);

            var surface = new IxenSurface(root) { Styles = registry };

            chip.AddState("hover");
            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return BackgroundOf(chip);
        }

        [TestMethod]
        public void ANegatedRuleIsRefusedAsADefault()
        {
            var registry = new StyleRegistry();
            var set = Compile("row:not(.wide) { background: #111111 }");

            registry.AddDefaults(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            VisualElement row = Row("row", null);
            root.AddChild(row);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(BackgroundOf(row),
                "the defaults layer is one rule per selector, and a negation is not part of the name");
        }

        [TestMethod]
        public void AnUnclosedNegationIsReported()
        {
            var source = new XnsSource("row:not(.wide { background: #111111 }");

            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics.Any(d => d.Message.Contains("closing parenthesis")),
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void ANestedNegationIsReported()
        {
            var source = new XnsSource("row:not(:not(.wide)) { background: #111111 }");

            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics.Any(d => d.Message.Contains("another")),
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void ADotOutsideAParenthesisIsStillASyntaxError()
        {
            var source = new XnsSource("row.wide { background: #111111 }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "a compound selector is not a thing, and widening the character set inside the "
                + "parentheses must not have made it one silently");
        }
    }
}
