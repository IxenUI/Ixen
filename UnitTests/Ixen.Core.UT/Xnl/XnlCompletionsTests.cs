using Ixen.Core.Language.Xnl;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlCompletionsTests
    {
        private static XnlCompletionContext At(string content)
        {
            int caret = content.IndexOf('|');
            Assert.IsTrue(caret >= 0, "the fixture must mark the caret with a pipe");

            return XnlCompletions.At(content.Remove(caret, 1), caret);
        }

        [TestMethod]
        public void AnElementNamePositionProposesNothing()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("cont|").Kind);
            Assert.AreEqual(XnlCompletionKind.None, At("container {} [\r\n    hea|\r\n]").Kind);
        }

        [TestMethod]
        public void AfterAnAngleBracketTheTypesAreProposed()
        {
            XnlCompletionContext context = At("title<Tex|");

            Assert.AreEqual(XnlCompletionKind.ElementType, context.Kind);
            Assert.AreEqual(3, context.SpanLength);
            CollectionAssert.Contains(context.Items.ToArray(), nameof(TextField));
            CollectionAssert.Contains(context.Items.ToArray(), nameof(VisualElement));
        }

        [TestMethod]
        public void InsideABlockThePropertiesOfTheTypeAreProposed()
        {
            XnlCompletionContext context = At("field<TextField> { place| }");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            Assert.AreEqual(nameof(TextField), context.TypeName);
            CollectionAssert.Contains(context.Items.ToArray(), "placeholder");
            CollectionAssert.Contains(context.Items.ToArray(), "password-char");
        }

        [TestMethod]
        public void AnUntypedNodeFallsBackToVisualElement()
        {
            XnlCompletionContext context = At("header { te| }");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            Assert.IsNull(context.TypeName);
            CollectionAssert.Contains(context.Items.ToArray(), "text");
            CollectionAssert.DoesNotContain(context.Items.ToArray(), "placeholder");
        }

        [TestMethod]
        public void AnUnknownTypeOffersOnlyWhatIsAlwaysValid()
        {
            XnlCompletionContext context = At("card<CardComponent> { | }");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            CollectionAssert.AreEquivalent(new[] { "class", "slot" }, context.Items.ToArray(),
                "a component's own props are not reachable without a compilation, and VisualElement's would be wrong");
        }

        [TestMethod]
        public void TheReservedPropertiesAreAlwaysThere()
        {
            string[] items = At("header { | }").Items.ToArray();

            CollectionAssert.IsSubsetOf(new[] { "class", "slot" }, items);

            CollectionAssert.DoesNotContain(items, "each",
                "each and key became regions and are diagnostics now, so proposing them would be wrong");
            CollectionAssert.DoesNotContain(items, "key");
        }

        [TestMethod]
        public void EventsAreProposedAlongsideProperties()
        {
            string[] items = At("header { cl| }").Items.ToArray();

            CollectionAssert.Contains(items, "click");
            CollectionAssert.Contains(items, "pointer-click", "the alias and the real name both work");
        }

        [TestMethod]
        public void AnEventDeclaredByASubclassIsProposed()
        {
            string[] items = At("field<TextField> { text-ch| }").Items.ToArray();

            CollectionAssert.Contains(items, "text-changed");
        }

        [TestMethod]
        public void AnEventValueProposesNothing()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { click: \"|\" }").Kind,
                "the expression is C#, and the model is not visible from here");
        }

        [TestMethod]
        public void ABooleanPropertyProposesItsTwoValues()
        {
            XnlCompletionContext context = At("header { focusable: \"|\" }");

            Assert.AreEqual(XnlCompletionKind.PropertyValue, context.Kind);
            Assert.AreEqual("focusable", context.PropertyName);
            CollectionAssert.AreEquivalent(new[] { "false", "true" }, context.Items.ToArray());
        }

        [TestMethod]
        public void AFreeFormPropertyValueProposesNothing()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { text: \"Cou|\" }").Kind);
        }

        [TestMethod]
        public void ABindingIsNotACompletionPosition()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { focusable: \"{IsEdi|}\" }").Kind);
        }

        [TestMethod]
        public void AClosedBlockLeavesThePropertyContext()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { text: \"a\" } [\r\n    item|\r\n]").Kind);
        }

        [TestMethod]
        public void ABraceInsideAValueDoesNotOpenABlock()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { text: \"{Name}\" } [\r\n    it|\r\n]").Kind);
        }

        [TestMethod]
        public void NothingIsProposedInsideAComment()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("header { // te|\r\n}").Kind);
            Assert.AreEqual(XnlCompletionKind.None, At("header { /* te| */ }").Kind);
        }

        [TestMethod]
        public void ACommentDoesNotOpenABlock()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("// header {\r\nite|").Kind);
        }

        [TestMethod]
        public void ARegionHeaderDoesNotOpenAPropertyBlock()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("root {} [\r\n\t@if (V) {\r\n\t\tit|\r\n\t@}\r\n]").Kind,
                "inside a region we are at child position, where element names are the user's own words");
        }

        [TestMethod]
        public void PropertiesAreStillProposedInsideARegion()
        {
            XnlCompletionContext context = At("root {} [\r\n\t@if (V) {\r\n\t\tel { te| }\r\n\t@}\r\n]");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            CollectionAssert.Contains(context.Items.ToArray(), "text");
        }

        [TestMethod]
        public void NothingIsProposedInsideARegionHeader()
        {
            Assert.AreEqual(XnlCompletionKind.None, At("root {} [\r\n\t@if (Vis|\r\n]").Kind,
                "the header is C#, and the model is not visible from here");
        }

        [TestMethod]
        public void AStatementDoesNotSwallowTheNextNode()
        {
            XnlCompletionContext context = At("root {} [\r\n\t@var max = 5;\r\n\tel { te| }\r\n]");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind,
                "a statement ends at its semicolon, not at the next brace");
            CollectionAssert.Contains(context.Items.ToArray(), "text");
        }

        [TestMethod]
        public void AForHeaderIsSkippedWholeDespiteItsSemicolons()
        {
            XnlCompletionContext context =
                At("root {} [\r\n\t@for (int i = 0; i < 3; i++) {\r\n\t\tel { te| }\r\n\t@}\r\n]");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            CollectionAssert.Contains(context.Items.ToArray(), "text");
        }

        [TestMethod]
        public void ABareElseClauseDoesNotOpenAPropertyBlock()
        {
            XnlCompletionContext context =
                At("root {} [\r\n\t@if (V) {\r\n\t\ta {}\r\n\t@} else {\r\n\t\tb { te| }\r\n\t@}\r\n]");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            CollectionAssert.Contains(context.Items.ToArray(), "text");
        }

        [TestMethod]
        public void AClosedRegionReturnsToChildPosition()
        {
            XnlCompletionContext context = At("root {} [\r\n\t@if (V) {\r\n\t\ta {}\r\n\t@}\r\n\tb { te| }\r\n]");

            Assert.AreEqual(XnlCompletionKind.PropertyName, context.Kind);
            CollectionAssert.Contains(context.Items.ToArray(), "text");
        }

        [TestMethod]
        public void EveryProposedPropertyNameRoundTripsToARealProperty()
        {
            foreach (string typeName in XnlCompletions.ElementTypes)
            {
                System.Type type = XnlTypes.Find(typeName);

                foreach (string name in XnlTypes.PropertiesOf(type))
                {
                    if (XnlTypes.UniversalProperties.Contains(name))
                    {
                        continue;
                    }

                    string member = XnlEvents.Resolve(name, ToPropertyName(name));

                    Assert.IsTrue(type.GetProperty(member) != null || type.GetEvent(member) != null,
                        $"{typeName}: '{name}' does not map back to '{member}'");
                }
            }
        }

        [TestMethod]
        public void OnlyWhatXnlCanConvertIsProposed()
        {
            string[] items = XnlTypes.PropertiesOf(XnlTypes.Find("VisualElement")).ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "accessible-value", "class", "click", "description", "double-click", "drag",
                    "drag-end", "drag-start",
                    "enabled", "focusable", "got-focus", "id", "key-down", "key-up", "label", "live-region", "long-press",
                    "lost-focus", "modal", "name",
                    "pointer-click", "pointer-double-click", "pointer-down", "pointer-drag", "pointer-drag-end",
                    "pointer-drag-start", "pointer-enter", "pointer-leave", "pointer-long-press", "pointer-move",
                    "pointer-up", "pointer-wheel", "role", "scroll-x", "scroll-y", "scrollable", "slot",
                    "text", "text-input",
                    "transition-ended", "type-name", "wheel"
                },
                items,
                "Classes and Styles have public setters but no XNL value can be converted to them: " + string.Join(", ", items));
        }

        [TestMethod]
        public void EveryProposedTypeIsInstantiable()
        {
            foreach (string typeName in XnlCompletions.ElementTypes)
            {
                System.Type type = XnlTypes.Find(typeName);

                Assert.IsNotNull(type, typeName);
                Assert.IsInstanceOfType(System.Activator.CreateInstance(type), typeof(VisualElement), typeName);
            }
        }

        [TestMethod]
        public void AnOutOfRangePositionIsHarmless()
        {
            Assert.AreEqual(XnlCompletionKind.None, XnlCompletions.At(null, 0).Kind);
            Assert.AreEqual(XnlCompletionKind.None, XnlCompletions.At("el {}", 99).Kind);
            Assert.AreEqual(XnlCompletionKind.None, XnlCompletions.At("el {}", -1).Kind);
        }

        private static string ToPropertyName(string xnlName)
        {
            var sb = new System.Text.StringBuilder();
            bool upperNext = true;

            foreach (char c in xnlName)
            {
                if (c == '-')
                {
                    upperNext = true;
                    continue;
                }

                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return sb.ToString();
        }
    }
}
