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

        [TestMethod]
        public void AFreshNavigatorHasNothingInFront()
        {
            Assert.IsFalse(_navigator.CanGoForward);
            Assert.IsNull(_navigator.Next);
            Assert.IsFalse(_navigator.Forward());
            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void GoingBackPutsWhereWeWereInFront()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();

            Assert.AreEqual(Navigator.ROOT, _navigator.Path);
            Assert.IsTrue(_navigator.CanGoForward);
            Assert.AreEqual("/poems", _navigator.Next);
        }

        [TestMethod]
        public void GoingForwardTakesItBack()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();

            Assert.IsTrue(_navigator.Forward());

            Assert.AreEqual("/poems", _navigator.Path);
            Assert.AreEqual(2, _navigator.Depth);
            Assert.IsFalse(_navigator.CanGoForward);
            Assert.AreEqual(3, _changes);
        }

        [TestMethod]
        public void ForwardUnwindsInTheOrderBackWoundIt()
        {
            _navigator.Navigate("/poems");
            _navigator.Navigate("/poems/swans");
            _navigator.Back();
            _navigator.Back();

            Assert.AreEqual(Navigator.ROOT, _navigator.Path);

            _navigator.Forward();

            Assert.AreEqual("/poems", _navigator.Path);

            _navigator.Forward();

            Assert.AreEqual("/poems/swans", _navigator.Path);
            Assert.IsFalse(_navigator.CanGoForward);
        }

        [TestMethod]
        public void NavigatingThrowsAwayWhatWasInFront()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();
            _navigator.Navigate("/authors");

            Assert.IsFalse(_navigator.CanGoForward);
            Assert.IsNull(_navigator.Next);
        }

        [TestMethod]
        public void ReplacingKeepsWhatIsInFront()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();
            _navigator.Replace("/home");

            Assert.AreEqual("/home", _navigator.Path);
            Assert.IsTrue(_navigator.CanGoForward);
            Assert.AreEqual("/poems", _navigator.Next);
        }

        [TestMethod]
        public void ResettingThrowsAwayBothDirections()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();
            _navigator.Reset("/home");

            Assert.AreEqual(1, _navigator.Depth);
            Assert.IsFalse(_navigator.CanGoBack);
            Assert.IsFalse(_navigator.CanGoForward);
        }

        [TestMethod]
        public void ResettingToWhereWeAreStillClearsWhatIsInFront()
        {
            _navigator.Navigate("/poems");
            _navigator.Back();

            int before = _changes;

            _navigator.Reset(Navigator.ROOT);

            Assert.IsFalse(_navigator.CanGoForward);
            Assert.AreEqual(before + 1, _changes, "something observable moved, so it announces");
        }

        [TestMethod]
        public void ResettingToWhereWeAreWithNothingInFrontAnnouncesNothing()
        {
            int before = _changes;

            _navigator.Reset(Navigator.ROOT);

            Assert.AreEqual(before, _changes);
        }

        [TestMethod]
        public void LoweringTheLimitBoundsWhatIsInFrontToo()
        {
            for (int index = 0; index < 5; index++)
            {
                _navigator.Navigate("/step/" + index);
            }

            for (int index = 0; index < 5; index++)
            {
                _navigator.Back();
            }

            Assert.AreEqual(1, _navigator.Depth);
            Assert.AreEqual("/step/0", _navigator.Next);

            _navigator.Limit = 3;

            int steps = 0;

            while (steps < 10 && _navigator.Forward())
            {
                steps++;
            }

            Assert.AreEqual(3, steps, "the oldest entries in front are the ones that go");
            Assert.AreEqual("/step/2", _navigator.Path);
        }

        [TestMethod]
        public void AQueryIsKeptOnTheLocationAndOffThePath()
        {
            _navigator.Navigate("/poems?sort=date&page=2");

            Assert.AreEqual("/poems", _navigator.Path);
            Assert.AreEqual("/poems?sort=date&page=2", _navigator.Location);
            Assert.AreEqual("sort=date&page=2", _navigator.QueryString);
        }

        [TestMethod]
        public void AQueryIsReadByName()
        {
            _navigator.Navigate("/poems?sort=date&page=2");

            Assert.AreEqual("date", _navigator.Query("sort"));
            Assert.AreEqual("2", _navigator.Query("page"));
            Assert.IsNull(_navigator.Query("missing"));
            Assert.IsNull(_navigator.Query(null));
        }

        [TestMethod]
        public void AQueryNameIgnoresItsCaseLikeAPathParameter()
        {
            _navigator.Navigate("/poems?Sort=date");

            Assert.AreEqual("date", _navigator.Query("sort"));
        }

        [TestMethod]
        public void ANameWithNoValueIsPresentAndEmpty()
        {
            _navigator.Navigate("/poems?draft&sort=date");

            Assert.AreEqual(string.Empty, _navigator.Query("draft"));
            Assert.AreEqual("date", _navigator.Query("sort"));
        }

        [TestMethod]
        public void TheLastEntryOfARepeatedNameWins()
        {
            _navigator.Navigate("/poems?sort=date&sort=title");

            Assert.AreEqual("title", _navigator.Query("sort"));
        }

        [TestMethod]
        public void AQueryIsPercentDecoded()
        {
            _navigator.Navigate("/poems?title=the%20wild%20swans&by=W%2EB%2E");

            Assert.AreEqual("the wild swans", _navigator.Query("title"));
            Assert.AreEqual("W.B.", _navigator.Query("by"));
        }

        [TestMethod]
        public void APlusStaysAPlusRatherThanBecomingASpace()
        {
            _navigator.Navigate("/sum?q=1+1");

            Assert.AreEqual("1+1", _navigator.Query("q"));
        }

        [TestMethod]
        public void AnEmptyQueryIsNoQueryAtAll()
        {
            _navigator.Navigate("/poems?");

            Assert.AreEqual("/poems", _navigator.Location);
            Assert.IsNull(_navigator.QueryString);
            Assert.IsNull(_navigator.Query("anything"));
        }

        [TestMethod]
        public void NormalizingLeavesTheQueryAloneAndStillTidiesThePath()
        {
            Assert.AreEqual("/poems?x=1", Navigator.Normalized("poems/?x=1"));
            Assert.AreEqual("/poems?x=1", Navigator.Normalized("  /poems?x=1  "));
            Assert.AreEqual("/?x=1", Navigator.Normalized("?x=1"));
            Assert.AreEqual("/poems?a=1&b=2", Navigator.Normalized("/poems?a=1&b=2"));
        }

        [TestMethod]
        public void APatternMatchesThePathAndIgnoresTheQuery()
        {
            _navigator.Navigate("/poems/swans?sort=date");

            Assert.IsTrue(_navigator.Is("/poems/{id}"));
            Assert.AreEqual("swans", _navigator.Get("id"));
            Assert.IsTrue(_navigator.Is("/poems/swans?sort=title"),
                "a query on the pattern says nothing about the path");
        }

        [TestMethod]
        public void GoingBackCarriesTheQueryWithIt()
        {
            _navigator.Navigate("/poems?sort=date");
            _navigator.Navigate("/authors");
            _navigator.Back();

            Assert.AreEqual("date", _navigator.Query("sort"));
            Assert.AreEqual("/poems?sort=date", _navigator.Location);
        }

        [TestMethod]
        public void TwoLocationsThatDifferOnlyByTheirQueryAreTwoEntries()
        {
            _navigator.Navigate("/poems?sort=date");
            _navigator.Replace("/poems?sort=title");

            Assert.AreEqual("title", _navigator.Query("sort"));
            Assert.AreEqual(2, _changes, "the path did not move, but the location did");
        }

        [TestMethod]
        public void ReplacingWithTheSameLocationQueryIncludedAnnouncesNothing()
        {
            _navigator.Navigate("/poems?sort=date");

            int before = _changes;

            _navigator.Replace("/poems?sort=date");

            Assert.AreEqual(before, _changes);
        }

        [TestMethod]
        public void ResettingToTheSameLocationQueryIncludedAnnouncesNothing()
        {
            _navigator.Reset("/poems?sort=date");

            int before = _changes;

            _navigator.Reset("/poems?sort=date");

            Assert.AreEqual(before, _changes);
        }

        [TestMethod]
        public void TheQueryIsReReadWhenTheLocationMoves()
        {
            _navigator.Navigate("/poems?sort=date");

            Assert.AreEqual("date", _navigator.Query("sort"));

            _navigator.Navigate("/poems?sort=title");

            Assert.AreEqual("title", _navigator.Query("sort"));

            _navigator.Back();

            Assert.AreEqual("date", _navigator.Query("sort"));
        }
    }
}
