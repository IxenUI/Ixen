using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ScrollStateTests
    {
        private const int VIEWPORT = 300;
        private const float ROW = 20;

        private static IxenSurface Build(int rows, bool persist, out VisualElement library)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var page = new VisualElement { Name = "page" };
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            library = new VisualElement { Name = "library", Scrollable = true, PersistScroll = persist };
            library.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            library.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            library.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            Fill(library, rows);

            page.AddChild(library);
            root.AddChild(page);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static void Fill(VisualElement library, int rows)
        {
            for (int i = 0; i < rows; i++)
            {
                var row = new VisualElement();
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = ROW };
                library.AddChild(row);
            }
        }

        private static string Saved(float scrollY, bool persist = true)
        {
            IxenSurface first = Build(50, persist, out VisualElement library);

            library.ScrollY = scrollY;
            first.ComputeLayout(VIEWPORT, VIEWPORT);

            return first.SaveState();
        }

        [TestMethod]
        public void AScrollKeptByOneSurfaceIsWhereTheNextOneOpens()
        {
            string saved = Saved(300);

            IxenSurface next = Build(50, true, out VisualElement library);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(300, library.ScrollY, 0.01f);
        }

        [TestMethod]
        public void NothingIsKeptUnlessTheElementAsksForIt()
        {
            string saved = Saved(300, false);

            Assert.IsFalse(saved.Contains("library"), "an element that did not ask writes nothing");

            IxenSurface next = Build(50, true, out VisualElement library);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, library.ScrollY);
        }

        [TestMethod]
        public void ContentThatArrivesLaterIsScrolledToOnceItIsThere()
        {
            string saved = Saved(300);

            IxenSurface next = Build(3, true, out VisualElement library);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, library.ScrollY, "three rows do not scroll");

            Fill(library, 20);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(360, library.MaxScrollY, 0.01f);
            Assert.AreEqual(300, library.ScrollY, 0.01f,
                "the measure clamped the offset to what was there; the wanted value waited for the rest");
        }

        [TestMethod]
        public void TheUserScrollingFirstWins()
        {
            string saved = Saved(300);

            IxenSurface next = Build(10, true, out VisualElement library);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(100, library.ScrollY, 0.01f, "as far as it goes for now");

            library.ScrollBy(0, -40);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Fill(library, 40);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(60, library.ScrollY, 0.01f, "a scroll by the user ends the wait");
        }

        [TestMethod]
        public void SavingWhileStillWaitingKeepsWhatWasWanted()
        {
            string saved = Saved(300);

            IxenSurface waiting = Build(3, true, out _);

            waiting.RestoreState(saved);
            waiting.ComputeLayout(VIEWPORT, VIEWPORT);

            string again = waiting.SaveState();

            IxenSurface next = Build(50, true, out VisualElement library);

            next.RestoreState(again);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(300, library.ScrollY, 0.01f,
                "an app closed before its library loaded would otherwise save 0 over the reader's place");
        }
    }
}
