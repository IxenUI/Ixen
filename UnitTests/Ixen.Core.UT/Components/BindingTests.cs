using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class BindingTests
    {
        private const int VIEWPORT = 200;

        private static string TextOf(BoundComponent component, string name)
            => component.Initialize().FindByName(name).Text;

        private static IxenSurface Laid(BoundComponent component)
        {
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        [TestMethod]
        public void ABoundViewImplementsTheBindingInterface()
        {
            var component = new BoundComponent();

            Assert.IsInstanceOfType(component.Initialize(), typeof(IBoundView),
                "the generator only adds the interface when the view actually binds something");
        }

        [TestMethod]
        public void ASingleMemberIsAssignedDirectly()
        {
            var component = new BoundComponent { Caption = "title" };
            component.Initialize();

            Assert.AreEqual("title", TextOf(component, "bound_plain"));
        }

        [TestMethod]
        public void TextAndBindingsMixIntoAnInterpolatedString()
        {
            var component = new BoundComponent { Count = 1, Total = 4 };
            component.Initialize();

            Assert.AreEqual("1 of 4 done", TextOf(component, "bound_mixed"));
        }

        [TestMethod]
        public void ANonStringPropertyIsBoundWithoutConversion()
        {
            var component = new BoundComponent { IsEditable = true };
            component.Initialize();

            Assert.IsTrue(component.Initialize().FindByName("bound_typed").Focusable);
        }

        [TestMethod]
        public void AnArbitraryExpressionIsEmittedVerbatim()
        {
            var component = new BoundComponent { Count = 3 };
            component.Initialize();

            Assert.AreEqual("6", TextOf(component, "bound_expression"));
        }

        [TestMethod]
        public void AMethodCallOnTheModelWorksAndItsArgumentsAreQualifiedToo()
        {
            var component = new BoundComponent { Count = 7 };
            component.Initialize();

            Assert.AreEqual("n=7", TextOf(component, "bound_call"),
                "Describe(Count) became model.Describe(model.Count)");
        }

        [TestMethod]
        public void ANestedMemberIsNotDoublePrefixed()
        {
            var component = new BoundComponent();
            component.Inner.Name = "deep";
            component.Initialize();

            Assert.AreEqual("deep", TextOf(component, "bound_nested"));
        }

        [TestMethod]
        public void DoubledBracesAreALiteralBrace()
        {
            var component = new BoundComponent();
            component.Initialize();

            Assert.AreEqual("{Caption}", TextOf(component, "bound_literal"),
                "the escaped value is not a binding at all");
        }

        [TestMethod]
        public void BindingsAreAppliedAtInitialization()
        {
            var component = new BoundComponent { Caption = "at init" };

            Assert.AreEqual("at init", TextOf(component, "bound_plain"),
                "no layout pass is needed for the first binding");
        }

        [TestMethod]
        public void BindingsAreReappliedAfterAStateChange()
        {
            var component = new BoundComponent { Count = 0, Total = 2 };
            IxenSurface surface = Laid(component);

            component.Advance();

            Assert.AreEqual("0 of 2 done", TextOf(component, "bound_mixed"),
                "the binding waits for the layout pass, like Render");

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("1 of 2 done", TextOf(component, "bound_mixed"));
        }

        [TestMethod]
        public void ABindingFollowsAPropertyChangedWithoutSetState()
        {
            var component = new BoundComponent { Caption = "first" };
            IxenSurface surface = Laid(component);

            component.Caption = "second";
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("first", TextOf(component, "bound_plain"),
                "nothing observes a plain property, so SetState is what triggers a re-bind");

            component.Advance();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("second", TextOf(component, "bound_plain"));
        }

        [TestMethod]
        public void AViewWithNoBindingDoesNotImplementTheInterface()
        {
            var component = new CounterComponent();

            Assert.IsNotInstanceOfType(component.Initialize(), typeof(IBoundView),
                "an unbound view pays nothing for the feature");
        }

        [TestMethod]
        public void OnlyBoundElementsBecomeFields()
        {
            System.Reflection.FieldInfo[] fields = typeof(Ixen.Views.BoundView)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Where(f => f.Name.StartsWith("el"))
                .ToArray();

            Assert.AreEqual(6, fields.Length,
                "six of the seven children are bound; the escaped one stays a local");
            Assert.IsFalse(fields.Any(f => f.Name.Contains("literal")));
        }
    }
}
