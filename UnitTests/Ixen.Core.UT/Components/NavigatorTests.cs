using Ixen.Core.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class NavigatorTests
    {
        private Navigator _navigator;
        private int _changes;

        [TestInitialize]
        public void Setup()
        {
            _navigator = new Navigator();
            _navigator.Changed += (sender, e) => _changes++;
        }

        [TestMethod]
        public void AFreshNavigatorSitsAtTheRoot()
        {
            Assert.AreEqual(Navigator.ROOT, _navigator.Path);
            Assert.AreEqual(1, _navigator.Depth);
            Assert.IsFalse(_navigator.CanGoBack);
            Assert.IsNull(_navigator.Previous);
        }

        [TestMethod]
        public void AStartingPathIsNormalized()
        {
            Assert.AreEqual("/poems", new Navigator("poems").Path);
            Assert.AreEqual("/poems", new Navigator("/poems/").Path);
            Assert.AreEqual("/poems", new Navigator("  /poems  ").Path);
            Assert.AreEqual(Navigator.ROOT, new Navigator(null).Path);
            Assert.AreEqual(Navigator.ROOT, new Navigator("   ").Path);
            Assert.AreEqual(Navigator.ROOT, new Navigator("/").Path);
        }

        [TestMethod]
        public void NavigatingPushesAndAnnounces()
        {
            _navigator.Navigate("/poems");

            Assert.AreEqual("/poems", _navigator.Path);
            Assert.AreEqual(2, _navigator.Depth);
            Assert.IsTrue(_navigator.CanGoBack);
            Assert.AreEqual(Navigator.ROOT, _navigator.Previous);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void NavigatingToWhereWeAlreadyAreStillPushes()
        {
            _navigator.Navigate("/poems");
            _navigator.Navigate("/poems");

            Assert.AreEqual(3, _navigator.Depth);
            Assert.AreEqual(2, _changes);
        }

        [TestMethod]
        public void GoingBackPopsAndAnnounces()
        {
            _navigator.Navigate("/poems");
            _changes = 0;

            Assert.IsTrue(_navigator.Back());
            Assert.AreEqual(Navigator.ROOT, _navigator.Path);
            Assert.AreEqual(1, _navigator.Depth);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void GoingBackFromTheRootDoesNothingAtAll()
        {
            Assert.IsFalse(_navigator.Back());
            Assert.AreEqual(Navigator.ROOT, _navigator.Path);
            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void ReplacingSwapsTheTopWithoutDeepeningTheStack()
        {
            _navigator.Navigate("/poems");
            _changes = 0;

            _navigator.Replace("/authors");

            Assert.AreEqual("/authors", _navigator.Path);
            Assert.AreEqual(2, _navigator.Depth);
            Assert.AreEqual(Navigator.ROOT, _navigator.Previous);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void ReplacingWithTheSamePathAnnouncesNothing()
        {
            _navigator.Navigate("/poems");
            _changes = 0;

            _navigator.Replace("poems/");

            Assert.AreEqual("/poems", _navigator.Path);
            Assert.AreEqual(2, _navigator.Depth);
            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void ResettingClearsTheHistory()
        {
            _navigator.Navigate("/poems");
            _navigator.Navigate("/poems/12");
            _changes = 0;

            _navigator.Reset("/home");

            Assert.AreEqual("/home", _navigator.Path);
            Assert.AreEqual(1, _navigator.Depth);
            Assert.IsFalse(_navigator.CanGoBack);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void ResettingToWhereWeAlreadyStandAloneAnnouncesNothing()
        {
            _navigator.Reset("/");

            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void ResettingStillAnnouncesWhenItThrowsHistoryAway()
        {
            _navigator.Navigate("/poems");
            _changes = 0;

            _navigator.Reset("/");

            Assert.AreEqual(1, _navigator.Depth);
            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void APatternMatchesItsOwnPath()
        {
            _navigator.Navigate("/poems/all");

            Assert.IsTrue(_navigator.Is("/poems/all"));
            Assert.IsTrue(_navigator.Is("poems/all"));
            Assert.IsFalse(_navigator.Is("/poems"));
            Assert.IsFalse(_navigator.Is("/poems/all/extra"));
        }

        [TestMethod]
        public void ALiteralSegmentIsMatchedWhateverItsCase()
        {
            _navigator.Navigate("/Poems");

            Assert.IsTrue(_navigator.Is("/poems"));
        }

        [TestMethod]
        public void TheRootMatchesTheRootPattern()
        {
            Assert.IsTrue(_navigator.Is("/"));
            Assert.IsFalse(_navigator.Is("/poems"));
        }

        [TestMethod]
        public void APlaceholderCapturesTheSegmentAsItWasWritten()
        {
            _navigator.Navigate("/poems/Wild-Swans");

            Assert.IsTrue(_navigator.Is("/poems/{name}"));
            Assert.AreEqual("Wild-Swans", _navigator.Get("name"));
            Assert.AreEqual("Wild-Swans", _navigator.Get("NAME"));
            Assert.IsNull(_navigator.Get("other"));
            Assert.IsNull(_navigator.Get(null));
        }

        [TestMethod]
        public void SeveralPlaceholdersAreAllCaptured()
        {
            _navigator.Navigate("/poems/12/verses/3");

            Assert.IsTrue(_navigator.Is("/poems/{poem}/verses/{verse}"));
            Assert.AreEqual("12", _navigator.Get("poem"));
            Assert.AreEqual("3", _navigator.Get("verse"));
        }

        [TestMethod]
        public void AMatchWithNoPlaceholderForgetsTheOneBefore()
        {
            _navigator.Navigate("/poems/12");
            Assert.IsTrue(_navigator.Is("/poems/{id}"));

            _navigator.Navigate("/authors");
            Assert.IsTrue(_navigator.Is("/authors"));

            Assert.IsNull(_navigator.Get("id"), "a stale parameter survived a match that has none");
        }

        [TestMethod]
        public void AFailedMatchLeavesTheLastSuccessfulOneAlone()
        {
            _navigator.Navigate("/poems/12");

            Assert.IsTrue(_navigator.Is("/poems/{id}"));
            Assert.IsFalse(_navigator.Is("/authors/{id}"));

            Assert.AreEqual("12", _navigator.Get("id"));
        }

        [TestMethod]
        public void AnEmptyPlaceholderIsAnOrdinaryLiteral()
        {
            _navigator.Navigate("/poems/12");

            Assert.IsFalse(_navigator.Is("/poems/{}"));
            Assert.IsNull(_navigator.Get(string.Empty));
        }

        [TestMethod]
        public void TheHistoryIsBoundedAndDropsItsOldestEntry()
        {
            _navigator.Limit = 3;

            _navigator.Navigate("/a");
            _navigator.Navigate("/b");
            _navigator.Navigate("/c");

            Assert.AreEqual(3, _navigator.Depth);
            Assert.AreEqual("/c", _navigator.Path);
            Assert.AreEqual("/b", _navigator.Previous);

            Assert.IsTrue(_navigator.Back());
            Assert.IsTrue(_navigator.Back());
            Assert.IsFalse(_navigator.Back());
            Assert.AreEqual("/a", _navigator.Path);
        }

        [TestMethod]
        public void LoweringTheLimitTrimsWhatIsAlreadyThere()
        {
            _navigator.Navigate("/a");
            _navigator.Navigate("/b");

            _navigator.Limit = 1;

            Assert.AreEqual(1, _navigator.Depth);
            Assert.AreEqual("/b", _navigator.Path);
        }

        [TestMethod]
        public void ALimitBelowOneIsOne()
        {
            _navigator.Limit = 0;

            Assert.AreEqual(1, _navigator.Limit);
        }

        [TestMethod]
        public void NormalizingIsWhatEveryEntryPointGoesThrough()
        {
            _navigator.Navigate("poems/");

            Assert.AreEqual("/poems", _navigator.Path);

            _navigator.Replace("authors/");

            Assert.AreEqual("/authors", _navigator.Path);

            _navigator.Reset("home/");

            Assert.AreEqual("/home", _navigator.Path);
        }
    }
}
