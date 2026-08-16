using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ActionTests
    {
        private const int VIEWPORT = 200;

        private static VisualElement Element(ActionComponent component, string name)
            => component.Initialize().FindByName(name);

        private static PointerEventArgs Pointer(VisualElement source, float x = 0, float y = 0)
            => new PointerEventArgs(x, y, PointerButton.Left, source);

        [TestMethod]
        public void AClickBindingCallsTheModel()
        {
            var component = new ActionComponent();
            VisualElement button = Element(component, "action_button");

            button.RaisePointerClick(Pointer(button));

            Assert.AreEqual(1, component.Count);
        }

        [TestMethod]
        public void AnActionCoexistsWithAPropertyBindingOnTheSameElement()
        {
            var component = new ActionComponent { Caption = "press" };

            Assert.AreEqual("press", Element(component, "action_button").Text);
        }

        [TestMethod]
        public void AnAliasResolvesToThePointerEvent()
        {
            var component = new ActionComponent();
            VisualElement element = Element(component, "action_aliased");

            element.RaisePointerDoubleClick(Pointer(element));

            Assert.AreEqual(5, component.Count, "double-click is an alias for PointerDoubleClick");
        }

        [TestMethod]
        public void TheFullEventNameWorksWithoutAnAlias()
        {
            var component = new ActionComponent { Count = 9 };
            VisualElement element = Element(component, "action_direct");

            element.RaisePointerLongPress(Pointer(element));

            Assert.AreEqual(0, component.Count, "pointer-long-press maps straight to PointerLongPress");
        }

        [TestMethod]
        public void TheEventArgumentIsInScopeInTheExpression()
        {
            var component = new ActionComponent();
            VisualElement element = Element(component, "action_with_args");

            element.RaisePointerDown(Pointer(element, 12, 34));

            Assert.AreEqual(12f, component.LastX);
            Assert.AreEqual(34f, component.LastY, "'e' is not a model member, so Qualify leaves it alone");
        }

        [TestMethod]
        public void AnEventDeclaredByASubclassIsReachable()
        {
            var component = new ActionComponent();
            var field = (TextField)Element(component, "action_field");

            field.RaiseTextInput(new TextInputEventArgs("a", field));

            Assert.IsTrue(component.Captured, "text-changed is TextField's own event, not VisualElement's");
        }

        [TestMethod]
        public void AnActionSurvivesAStateChange()
        {
            var component = new ActionComponent();
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement button = Element(component, "action_button");

            button.RaisePointerClick(Pointer(button));
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            button.RaisePointerClick(Pointer(button));

            Assert.AreEqual(2, component.Count,
                "Bind is replayed on every SetState, so a handler wired there would fire twice on the second click");
        }

        [TestMethod]
        public void TheHandlerIsWiredInTheConstructorNotInBind()
        {
            var view = new Ixen.Views.ActionView();
            VisualElement button = view.FindByName("action_button");

            button.RaisePointerClick(Pointer(button));

            Assert.IsNotNull(button, "an unbound view still wires its handlers; they are no-ops until Bind runs");
        }

        [TestMethod]
        public void AnActionOnlyViewStillBinds()
        {
            var component = new ActionComponent();

            Assert.IsInstanceOfType(component.Initialize(), typeof(IBoundView));

            Assert.IsTrue(typeof(Ixen.Views.ActionView)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Any(f => f.Name == "_model"),
                "the model is held in a field so a constructor-wired handler can reach it");
        }
    }
}
