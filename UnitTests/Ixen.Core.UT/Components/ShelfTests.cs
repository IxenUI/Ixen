using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ShelfTests
    {
        private const int VIEWPORT = 300;

        private ShelfComponent _shelf;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup() => Open(new ShelfComponent());

        private void Open(ShelfComponent shelf)
        {
            _shelf = shelf;
            _surface = new IxenSurface(_shelf) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Find(string name) => _shelf.View.FindByName(name);

        private ShelfCardComponent Card(string name) => (ShelfCardComponent)Find(name).Owner;

        private string Title(string card) => Find(card).FindByName("card_title").Text;

        [TestMethod]
        public void AChildComponentHasItsBoundPropsAtItsFirstRender()
        {
            Assert.AreEqual("BLUE", Title("featured"),
                "the child's view reads Album.ToUpperInvariant(): rendered before the parent bound it, "
                + "it threw a NullReferenceException while the parent was being built");
            Assert.AreEqual("Blue", Card("featured").SeenWhenInitialized,
                "props are set before OnInitialized, bound ones included");
        }

        [TestMethod]
        public void AChangeOfABoundPropRerendersTheChild()
        {
            int renders = Card("featured").Renders;

            _shelf.Update(() => _shelf.Featured = "Navy");
            Layout();

            Assert.AreEqual("NAVY", Title("featured"), "the child's view used to keep the first value for good");
            Assert.IsTrue(Card("featured").Renders > renders);
        }

        [TestMethod]
        public void AChildComponentInsideARegionGetsItsPropsToo()
        {
            _shelf.Update(() => _shelf.ShowPicked = true);
            Layout();

            Assert.AreEqual("GREEN", Title("picked"),
                "inside a region the generated code set the props on the view, which did not compile");
        }

        [TestMethod]
        public void ABoundClassIsAClassNotItsOwnText()
        {
            VisualElement swatch = Find("swatch");

            CollectionAssert.Contains(swatch.Classes, "swatch");
            CollectionAssert.Contains(swatch.Classes, "warm");
            CollectionAssert.DoesNotContain(swatch.Classes, "{Tint}", "the binding was added as a class name");

            swatch.Classes.Add("pinned");

            _shelf.Update(() => _shelf.Tint = "cool");
            Layout();

            CollectionAssert.Contains(swatch.Classes, "cool");
            CollectionAssert.DoesNotContain(swatch.Classes, "warm", "the class the binding gave before is taken back");
            CollectionAssert.Contains(swatch.Classes, "swatch");
            CollectionAssert.Contains(swatch.Classes, "pinned", "and only that one: a class added by code stays");
        }

        [TestMethod]
        public void ARepeatedRowBindsItsClassToo()
        {
            VisualElement tiles = Find("tiles");

            CollectionAssert.Contains(tiles.ChildElements[0].Classes, "red");
            CollectionAssert.Contains(tiles.ChildElements[1].Classes, "gold");
            Assert.AreEqual(1, tiles.ChildElements[0].Classes.Count,
                "the row's constructor must not also add the binding's text as a class");
        }

        [TestMethod]
        public void ASubclassOfAComponentStillBindsItsView()
        {
            Open(new FeaturedShelfComponent());

            Assert.AreEqual("SILVER", Title("featured"),
                "two classes deriving from Component<ShelfView> - the base and its subclass - were "
                + "'several components', and the view lost every binding");
        }
    }
}
