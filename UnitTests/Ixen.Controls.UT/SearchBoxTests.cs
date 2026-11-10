using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class SearchBoxTests
    {
        private static readonly string[] CATALOGUE =
        {
            "Abbey Road", "Abraxas", "Achtung Baby", "Aja", "Blue", "Blonde on Blonde"
        };

        private VisualElement _root;
        private VisualElement _elsewhere;
        private SearchBox _search;
        private IxenSurface _surface;
        private readonly List<string> _chosen = new();
        private readonly List<string> _submitted = new();
        private int _queries;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var bar = new VisualElement { Name = "bar" };
            bar.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            bar.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _search = new SearchBox { Name = "search" };
            _search.QueryChanged += (sender, e) =>
            {
                _queries++;
                string query = _search.Query ?? string.Empty;
                _search.SetSuggestions(query.Length == 0
                    ? new string[0]
                    : CATALOGUE.Where(title => title.StartsWith(query, System.StringComparison.OrdinalIgnoreCase)));
            };
            _search.SuggestionChosen += (sender, e) => _chosen.Add(e.Text);
            _search.Submitted += (sender, e) => _submitted.Add(e.Text);

            _elsewhere = new VisualElement { Name = "elsewhere", Focusable = true };
            _elsewhere.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            bar.AddChild(_search);
            _root.AddChildren(bar, _elsewhere);

            var styles = new StyleRegistry();
            styles.AddLoadedDefaults();

            _surface = new IxenSurface(_root) { Styles = styles };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(600, 400);

        private void Type(string text)
        {
            _surface.Focus(_search.Field);

            foreach (char c in text)
            {
                _surface.TextInput(c.ToString());
            }

            Layout();
        }

        private void Key(Key key)
        {
            _surface.KeyDown(key, KeyModifiers.None);
            Layout();
        }

        private VisualElement Row(int index) => _search.Popup.ChildElements[0].ChildElements[index];

        [TestMethod]
        public void TypingOffersWhatMatchesBelowTheField()
        {
            Type("ab");

            Assert.IsTrue(_search.IsOpen);
            Assert.AreEqual("Abbey Road,Abraxas", string.Join(",", _search.Suggestions));
            Assert.AreEqual(_search.Y + _search.ActualHeight, Row(0).Y, 8f, "the list hangs under the field");
            Assert.AreEqual(_search.Field, _surface.FocusedElement, "and the caret stays where the typing is");
        }

        [TestMethod]
        public void TheArrowsWalkTheSuggestionsWhileTheFocusStaysInTheField()
        {
            Type("a");

            Key(Ixen.Core.Input.Key.Down);
            Key(Ixen.Core.Input.Key.Down);

            Assert.AreEqual(1, _search.Highlighted);
            Assert.IsTrue(Row(1).HasState(SearchBox.ACTIVE));
            Assert.IsTrue(Row(1).Selected, "the highlighted one is the selected item for a screen reader");
            Assert.AreEqual(_search.Field, _surface.FocusedElement);

            Key(Ixen.Core.Input.Key.Up);
            Key(Ixen.Core.Input.Key.Up);

            Assert.AreEqual(-1, _search.Highlighted, "above the first is back to what was typed");
        }

        [TestMethod]
        public void EnterChoosesTheHighlightedOneAndClosesWithoutReopening()
        {
            Type("ac");
            Key(Ixen.Core.Input.Key.Down);
            Key(Ixen.Core.Input.Key.Enter);

            Assert.AreEqual("Achtung Baby", string.Join(",", _chosen));
            Assert.AreEqual("Achtung Baby", _search.Query);
            Assert.IsFalse(_search.IsOpen);

            _search.SetSuggestions(new[] { "Achtung Baby", "Achtung Baby (Deluxe)" });
            Layout();

            Assert.IsFalse(_search.IsOpen,
                "the chosen text changed the query, and the answer to that query arrives later - it must not "
                + "reopen the list the user just closed by choosing");
            Assert.AreEqual(0, _submitted.Count);
        }

        [TestMethod]
        public void EnterWithNothingHighlightedSubmitsWhatWasTyped()
        {
            Type("blu");
            Key(Ixen.Core.Input.Key.Enter);

            Assert.AreEqual("blu", string.Join(",", _submitted));
            Assert.IsFalse(_search.IsOpen);
        }

        [TestMethod]
        public void ClickingASuggestionChoosesIt()
        {
            Type("bl");

            VisualElement row = Row(1);
            float x = row.X + 10;
            float y = row.Y + row.ActualHeight / 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            Layout();

            Assert.IsTrue(_search.IsOpen, "pressing a suggestion takes the focus from the field, which must not close the list under the press");

            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreEqual("Blonde on Blonde", string.Join(",", _chosen));
            Assert.AreEqual(_search.Field, _surface.FocusedElement, "the focus comes back to the field");
        }

        [TestMethod]
        public void EscapeClosesAndTypingReopens()
        {
            Type("a");
            Key(Ixen.Core.Input.Key.Escape);

            Assert.IsFalse(_search.IsOpen);
            Assert.AreEqual("a", _search.Query, "escape closes the list, it does not clear the query");

            Type("b");

            Assert.IsTrue(_search.IsOpen);
        }

        [TestMethod]
        public void DownReopensAClosedList()
        {
            Type("a");
            Key(Ixen.Core.Input.Key.Escape);
            Key(Ixen.Core.Input.Key.Down);

            Assert.IsTrue(_search.IsOpen);
            Assert.AreEqual(0, _search.Highlighted);
        }

        [TestMethod]
        public void LeavingTheFieldClosesTheList()
        {
            Type("a");

            _surface.Focus(_elsewhere);
            Layout();

            Assert.IsFalse(_search.IsOpen);

            Type("b");
            float x = _elsewhere.X + 10;
            float y = _elsewhere.Y + 10;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.IsFalse(_search.IsOpen, "a press anywhere else closes it too");
        }

        [TestMethod]
        public void NothingToOfferShowsNothing()
        {
            Type("zz");

            Assert.IsFalse(_search.IsOpen);
            Assert.AreEqual(2, _queries);
        }

        [TestMethod]
        public void SuggestionsArrivingWhileTheFieldIsNotFocusedWait()
        {
            _search.SetSuggestions(CATALOGUE);
            Layout();

            Assert.IsFalse(_search.IsOpen, "late answers to an abandoned search do not pop a list over the page");
        }

        [TestMethod]
        public void TheListIsExposedAsAnExpandableList()
        {
            Type("a");

            AccessibleNode node = Walk(_surface.BuildAccessibilityTree()).First(n => n.Element == _search);

            Assert.IsTrue(node.States.HasFlag(AccessibleStates.Expanded));
            Assert.IsTrue(Walk(node).Any(n => n.Role == AccessibleRole.List));
            Assert.AreEqual(4, Walk(node).Count(n => n.Role == AccessibleRole.ListItem));
        }

        private static IEnumerable<AccessibleNode> Walk(AccessibleNode node)
        {
            yield return node;

            foreach (AccessibleNode child in node.Children)
            {
                foreach (AccessibleNode descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
