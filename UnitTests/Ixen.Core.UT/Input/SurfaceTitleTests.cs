using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class SurfaceTitleTests
    {
        [TestMethod]
        public void ATitleStartsFromTheOptionsAndAChangeIsAnnounced()
        {
            var surface = new IxenSurface(new VisualElement(), new IxenSurfaceInitOptions { Title = "Player" });
            int changes = 0;

            surface.TitleChanged += () => changes++;

            Assert.AreEqual("Player", surface.Title);

            surface.Title = "Player";
            Assert.AreEqual(0, changes, "the same title again is no change, so a SetState replay costs the window nothing");

            ((IElementHost)surface).Title = "Song 3 - Player";

            Assert.AreEqual(1, changes,
                "a component reaches it through its host, and the window follows - before, the "
                + "title was read once to create the window and every later one was ignored");
            Assert.AreEqual("Song 3 - Player", surface.Title);
        }
    }
}
