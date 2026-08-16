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
            CollectionAssert.AreEquivalent(new[] { "class", "each", "key" }, context.Items.ToArray(),
                "a component's own props are not reachable without a compilation, and VisualElement's would be wrong");
        }

        [TestMethod]
        public void TheThreeUniversalPropertiesAreAlwaysThere()
        {
            string[] items = At("header { | }").Items.ToArray();

            CollectionAssert.IsSubsetOf(new[] { "class", "each", "key" }, items);
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

                    string propertyName = ToPropertyName(name);

                    Assert.IsNotNull(type.GetProperty(propertyName),
                        $"{typeName}: '{name}' does not map back to '{propertyName}'");
                }
            }
        }

        [TestMethod]
        public void OnlyWhatXnlCanConvertIsProposed()
        {
            string[] items = XnlTypes.PropertiesOf(XnlTypes.Find("VisualElement")).ToArray();

            CollectionAssert.AreEqual(
                new[] { "class", "each", "focusable", "id", "key", "name", "scroll-x", "scroll-y", "scrollable", "text", "type-name" },
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
