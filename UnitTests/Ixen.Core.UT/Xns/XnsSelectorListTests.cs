using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsSelectorListTests
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

        private static List<StyleClass> ClassesOf(string xns)
            => Compile(xns).Classes;

        private static VisualElement Box(string name)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

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
        public void ACommaDeclaresTheSameRuleTwice()
        {
            List<StyleClass> classes = ClassesOf("title, subtitle { background: #FF0000 }");

            Assert.AreEqual(2, classes.Count);
            CollectionAssert.AreEqual(new[] { "title", "subtitle" },
                classes.Select(c => c.Name).ToArray());
            Assert.IsTrue(classes.All(c => c.Styles.Count == 1));
        }

        [TestMethod]
        public void EachEntryCarriesItsOwnTarget()
        {
            List<StyleClass> classes = ClassesOf(".card, #Button, plain { background: #FF0000 }");

            Assert.AreEqual(StyleClassTarget.ClassName, classes[0].Target);
            Assert.AreEqual("card", classes[0].Name);
            Assert.AreEqual(StyleClassTarget.ElementType, classes[1].Target);
            Assert.AreEqual("Button", classes[1].Name);
            Assert.AreEqual(StyleClassTarget.ElementName, classes[2].Target);
            Assert.AreEqual("plain", classes[2].Name);
        }

        [TestMethod]
        public void EachEntryCarriesItsOwnState()
        {
            List<StyleClass> classes = ClassesOf("action:hover, other { background: #FF0000 }");

            CollectionAssert.AreEqual(new[] { "action:hover", "other" },
                classes.Select(c => c.Name).ToArray());
        }

        [TestMethod]
        public void ANestedListKeepsTheScopeOfItsParent()
        {
            List<StyleClass> classes = ClassesOf("page { title, subtitle { background: #FF0000 } }");

            Assert.AreEqual(2, classes.Count);
            Assert.IsTrue(classes.All(c => c.Scope == "page"));
        }

        [TestMethod]
        public void AListAsTheScopeRepeatsWhatIsNestedInIt()
        {
            List<StyleClass> classes = ClassesOf("page, sheet { title { background: #FF0000 } }");

            Assert.AreEqual(2, classes.Count);
            Assert.IsTrue(classes.All(c => c.Name == "title"));
            CollectionAssert.AreEqual(new[] { "page", "sheet" },
                classes.Select(c => c.Scope).ToArray());
        }

        [TestMethod]
        public void TwoListsMultiply()
        {
            List<StyleClass> classes = ClassesOf(
                "page, sheet { title, subtitle { background: #FF0000 } }");

            Assert.AreEqual(4, classes.Count);
            CollectionAssert.AreEqual(
                new[] { "title/page", "title/sheet", "subtitle/page", "subtitle/sheet" },
                classes.Select(c => c.Name + "/" + c.Scope).ToArray());
        }

        [TestMethod]
        public void ThreeLevelsMultiplyToo()
        {
            List<StyleClass> classes = ClassesOf(
                "shell { page, sheet { title, subtitle { background: #FF0000 } } }");

            Assert.AreEqual(4, classes.Count);
            Assert.IsTrue(classes.All(c => c.Scope.StartsWith("shell" + StyleScope.SEPARATOR)));
        }

        [TestMethod]
        public void TheImmediateMarkerBelongsToItsOwnEntry()
        {
            List<StyleClass> classes = ClassesOf(
                "page { > title, subtitle { background: #FF0000 } }");

            Assert.AreEqual(">page", classes.Single(c => c.Name == "title").Scope,
                "only the marked entry is tied to its parent");
            Assert.AreEqual("page", classes.Single(c => c.Name == "subtitle").Scope);
        }

        [TestMethod]
        public void AnImmediateChildOfEveryAlternative()
        {
            List<StyleClass> classes = ClassesOf("page, sheet { > title { background: #FF0000 } }");

            CollectionAssert.AreEqual(new[] { ">page", ">sheet" },
                classes.Select(c => c.Scope).ToArray());
        }

        [TestMethod]
        public void AListMaySpanLines()
        {
            CollectionAssert.AreEqual(
                ClassesOf("title, subtitle { background: #FF0000 }").Select(c => c.Name).ToArray(),
                ClassesOf("title,\r\nsubtitle { background: #FF0000 }").Select(c => c.Name).ToArray(),
                "a long list is readable one selector per line");
        }

        [TestMethod]
        public void SpacesAroundTheCommaAreOptional()

        {
            CollectionAssert.AreEqual(
                ClassesOf("title,subtitle { background: #FF0000 }").Select(c => c.Name).ToArray(),
                ClassesOf("title ,  subtitle { background: #FF0000 }").Select(c => c.Name).ToArray());
        }

        [TestMethod]
        public void TheStylesAreParsedOnceForTheWholeList()
        {
            var source = new XnsSource("title, subtitle, other { width: nonsense }");
            source.Compile();

            Assert.AreEqual(1, source.Diagnostics.Count,
                "one bad value is one diagnostic, however many selectors share it");
        }

        [TestMethod]
        public void ATrailingCommaIsOneClearDiagnostic()
        {
            var source = new XnsSource("title, { background: #FF0000 }");
            source.Tokenize();

            Assert.AreEqual(1, source.Diagnostics.Count,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual("title",
                source.GetTokens().First(t => t.Type == XnsTokenType.ClassName).Content,
                "the reader keeps the entries it read rather than piling a second error on top");
        }

        [TestMethod]
        public void AListInsideAMediaBlockIsConditional()
        {
            List<StyleClass> classes = ClassesOf(
                "@media (max-width: 400px) { title, subtitle { background: #FF0000 } }");

            Assert.AreEqual(2, classes.Count);
            Assert.IsTrue(classes.All(c => c.Media != null));
        }

        [TestMethod]
        public void BareDeclarationsInAMediaBlockReachEveryEntryOfTheListAboveIt()
        {
            List<StyleClass> classes = ClassesOf(
                "title, subtitle { @media (max-width: 400px) { background: #FF0000 } }");

            Assert.AreEqual(2, classes.Count);
            CollectionAssert.AreEqual(new[] { "title", "subtitle" },
                classes.Select(c => c.Name).ToArray());
            Assert.IsTrue(classes.All(c => c.Media != null));
        }

        [TestMethod]
        public void AKeyframeStopListSharesOneBlock()
        {
            ClassesSet set = Compile(@"@keyframes pulse {
    0%, 100% { background: #FF0000 }
    50% { background: #00FF00 }
}");

            KeyframesSet keyframes = set.Keyframes.Single();

            Assert.AreEqual(3, keyframes.Frames.Count);
            CollectionAssert.AreEqual(new[] { 0f, 1f, 0.5f },
                keyframes.Frames.Select(f => f.Offset).ToArray());
        }

        [TestMethod]
        public void AListIsOneTokenWhoseContentIsNormalised()
        {
            var source = new XnsSource("page { > title , subtitle { background: #FF0000 } }");
            source.Tokenize();

            List<XnsToken> names = source.GetTokens()
                .Where(t => t.Type == XnsTokenType.ClassName)
                .ToList();

            Assert.AreEqual(2, names.Count, "the whole list is one selector token");
            Assert.AreEqual(">title,subtitle", names[1].Content);
            Assert.AreEqual("> title , subtitle".Length, names[1].Length,
                "the length is the source span, so colouring covers the whole list");
        }

        [TestMethod]
        public void BothSelectorsActuallyApply()
        {
            VisualElement root = Box("root");
            VisualElement title = Box("title");
            VisualElement subtitle = Box("subtitle");
            VisualElement other = Box("other");

            root.AddChildren(title, subtitle, other);

            Apply("title, subtitle { background: #FF0000 }", root);

            Assert.AreEqual("#FF0000", BackgroundOf(title));
            Assert.AreEqual("#FF0000", BackgroundOf(subtitle));
            Assert.IsNull(BackgroundOf(other));
        }

        [TestMethod]
        public void AListedScopeAppliesUnderEachOfItsAlternatives()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement sheet = Box("sheet");
            VisualElement loose = Box("loose");
            VisualElement inPage = Box("title");
            VisualElement inSheet = Box("title");
            VisualElement inLoose = Box("title");

            page.AddChild(inPage);
            sheet.AddChild(inSheet);
            loose.AddChild(inLoose);
            root.AddChildren(page, sheet, loose);

            Apply("page, sheet { title { background: #FF0000 } }", root);

            Assert.AreEqual("#FF0000", BackgroundOf(inPage));
            Assert.AreEqual("#FF0000", BackgroundOf(inSheet));
            Assert.IsNull(BackgroundOf(inLoose));
        }
    }
}
