using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class FragmentTests
    {
        private const int VIEWPORT = 300;

        [TestMethod]
        public void AFragmentIsReadApartFromThePathAndTheQuery()
        {
            var nav = new Navigator("/guide/install?lang=fr#step-two");

            Assert.AreEqual("/guide/install", nav.Path);
            Assert.AreEqual("lang=fr", nav.QueryString);
            Assert.AreEqual("fr", nav.Query("lang"), "the fragment does not leak into the last parameter");
            Assert.AreEqual("step-two", nav.Fragment);
            Assert.IsTrue(nav.Is("/guide/{page}"));
            Assert.AreEqual("install", nav.Get("page"));
        }

        [TestMethod]
        public void AFragmentAloneOrAbsentIsUnderstood()
        {
            Assert.AreEqual("top", new Navigator("/guide/#top").Fragment);
            Assert.AreEqual("/guide", new Navigator("/guide/#top").Path, "the trailing slash is trimmed before the mark");
            Assert.AreEqual("/guide#top", new Navigator("guide/#top").Location);
            Assert.IsNull(new Navigator("/guide").Fragment);
            Assert.IsNull(new Navigator("/guide#").Fragment, "an empty fragment is no fragment");
            Assert.AreEqual("a b", new Navigator("/guide#a%20b").Fragment);
        }

        [TestMethod]
        public void AFragmentIsPartOfTheHistory()
        {
            var nav = new Navigator("/guide");

            nav.Navigate("/guide#install");
            nav.Navigate("/guide#usage");
            nav.Back();

            Assert.AreEqual("install", nav.Fragment);
        }

        private class GuideComponent : Component<VisualElement>
        {
            internal readonly Navigator Nav = new Navigator("/");
            internal VisualElement Page;

            protected override void OnInitialized()
            {
                Follow(Nav);
            }

            internal void Go(string location)
            {
                Nav.Navigate(location);
                ScrollTo(Nav.Fragment);
            }

            internal bool AskFor(string name) => ScrollTo(name);

            protected override void Render()
            {
                if (Page == null)
                {
                    Page = new VisualElement { Name = "page", Scrollable = true };
                    Page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
                    Page.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
                    Page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
                    View.AddChild(Page);
                }

                if (Nav.Path == "/guide" && Page.ChildElements.Count == 0)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var section = new VisualElement { Name = "section" + i };

                        section.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
                        Page.AddChild(section);
                    }
                }
            }
        }

        private IxenSurface _surface;
        private GuideComponent _guide;

        [TestInitialize]
        public void Setup()
        {
            _guide = new GuideComponent();

            var root = new VisualElement { Name = "root" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(_guide.Initialize());

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void NavigatingToAFragmentScrollsItsElementToTheTopInTheSameFrame()
        {
            _guide.Go("/guide#section4");
            Layout();

            Assert.AreEqual(400f, _guide.Page.ScrollY,
                "section4 did not exist when the scroll was asked for: the page was built by the next render");
            Assert.AreEqual(_guide.Page.Y, _guide.Page.FindByName("section4").Y, 0.5f,
                "and it is in place in that same frame, not one frame later");
        }

        [TestMethod]
        public void ScrollingStopsWhereThePageEnds()
        {
            _guide.Go("/guide#section9");
            Layout();

            Assert.AreEqual(800f, _guide.Page.ScrollY, "ten sections of 100 in a page of 200 scroll 800 at most");
        }

        [TestMethod]
        public void AnElementCanBeBroughtIntoViewTheShortWay()
        {
            _guide.Go("/guide");
            Layout();

            VisualElement six = _guide.Page.FindByName("section6");

            Assert.IsTrue(six.ScrollIntoView());
            Layout();

            Assert.AreEqual(500f, _guide.Page.ScrollY, "just enough for its bottom edge to show");

            Assert.IsTrue(_guide.Page.FindByName("section5").ScrollIntoView());
            Layout();

            Assert.AreEqual(500f, _guide.Page.ScrollY, "already in view, so nothing moves");
        }

        [TestMethod]
        public void AnUnknownNameOrAHiddenElementMovesNothing()
        {
            _guide.Go("/guide#nowhere");
            Layout();

            Assert.AreEqual(0f, _guide.Page.ScrollY);

            VisualElement three = _guide.Page.FindByName("section3");

            three.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            three.Invalidate();
            Layout();

            Assert.IsTrue(_guide.AskFor("section3"));
            Layout();

            Assert.AreEqual(0f, _guide.Page.ScrollY, "something nobody can see is not scrolled to");
        }

        [TestMethod]
        public void AnElementOutsideAHostedTreeCannotAsk()
        {
            Assert.IsFalse(new VisualElement().ScrollIntoView());
        }

        [TestMethod]
        public void EveryScrollingAncestorMoves()
        {
            var outer = new VisualElement { Name = "outer", Scrollable = true };
            var spacer = new VisualElement { Name = "spacer" };
            var inner = new VisualElement { Name = "inner", Scrollable = true };
            var target = new VisualElement { Name = "target" };

            outer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            outer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            outer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            spacer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 500 };
            inner.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            target.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };

            for (int i = 0; i < 6; i++)
            {
                var filler = new VisualElement();

                filler.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
                inner.AddChild(filler);
            }

            inner.AddChild(target);
            outer.AddChild(spacer);
            outer.AddChild(inner);

            var root = new VisualElement { Name = "root" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(outer);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            Layout();

            Assert.IsTrue(target.ScrollIntoView(true));
            Layout();

            Assert.AreEqual(250f, inner.ScrollY, "the list scrolls as far as it goes, which leaves the target 50 down");
            Assert.AreEqual(400f, outer.ScrollY, "and the page scrolls the list up as far as it can");
            Assert.AreEqual(inner.Y + 50, target.Y, 0.5f, "the page used where the list had left it, not where it was");
        }
    }
}
