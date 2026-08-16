using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class TwoWayTests
    {
        private const int VIEWPORT = 200;

        private static TextField Field(TwoWayComponent component, string name)
            => (TextField)component.Initialize().FindByName(name);

        private static void Type(TextField field, string text)
            => field.RaiseTextInput(new TextInputEventArgs(text, field));

        private static IxenSurface Laid(TwoWayComponent component)
        {
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        [TestMethod]
        public void TheModelIsWrittenIntoTheFieldFirst()
        {
            var component = new TwoWayComponent { Name = "hello" };

            Assert.AreEqual("hello", Field(component, "two_way_field").Text,
                "the one-way half is unchanged");
        }

        [TestMethod]
        public void AnEditIsPushedBackIntoTheModel()
        {
            var component = new TwoWayComponent { Name = "" };
            TextField field = Field(component, "two_way_field");

            Type(field, "a");

            Assert.AreEqual("a", component.Name);
        }

        [TestMethod]
        public void ANestedPathIsAssignable()
        {
            var component = new TwoWayComponent();
            TextField field = Field(component, "two_way_nested");

            field.Text = string.Empty;
            Type(field, "z");

            Assert.AreEqual("z", component.Inner.Label);
        }

        [TestMethod]
        public void TheWriteBackAsksForAReRender()
        {
            var component = new TwoWayComponent { Name = "" };
            IxenSurface surface = Laid(component);

            Type(Field(component, "two_way_field"), "x");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("[x]", component.Initialize().FindByName("two_way_echo").Text,
                "the write-back calls SetState, so every other binding catches up");
        }

        [TestMethod]
        public void AssigningTheModelBackDoesNotLoop()
        {
            var component = new TwoWayComponent { Name = "" };
            IxenSurface surface = Laid(component);
            TextField field = Field(component, "two_way_field");

            int raised = 0;
            field.TextChanged += (sender, e) => raised++;

            Type(field, "q");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, raised,
                "Bind reassigns Text, but the setter neither raises TextChanged nor re-enters the write-back");
            Assert.AreEqual("q", component.Name);
        }

        [TestMethod]
        public void TheCaretSurvivesTheRoundTrip()
        {
            var component = new TwoWayComponent { Name = "" };
            IxenSurface surface = Laid(component);
            TextField field = Field(component, "two_way_field");

            Type(field, "a");
            Type(field, "b");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("ab", component.Name, "typing twice does not jump the caret to the start");
            Assert.AreEqual("ab", field.Text);
        }

        [TestMethod]
        public void DoubledBracketsAreALiteralPath()
        {
            var component = new TwoWayComponent();

            Assert.AreEqual("[Name]", component.Initialize().FindByName("two_way_escaped").Text,
                "the escape exists because a whole-value bracketed path is now a binding");
        }

        [TestMethod]
        public void BracketsInsideALongerValueAreJustText()
        {
            var component = new TwoWayComponent();

            Assert.AreEqual("see [1] below", component.Initialize().FindByName("two_way_partial").Text,
                "only a value that is exactly a bracketed member path is a binding");
        }

        [TestMethod]
        public void AOneWayBindingOnAFieldStaysOneWay()
        {
            var component = new TwoWayComponent { Name = "keep" };
            TextField field = Field(component, "two_way_readonly");

            Type(field, "!");

            Assert.AreEqual("keep", component.Name,
                "without the marker a field is a plain display, which is a legitimate thing to want");
        }
    }
}
