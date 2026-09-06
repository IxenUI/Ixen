using Ixen.Core.Components;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class StateTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private NavigationComponent Add(string name, VisualElement parent = null)
        {
            var component = new NavigationComponent();
            VisualElement view = component.Initialize();

            view.Name = name;
            (parent ?? _root).AddChild(view);

            return component;
        }

        private static IxenSurface Fresh(out VisualElement root)
        {
            root = new VisualElement();

            return new IxenSurface(root) { Styles = new StyleRegistry() };
        }

        [TestMethod]
        public void WhatOneSurfaceSavesAnotherRestores()
        {
            NavigationComponent first = Add("shell");
            Layout();

            first.Navigation.Navigate("/poems/12");
            first.Visits = 7;

            string saved = _surface.SaveState();

            IxenSurface next = Fresh(out VisualElement root);
            var second = new NavigationComponent();
            VisualElement view = second.Initialize();

            view.Name = "shell";
            root.AddChild(view);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("/poems/12", second.Navigation.Path);
            Assert.AreEqual(7, second.Visits);
        }

        [TestMethod]
        public void TheRestoredValueIsOnScreenOnTheVeryFirstFrame()
        {
            NavigationComponent first = Add("shell");
            Layout();
            first.Navigation.Navigate("/poems");

            string saved = _surface.SaveState();

            IxenSurface next = Fresh(out VisualElement root);
            var second = new NavigationComponent();
            VisualElement view = second.Initialize();

            view.Name = "shell";
            root.AddChild(view);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("/poems", view.FindByName("navigation_label")?.Text);
        }

        [TestMethod]
        public void AComponentWithNothingToSayContributesNothing()
        {
            NavigationComponent component = Add("shell");

            component.Saves = false;
            Layout();

            string saved = _surface.SaveState();

            IxenSurface next = Fresh(out VisualElement root);

            next.RestoreState(saved);

            Assert.AreEqual(0, next.PendingStateCount);
        }

        [TestMethod]
        public void RestoringAPlainFieldStillPutsItOnTheNextFrame()
        {
            NavigationComponent component = Add("shell");

            Layout();

            _surface.RestoreState("ixen-state\t1\nshell\tvisits\t7\n");

            int before = component.Renders;

            Layout();

            Assert.AreEqual(7, component.Visits);
            Assert.AreEqual(before + 1, component.Renders,
                "the restored value would not reach the screen until something else moved");
        }

        [TestMethod]
        public void AComponentWithNoEntryIsNotRestoredAtAll()
        {
            NavigationComponent component = Add("elsewhere");

            _surface.RestoreState("ixen-state\t1\nshell\tpath\t/poems\n");
            Layout();

            Assert.AreEqual(0, component.Restores);
            Assert.AreEqual(Navigator.ROOT, component.Navigation.Path);
        }

        [TestMethod]
        public void ARestoreHappensOnceAndOnlyOnce()
        {
            NavigationComponent first = Add("shell");

            _surface.RestoreState("ixen-state\t1\nshell\tpath\t/poems\n");

            Assert.AreEqual(1, _surface.PendingStateCount);

            Layout();

            Assert.AreEqual(1, first.Restores);
            Assert.AreEqual(0, _surface.PendingStateCount);

            NavigationComponent second = Add("shell");

            Layout();

            Assert.AreEqual(0, second.Restores, "a second component took the same state");
            Assert.AreEqual(1, first.Restores);
        }

        [TestMethod]
        public void AComponentThatArrivesLaterIsStillRestored()
        {
            _surface.RestoreState("ixen-state\t1\nlate\tpath\t/poems\n");
            Layout();

            Assert.AreEqual(1, _surface.PendingStateCount, "the state was dropped before it was needed");

            NavigationComponent component = Add("late");

            Layout();

            Assert.AreEqual("/poems", component.Navigation.Path);
        }

        [TestMethod]
        public void ThePathIsEveryNamedElementOnTheWayDown()
        {
            var page = new VisualElement { Name = "page" };
            var band = new VisualElement();

            _root.AddChild(page);
            page.AddChild(band);

            NavigationComponent component = Add("shell", band);

            Layout();
            component.Navigation.Navigate("/poems");

            string saved = _surface.SaveState();

            StringAssert.Contains(saved, "page/shell\tpath\t/poems");
        }

        [TestMethod]
        public void AnUnnamedComponentUnderAnUnnamedRootIsTheEmptyPath()
        {
            NavigationComponent component = Add(null);

            Layout();
            component.Navigation.Navigate("/poems");

            string saved = _surface.SaveState();

            StringAssert.Contains(saved, "\n\tpath\t/poems");

            IxenSurface next = Fresh(out VisualElement root);
            var second = new NavigationComponent();

            root.AddChild(second.Initialize());
            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("/poems", second.Navigation.Path);
        }

        [TestMethod]
        public void TwoComponentsAtTheSamePathAreRefusedRatherThanConfused()
        {
            Add("shell");
            Add("shell");

            Layout();

            InvalidOperationException error
                = Assert.ThrowsExactly<InvalidOperationException>(() => _surface.SaveState());

            StringAssert.Contains(error.Message, "shell");
        }

        [TestMethod]
        public void TwoSilentComponentsAtOnePathAreNotAQuarrel()
        {
            NavigationComponent first = Add("shell");
            NavigationComponent second = Add("shell");

            first.Saves = false;
            second.Saves = false;

            Layout();

            Assert.AreEqual(StateFormat(), _surface.SaveState());
        }

        private static string StateFormat() => "ixen-state\t1\n";

        [TestMethod]
        public void TwoComponentsWithTheirOwnNamesKeepTheirOwnState()
        {
            NavigationComponent left = Add("left");
            NavigationComponent right = Add("right");

            Layout();

            left.Navigation.Navigate("/one");
            right.Navigation.Navigate("/two");

            string saved = _surface.SaveState();

            IxenSurface next = Fresh(out VisualElement root);

            var a = new NavigationComponent();
            var b = new NavigationComponent();
            VisualElement viewA = a.Initialize();
            VisualElement viewB = b.Initialize();

            viewA.Name = "left";
            viewB.Name = "right";
            root.AddChild(viewA);
            root.AddChild(viewB);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("/one", a.Navigation.Path);
            Assert.AreEqual("/two", b.Navigation.Path);
        }

        [TestMethod]
        public void SavingWithNoTreeAtAllIsStillReadable()
        {
            var bare = new IxenSurface();

            string saved = bare.SaveState();

            IxenSurface next = Fresh(out VisualElement root);

            next.RestoreState(saved);

            Assert.AreEqual(0, next.PendingStateCount);
        }

        [TestMethod]
        public void AValueMayHoldAnythingAtAll()
        {
            NavigationComponent component = Add("shell");

            Layout();
            component.Navigation.Navigate("/a\tb\nc%d\re=f/g and 100%09 and %25 and %0A");

            string saved = _surface.SaveState();

            IxenSurface next = Fresh(out VisualElement root);
            var second = new NavigationComponent();
            VisualElement view = second.Initialize();

            view.Name = "shell";
            root.AddChild(view);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("/a\tb\nc%d\re=f/g and 100%09 and %25 and %0A", second.Navigation.Path);
        }

        [TestMethod]
        public void StateFromAnotherFormatIsIgnoredRatherThanRefused()
        {
            _surface.RestoreState("ixen-state\t99\nshell\tpath\t/poems\n");

            Assert.AreEqual(0, _surface.PendingStateCount);
        }

        [TestMethod]
        public void RubbishIsIgnoredRatherThanRefused()
        {
            _surface.RestoreState("who knows what this is");
            Assert.AreEqual(0, _surface.PendingStateCount);

            _surface.RestoreState(null);
            Assert.AreEqual(0, _surface.PendingStateCount);

            _surface.RestoreState(string.Empty);
            Assert.AreEqual(0, _surface.PendingStateCount);

            _surface.RestoreState("ixen-state\t1\nnot enough fields\n");
            Assert.AreEqual(0, _surface.PendingStateCount);
        }
    }

    [TestClass]
    public class ComponentStateTests
    {
        private ComponentState _state;

        [TestInitialize]
        public void Setup() => _state = new ComponentState();

        [TestMethod]
        public void EveryTypeComesBackAsItWentIn()
        {
            _state.Set("text", "Ada");
            _state.Set("count", 42);
            _state.Set("big", 9_000_000_000L);
            _state.Set("ratio", 1.5f);
            _state.Set("yes", true);
            _state.Set("no", false);

            Assert.AreEqual("Ada", _state.Get("text"));
            Assert.AreEqual(42, _state.Get("count", 0));
            Assert.AreEqual(9_000_000_000L, _state.Get("big", 0L));
            Assert.AreEqual(1.5f, _state.Get("ratio", 0f));
            Assert.IsTrue(_state.Get("yes", false));
            Assert.IsFalse(_state.Get("no", true));
        }

        [TestMethod]
        public void AKeyThatWasNeverWrittenGivesTheFallback()
        {
            Assert.IsNull(_state.Get("nothing"));
            Assert.AreEqual("else", _state.Get("nothing", "else"));
            Assert.AreEqual(3, _state.Get("nothing", 3));
            Assert.AreEqual(3L, _state.Get("nothing", 3L));
            Assert.AreEqual(2.5f, _state.Get("nothing", 2.5f));
            Assert.IsTrue(_state.Get("nothing", true));
        }

        [TestMethod]
        public void AValueOfTheWrongShapeGivesTheFallbackToo()
        {
            _state.Set("count", "not a number");

            Assert.AreEqual(9, _state.Get("count", 9));
            Assert.AreEqual(9L, _state.Get("count", 9L));
            Assert.AreEqual(9f, _state.Get("count", 9f));
            Assert.IsTrue(_state.Get("count", true));
        }

        [TestMethod]
        public void WritingNullForgetsTheKey()
        {
            _state.Set("text", "Ada");
            Assert.IsTrue(_state.Has("text"));

            _state.Set("text", (string)null);

            Assert.IsFalse(_state.Has("text"));
            Assert.AreEqual(0, _state.Count);
        }

        [TestMethod]
        public void ANullKeyIsIgnoredRatherThanThrowing()
        {
            _state.Set(null, "Ada");

            Assert.AreEqual(0, _state.Count);
            Assert.IsFalse(_state.Has(null));
            Assert.IsNull(_state.Get(null));
        }

        [TestMethod]
        public void ANumberDoesNotDependOnTheMachinesCulture()
        {
            _state.Set("ratio", 1.5f);

            Assert.AreEqual("1.5", _state.Get("ratio"));
        }
    }
}
