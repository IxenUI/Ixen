using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class DefaultsDroppedTests
    {
        private static ClassesSet Compile(string source)
        {
            var xns = new XnsSource(source);
            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors, source);

            return set;
        }

        private static StyleClass ClassNamed(ClassesSet set, string name)
        {
            foreach (StyleClass styleClass in set.Classes)
            {
                if (styleClass.Name == name)
                {
                    return styleClass;
                }
            }

            Assert.Fail("no rule named " + name);

            return null;
        }

        [TestMethod]
        public void APlainRuleCanBeADefault()
        {
            Assert.IsTrue(StyleRegistry.CanBeDefault(
                ClassNamed(Compile("#Button { background: #FF0000 }"), "Button")));
        }

        [TestMethod]
        public void AStateVariantCanTooBecauseTheStateIsPartOfTheName()
        {
            Assert.IsTrue(StyleRegistry.CanBeDefault(
                ClassNamed(Compile("#Button:hover { background: #FF0000 }"), "Button:hover")));
        }

        [TestMethod]
        public void ANestedRuleCannot()
        {
            Assert.IsFalse(StyleRegistry.CanBeDefault(
                ClassNamed(Compile("#Slider { #SliderFill { background: #FF0000 } }"), "SliderFill")));
        }

        [TestMethod]
        public void NorOneInsideAMediaBlock()
        {
            Assert.IsFalse(StyleRegistry.CanBeDefault(
                ClassNamed(Compile("@media (max-width: 400px) { #Button { height: 20px } }"), "Button")));
        }

        [TestMethod]
        public void ThePredicateIsWhatAddDefaultsActuallyDoes()
        {
            ClassesSet set = Compile(@"#Button { background: #FF0000 }
#Slider { #SliderFill { background: #00FF00 } }");

            var registry = new StyleRegistry();

            registry.AddDefaults(set);

            Assert.IsNotNull(registry.GetDefault(StyleClassTarget.ElementType, "Button"));
            Assert.IsNull(registry.GetDefault(StyleClassTarget.ElementType, "SliderFill"),
                "the nested rule really is dropped, and CanBeDefault is the same test - the "
                + "build-time warning and the runtime behaviour cannot drift apart because they "
                + "call one method");
        }

        [TestMethod]
        public void ADroppedRuleCarriesTheSpanTheWarningPointsAt()
        {
            const string source = "#Slider {\r\n    #SliderFill { background: #FF0000 }\r\n}";

            StyleClass dropped = ClassNamed(Compile(source), "SliderFill");

            Assert.AreEqual(source.IndexOf("#SliderFill"), dropped.SourceIndex,
                "without the index the warning would land on line 1 of the file instead of on "
                + "the rule that does nothing");
            Assert.AreEqual("#SliderFill".Length, dropped.SourceLength);
        }

        [TestMethod]
        public void AnApplicationSheetIsNotConcerned()
        {
            ClassesSet set = Compile("#Slider { #SliderFill { background: #FF0000 } }");

            var registry = new StyleRegistry();

            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var slider = new VisualElement { TypeName = "Slider" };
            var fill = new VisualElement { TypeName = "SliderFill" };

            slider.AddChild(fill);
            root.AddChild(slider);

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(200, 200);

            Assert.AreEqual("#FF0000", fill.StylesHandlers.Background.Descriptor.Color,
                "a scoped rule is perfectly good in an application stylesheet - it is only the "
                + "defaults layer that cannot carry one, which is why the warning is gated on "
                + "the assembly attribute");
        }
    }
}
