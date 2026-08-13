using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ComponentTests
    {
        private const int VIEWPORT = 200;

        private static IxenSurface Laid(VisualElement root)
        {
            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static VisualElement Host(out CounterComponent first, out CounterComponent second)
        {
            VisualElement view = new Component<HostView>().View;
            VisualElement root = view.Children[0];

            first = (CounterComponent)root.Children[0].Owner;
            second = (CounterComponent)root.Children[1].Owner;

            return view;
        }

        [TestMethod]
        public void AComponentDeclaredInXnlIsInstantiatedAndOwnsItsElement()
        {
            VisualElement view = Host(out CounterComponent first, out CounterComponent second);
            VisualElement root = view.Children[0];

            Assert.AreEqual("host_root", root.Name);
            Assert.AreEqual(2, root.Children.Count);

            Assert.IsNotNull(first, "the injected element is owned by its component");
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second, "each declaration is its own instance");

            Assert.AreSame(first.Initialize(), root.Children[0],
                "the element in the tree is the component's own view");
        }

        [TestMethod]
        public void PropertiesReachTheComponentBeforeItIsInitialized()
        {
            Host(out CounterComponent first, out CounterComponent second);

            Assert.AreEqual("A", first.CaptionAtInit, "props are set before OnInitialized runs");
            Assert.AreEqual("B", second.CaptionAtInit);
            Assert.AreEqual(2, first.Step, "a typed property is converted, not left as a string");
            Assert.AreEqual(1, second.Step, "an undeclared property keeps its default");
        }

        [TestMethod]
        public void TheViewIsBuiltBeforeOnInitialized()
        {
            Host(out CounterComponent first, out _);

            Assert.AreEqual(1, first.ChildrenAtInit, "the view exists, so FindByName works there");
            Assert.IsNotNull(first.Button);
        }

        [TestMethod]
        public void RenderRunsOnceAtInitialization()
        {
            Host(out CounterComponent first, out _);

            Assert.AreEqual(1, first.Renders);
            Assert.AreEqual("A 0", first.Button.FindByName("counter_label").Text);
        }

        [TestMethod]
        public void TheElementNameAndClassesComeFromTheDeclaration()
        {
            VisualElement view = Host(out _, out _);
            VisualElement root = view.Children[0];

            Assert.AreEqual("first", root.Children[0].Name);
            Assert.AreEqual("second", root.Children[1].Name);
            Assert.IsTrue(root.Children[1].HasClass("boxed"));
            Assert.AreEqual("CounterView", root.Children[0].TypeName,
                "the element keeps its view type, so #CounterView still targets it");
        }

        [TestMethod]
        public void SetStateReRendersOnTheNextLayout()
        {
            VisualElement view = Host(out CounterComponent first, out CounterComponent second);
            IxenSurface surface = Laid(view);

            first.Button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, first.Button));

            Assert.AreEqual(2, first.Count, "the handler ran with its own Step");
            Assert.AreEqual(1, first.Renders, "but Render waits for the layout pass");
            Assert.IsTrue(surface.IsDirty);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(2, first.Renders);
            Assert.AreEqual("A 2", first.Button.FindByName("counter_label").Text);
            Assert.AreEqual(1, second.Renders, "the sibling component was not re-rendered");
        }

        [TestMethod]
        public void ARenderIsNotRepeatedWhileTheStateIsUnchanged()
        {
            VisualElement view = Host(out CounterComponent first, out _);
            IxenSurface surface = Laid(view);

            first.Button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, first.Button));
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            view.InvalidateLayout();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(2, first.Renders, "a layout pass with no state change renders nothing");
        }

        [TestMethod]
        public void SetStateSurvivesSeveralChangesBeforeALayout()
        {
            VisualElement view = Host(out CounterComponent first, out _);
            IxenSurface surface = Laid(view);

            first.Button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, first.Button));
            first.Button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, first.Button));
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(4, first.Count);
            Assert.AreEqual(2, first.Renders, "the two changes collapse into one render");
            Assert.AreEqual("A 4", first.Button.FindByName("counter_label").Text);
        }

        [TestMethod]
        public void InitializeIsIdempotent()
        {
            var component = new CounterComponent { Caption = "C" };

            VisualElement first = component.Initialize();
            VisualElement second = component.Initialize();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, component.Renders, "initializing twice does not re-run the lifecycle");
        }

        [TestMethod]
        public void AComponentUsedAsARootIsInitialized()
        {
            var component = new CounterComponent { Caption = "Root" };
            var surface = new IxenSurface(component);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, component.Renders);
            Assert.AreEqual("Root 0", component.Button.FindByName("counter_label").Text);
        }

        [TestMethod]
        public void AStateChangeOnANestedComponentIsFoundFromTheRoot()
        {
            VisualElement view = Host(out _, out CounterComponent second);
            IxenSurface surface = Laid(view);

            second.Button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, second.Button));
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("B 1", second.Button.FindByName("counter_label").Text);
        }
    }
}
