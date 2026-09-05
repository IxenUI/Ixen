using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsContainerQueryTests
    {
        private const int VIEWPORT = 600;

        private IxenSurface _surface;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static IReadOnlyList<LanguageError> Errors(string xns)
        {
            var source = new XnsSource(xns);
            source.Compile();

            return source.Diagnostics;
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

        private static VisualElement Card(string name, params VisualElement[] children)
        {
            VisualElement card = Box(name, "card");
            card.AddChildren(children);

            return card;
        }

        private const string NARROW_WIDE = @"
narrow { width: 150px }
wide   { width: 300px }

.card {
    @container (max-width: 200px) {
        title { background: #FF0000 }
    }
}";

        [TestMethod]
        public void TheEnclosingSelectorIsTheContainer()
        {
            StyleClass rule = Compile(NARROW_WIDE).Classes.Single(c => c.Name == "title");

            Assert.IsNotNull(rule.Container);
            Assert.AreEqual(".card", rule.Scope);
            Assert.AreEqual(1, rule.ContainerDepth,
                "the container is the selector the block was written inside");
        }

        [TestMethod]
        public void TwoContainersOfDifferentWidthsDecideSeparately()
        {
            VisualElement inNarrow = Box("title");
            VisualElement inWide = Box("title");
            VisualElement root = Box("root");

            root.AddChildren(Card("narrow", inNarrow), Card("wide", inWide));

            Apply(NARROW_WIDE, root);

            Assert.AreEqual("#FF0000", BackgroundOf(inNarrow));
            Assert.IsNull(BackgroundOf(inWide),
                "the same rule reads each container's own box");
        }

        [TestMethod]
        public void ItIsTheDeclaringSelectorRatherThanTheNearestScopeSegment()
        {
            VisualElement title = Box("title");
            VisualElement inner = Box("inner");
            VisualElement root = Box("root");

            inner.AddChild(title);
            inner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            root.AddChild(Card("wide", inner));

            Apply(@"
wide { width: 300px }

.card {
    @container (max-width: 200px) {
        inner { title { background: #FF0000 } }
    }
}", root);

            Assert.IsNull(BackgroundOf(title),
                "the card is the container, not the 100px box between it and the title");
        }

        [TestMethod]
        public void GrowingTheContainerRestylesWhatIsInsideIt()
        {
            VisualElement title = Box("title");
            VisualElement card = Box("card", "card", "small");
            VisualElement root = Box("root");

            card.AddChild(title);
            root.AddChild(card);

            Apply(@"
.small { width: 150px }
.big   { width: 400px }

.card {
    @container (max-width: 200px) {
        title { background: #FF0000 }
    }
}", root);

            Assert.AreEqual("#FF0000", BackgroundOf(title));

            card.RemoveClass("small");
            card.AddClass("big");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(BackgroundOf(title),
                "the frame that changes the size is the frame that drops the rule");
        }

        [TestMethod]
        public void TheFirstFrameIsAlreadyRight()
        {
            VisualElement title = Box("title");
            VisualElement root = Box("root");

            root.AddChild(Card("narrow", title));

            Apply(NARROW_WIDE, root);

            Assert.AreEqual("#FF0000", BackgroundOf(title),
                "the settling pass runs inside the first layout, so nothing is drawn stale");
        }

        [TestMethod]
        public void AContainerQueryReadsTheContentBoxRatherThanTheBorderBox()
        {
            VisualElement title = Box("title");
            VisualElement card = Card("narrow", title);
            VisualElement root = Box("root");

            root.AddChild(card);

            Apply(@"
narrow { width: 220px  padding: 30px }

.card {
    @container (max-width: 200px) {
        title { background: #FF0000 }
    }
}", root);

            Assert.AreEqual("#FF0000", BackgroundOf(title),
                "220 minus 60 of padding is what the children actually get");
        }

        [TestMethod]
        public void OrientationWorksOnAContainerToo()
        {
            VisualElement title = Box("title");
            VisualElement root = Box("root");

            root.AddChild(Card("box", title));

            Apply(@"
box { width: 100px  height: 300px }

.card {
    @container (orientation: portrait) {
        title { background: #FF0000 }
    }
}", root);

            Assert.AreEqual("#FF0000", BackgroundOf(title));
        }

        [TestMethod]
        public void AMediaBlockAroundAContainerBlockKeepsBoth()
        {
            List<StyleClass> classes = Compile(@"
.card {
    @media (min-width: 500px) {
        @container (max-width: 200px) {
            title { background: #FF0000 }
        }
    }
}").Classes;

            StyleClass rule = classes.Single(c => c.Name == "title");

            Assert.IsNotNull(rule.Media);
            Assert.IsNotNull(rule.Container);
        }

        [TestMethod]
        public void AContainerRuleUnderAMediaBlockStillWaitsForItsBreakpoint()
        {
            VisualElement title = Box("title");
            VisualElement root = Box("root");

            root.AddChild(Card("narrow", title));

            Apply(@"
narrow { width: 150px }

.card {
    @media (min-width: 1000px) {
        @container (max-width: 200px) {
            title { background: #FF0000 }
        }
    }
}", root);

            Assert.IsNull(BackgroundOf(title),
                "the container is narrow enough, the viewport is not");
        }

        [TestMethod]
        public void TwoContainerBlocksWithNothingBetweenThemAreCombined()
        {
            StyleClass rule = Compile(@"
.card {
    @container (min-width: 100px) {
        @container (max-width: 200px) {
            title { background: #FF0000 }
        }
    }
}").Classes.Single(c => c.Name == "title");

            Assert.AreEqual("(min-width: 100px) and (max-width: 200px)", rule.Container.Source);
            Assert.AreEqual(1, rule.ContainerDepth);
        }

        [TestMethod]
        public void TwoContainerBlocksWithASelectorBetweenThemAreRefused()
        {
            IReadOnlyList<LanguageError> errors = Errors(@"
.card {
    @container (min-width: 100px) {
        inner {
            @container (max-width: 200px) {
                title { background: #FF0000 }
            }
        }
    }
}");

            Assert.AreEqual(1, errors.Count,
                string.Join(" | ", errors.Select(e => e.Message)));
            StringAssert.Contains(errors[0].Message, "different containers");
        }

        [TestMethod]
        public void ATopLevelContainerBlockHasNothingToMeasure()
        {
            IReadOnlyList<LanguageError> errors = Errors(
                "@container (max-width: 200px) { title { background: #FF0000 } }");

            Assert.AreEqual(1, errors.Count,
                string.Join(" | ", errors.Select(e => e.Message)));
            StringAssert.Contains(errors[0].Message, "no container to measure");
        }

        [TestMethod]
        public void AContainerBlockCannotStyleItsOwnContainer()
        {
            IReadOnlyList<LanguageError> errors = Errors(@"
.card {
    @container (max-width: 200px) {
        background: #FF0000
    }
}");

            Assert.AreEqual(1, errors.Count,
                string.Join(" | ", errors.Select(e => e.Message)));
            StringAssert.Contains(errors[0].Message, "never the container itself");
        }

        [TestMethod]
        public void AMediaBlockInsideAContainerCannotStyleTheContainerEither()
        {
            IReadOnlyList<LanguageError> errors = Errors(@"
.card {
    @container (max-width: 200px) {
        @media (min-width: 100px) {
            background: #FF0000
        }
    }
}");

            Assert.AreEqual(1, errors.Count,
                string.Join(" | ", errors.Select(e => e.Message)));
            StringAssert.Contains(errors[0].Message, "never the container itself");
        }

        [TestMethod]
        public void ANonsenseConditionIsReported()
        {
            IReadOnlyList<LanguageError> errors = Errors(
                ".card { @container (wobble: 3) { title { background: #FF0000 } } }");

            Assert.AreEqual(1, errors.Count,
                string.Join(" | ", errors.Select(e => e.Message)));
            StringAssert.Contains(errors[0].Message, "not a valid container query");
        }

        [TestMethod]
        public void AGeneratedSheetCarriesTheQueryAndItsDepth()
        {
            StyleClass rule = new Ixen.StyleSheets.AllGeneratedStyles_StyleSheet().Classes
                .Single(c => c.Name == "generated_container_child");

            Assert.AreEqual("(max-width: 400px)", rule.Container.Source);
            Assert.AreEqual(1, rule.ContainerDepth);
        }

        [TestMethod]
        public void TheHeaderIsOneTokenSpanningItsWholeSource()
        {
            string text = ".card { @container (max-width: 200px) { title { background: #FF0000 } } }";
            var source = new XnsSource(text);
            source.Tokenize();

            XnsToken header = source.GetTokens()
                .Single(t => t.Type == XnsTokenType.ContainerQuery);

            Assert.AreEqual("(max-width: 200px)", header.Content);
            Assert.AreEqual("@container (max-width: 200px)",
                text.Substring(header.Index, header.Length).TrimEnd(),
                "the span runs from the marker to the opening brace");
        }

        [TestMethod]
        public void AStateOnTheContainedSelectorStillWorks()
        {
            VisualElement title = Box("title");
            VisualElement root = Box("root");

            root.AddChild(Card("narrow", title));

            Apply(@"
narrow { width: 150px }

.card {
    @container (max-width: 200px) {
        title:hover { background: #FF0000 }
    }
}", root);

            Assert.IsNull(BackgroundOf(title));

            title.AddState(StyleStates.HOVER);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#FF0000", BackgroundOf(title));
        }
    }
}
