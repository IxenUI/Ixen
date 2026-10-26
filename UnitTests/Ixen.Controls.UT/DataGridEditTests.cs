using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DataGridEditTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private VisualElement _elsewhere;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _commits;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>
            {
                new Person { Name = "Yeats", Age = 73 },
                new Person { Name = "Blake", Age = 69 },
                new Person { Name = "Keats", Age = 25 }
            };

            _grid = new DataGrid { Name = "grid", RowHeight = 20 };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };

            _elsewhere = new VisualElement { Name = "elsewhere", Focusable = true };
            _elsewhere.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _root.AddChildren(_grid, _elsewhere);

            _grid.SetSource(_people, NameColumn(), AgeColumn(), NoteColumn());

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private DataColumn NameColumn() => new DataColumn
        {
            Header = "Name",
            Width = 120,
            Bind = (cell, item) => cell.Text = ((Person)item).Name,
            Compare = (a, b) => string.CompareOrdinal(((Person)a).Name, ((Person)b).Name),
            Commit = (item, text) => { _commits++; ((Person)item).Name = text; }
        };

        private DataColumn AgeColumn() => new DataColumn
        {
            Header = "Age",
            Width = 80,
            Bind = (cell, item) => cell.Text = ((Person)item).Age + " years",
            Value = item => ((Person)item).Age.ToString(),
            Commit = (item, text) =>
            {
                _commits++;

                if (int.TryParse(text, out int age))
                {
                    ((Person)item).Age = age;
                }
            }
        };

        private DataColumn NoteColumn() => new DataColumn
        {
            Header = "Note",
            Bind = (cell, item) => cell.Text = "read only"
        };

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Key(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        private DataGridRow RowOf(Person person)
        {
            foreach (VisualElement realised in _grid.Body.RealisedRows)
            {
                if (ReferenceEquals(_grid.ItemAt(_grid.Body.IndexOfRow(realised)), person))
                {
                    return (DataGridRow)realised;
                }
            }

            return null;
        }

        private void DoubleClick(VisualElement cell)
        {
            float x = cell.X + 4;
            float y = cell.Y + 4;

            for (int i = 0; i < 2; i++)
            {
                _surface.PointerDown(x, y, PointerButton.Left);
                _surface.PointerUp(x, y, PointerButton.Left);
            }

            Layout();
        }

        private void StartOnFirstRow()
        {
            _grid.Select(0);
            _grid.Focus();
            Layout();
            Key(Ixen.Core.Input.Key.F2);
        }

        [TestMethod]
        public void F2EditsTheFirstEditableCellOfTheSelectedRow()
        {
            StartOnFirstRow();

            Assert.IsTrue(_grid.IsEditing);
            Assert.AreEqual(0, _grid.EditingColumn);
            Assert.AreSame(_grid.Editor, _surface.FocusedElement, "the editor has the focus, ready to type");
            Assert.AreEqual("Yeats", _grid.Editor.Text);
            Assert.AreSame(RowOf(_people[0]).CellAt(0), _grid.Editor.Parent, "it sits in the cell it edits");
            Assert.IsNull(RowOf(_people[0]).CellAt(0).Text, "and the cell's own text makes way for it");
        }

        [TestMethod]
        public void TypingThenEnterCommits()
        {
            StartOnFirstRow();

            _surface.KeyDown(Ixen.Core.Input.Key.End, KeyModifiers.None);
            _surface.TextInput("s");
            Key(Ixen.Core.Input.Key.Enter);

            Assert.IsFalse(_grid.IsEditing);
            Assert.AreEqual("Yeatss", _people[0].Name, "the column's Commit wrote the text back");
            Assert.AreEqual(1, _commits);
            Assert.AreSame(_grid, _surface.FocusedElement, "and the focus is back on the grid");
            Assert.AreEqual("Yeatss", RowOf(_people[0]).CellAt(0).Text, "the cell shows the new value");
        }

        [TestMethod]
        public void EscapeCancels()
        {
            StartOnFirstRow();

            _grid.Editor.Text = "Joyce";
            Key(Ixen.Core.Input.Key.Escape);

            Assert.IsFalse(_grid.IsEditing);
            Assert.AreEqual("Yeats", _people[0].Name);
            Assert.AreEqual(0, _commits);
            Assert.AreEqual("Yeats", RowOf(_people[0]).CellAt(0).Text);
        }

        [TestMethod]
        public void TabCommitsAndMovesToTheNextEditableCell()
        {
            StartOnFirstRow();

            _grid.Editor.Text = "Joyce";
            Key(Ixen.Core.Input.Key.Tab);

            Assert.AreEqual("Joyce", _people[0].Name);
            Assert.AreEqual(1, _grid.EditingColumn);
            Assert.AreEqual("73", _grid.Editor.Text, "Value gives the raw text to edit, not the displayed one");

            _grid.Editor.Text = "74";
            Key(Ixen.Core.Input.Key.Tab);

            Assert.AreEqual(74, _people[0].Age);
            Assert.IsFalse(_grid.IsEditing, "the note column is not editable, so the edit ends there");

            StartOnFirstRow();
            Key(Ixen.Core.Input.Key.Tab, KeyModifiers.Shift);

            Assert.IsFalse(_grid.IsEditing, "nothing editable before the first column either");
        }

        [TestMethod]
        public void ADoubleClickEditsThatCell()
        {
            DoubleClick(RowOf(_people[1]).CellAt(1));

            Assert.IsTrue(_grid.IsEditing);
            Assert.AreSame(_people[1], _grid.EditingItem);
            Assert.AreEqual(1, _grid.EditingColumn);
        }

        [TestMethod]
        public void ADoubleClickOnAReadOnlyCellEditsNothing()
        {
            DoubleClick(RowOf(_people[1]).CellAt(2));

            Assert.IsFalse(_grid.IsEditing);
        }

        [TestMethod]
        public void MovingTheFocusAwayCommits()
        {
            StartOnFirstRow();

            _grid.Editor.Text = "Joyce";
            _surface.Focus(_elsewhere);
            Layout();

            Assert.IsFalse(_grid.IsEditing);
            Assert.AreEqual("Joyce", _people[0].Name, "like a spreadsheet, leaving the cell keeps what was typed");
            Assert.AreSame(_elsewhere, _surface.FocusedElement, "without taking the focus back");
        }

        [TestMethod]
        public void EnterDoesNotCommitTwiceWhenTheFocusLeaves()
        {
            StartOnFirstRow();

            _grid.Editor.Text = "Joyce";
            Key(Ixen.Core.Input.Key.Enter);
            Layout();

            Assert.AreEqual(1, _commits, "the focus loss that follows an Enter belongs to an edit already over");
        }

        [TestMethod]
        public void AnEditedSortedColumnSortsAgainAndKeepsTheRow()
        {
            _grid.SortBy(0);
            Layout();

            Person keats = _people[2];
            _grid.Select(1);
            _grid.Focus();
            Layout();

            Assert.AreSame(keats, _grid.SelectedItem);

            Key(Ixen.Core.Input.Key.F2);
            _grid.Editor.Text = "Auden";
            Key(Ixen.Core.Input.Key.Enter);

            Assert.AreSame(keats, _grid.ItemAt(0), "Auden now sorts first");
            Assert.AreSame(keats, _grid.SelectedItem, "and the selection followed the person, not the position");
        }

        [TestMethod]
        public void TheEditorFollowsItsItemWhenTheRowsAreRebound()
        {
            StartOnFirstRow();

            _grid.SortBy(0);
            Layout();

            Assert.AreSame(RowOf(_people[0]).CellAt(0), _grid.Editor.Parent,
                "a re-sort rebinds every recycled row; the editor goes with Yeats, not with row one");
        }

        [TestMethod]
        public void ScrollingTheEditedRowAwayCommitsAndLeavesNoEditorBehind()
        {
            for (int i = 0; i < 40; i++)
            {
                _people.Add(new Person { Name = "Poet " + i, Age = i });
            }

            _grid.Refresh();
            Layout();
            StartOnFirstRow();

            _grid.Editor.Text = "Joyce";
            _grid.Body.ScrollTo(30);
            Layout();
            Layout();

            foreach (VisualElement row in _grid.Body.RealisedRows)
            {
                Assert.AreNotSame(row, _grid.Editor.Parent?.Parent,
                    "a recycled row now showing another person must not keep Yeats's editor");
            }

            Assert.IsFalse(_grid.IsEditing);
            Assert.AreEqual("Joyce", _people[0].Name, "the edit was kept when its row went out of view");
        }

        [TestMethod]
        public void TheGridLeavesTheArrowsToTheEditor()
        {
            StartOnFirstRow();

            Key(Ixen.Core.Input.Key.Down);

            Assert.IsTrue(_grid.IsEditing, "a key the field does not use must not move the grid under it");
            Assert.AreEqual(0, _grid.SelectedIndex);
        }

        [TestMethod]
        public void TwoClicksBeforeAFrameSelectTheRowsTheyHit()
        {
            VisualElement blake = RowOf(_people[1]).CellAt(0);
            VisualElement keats = RowOf(_people[2]).CellAt(0);

            _surface.PointerDown(blake.X + 4, blake.Y + 4, PointerButton.Left);
            _surface.PointerUp(blake.X + 4, blake.Y + 4, PointerButton.Left);
            _surface.PointerDown(keats.X + 4, keats.Y + 4, PointerButton.Left);
            _surface.PointerUp(keats.X + 4, keats.Y + 4, PointerButton.Left);

            Assert.AreSame(_people[2], _grid.SelectedItem,
                "a selection refreshes the list, which forgets its rows until the next layout; a row "
                + "answers with the item it was bound to rather than a position that is no longer realised");
        }

        [TestMethod]
        public void AGridWithNothingEditableIgnoresF2()
        {
            _grid.SetSource(_people, new DataColumn { Header = "Name", Bind = (cell, item) => cell.Text = ((Person)item).Name });
            _grid.Select(0);
            _grid.Focus();
            Layout();

            Key(Ixen.Core.Input.Key.F2);

            Assert.IsFalse(_grid.IsEditing);
        }
    }
}
