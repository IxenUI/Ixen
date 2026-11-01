using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    internal class Task
    {
        internal string Title;
        internal string Status;
    }

    [TestClass]
    public class DataGridChoiceTests
    {
        private static readonly string[] Statuses = { "Todo", "Doing", "Done" };

        private VisualElement _root;
        private DataGrid _grid;
        private List<Task> _tasks;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _tasks = new List<Task>
            {
                new Task { Title = "Sweep", Status = "Done" },
                new Task { Title = "Cook", Status = "Todo" },
                new Task { Title = "Write", Status = "Doing" }
            };

            _grid = new DataGrid { Name = "grid", RowHeight = 28 };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 160 };

            _root.AddChild(_grid);

            _grid.SetSource(_tasks,
                new DataColumn
                {
                    Header = "Title",
                    Bind = (cell, item) => cell.Text = ((Task)item).Title,
                    Compare = (a, b) => string.CompareOrdinal(((Task)a).Title, ((Task)b).Title)
                },
                new DataColumn
                {
                    Header = "Status",
                    Bind = (cell, item) => cell.Text = ((Task)item).Status,
                    Choices = Statuses,
                    Commit = (item, status) => ((Task)item).Status = status
                });

            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(500, 500);

        private DataGridRow RowOf(string title)
            => _grid.Body.RealisedRows.Cast<DataGridRow>().First(row => row.CellAt(0).Text == title);

        private MenuItem Entry(string text) => _grid.ChoiceMenu.Items.First(item => item.Text == text);

        private void Key(Key key)
        {
            _surface.KeyDown(key, KeyModifiers.None);
            Layout();
        }

        [TestMethod]
        public void EditingAChoiceCellOpensItsChoicesWithTheCurrentOneFocused()
        {
            _grid.BeginEdit(1, 1);
            Layout();

            Assert.IsTrue(_grid.IsChoosing);
            Assert.IsFalse(_grid.IsEditing, "no text field is opened over a choice");
            CollectionAssert.AreEqual(Statuses, _grid.ChoiceMenu.Items.Select(item => item.Text).ToArray());
            Assert.AreSame(Entry("Todo"), _surface.FocusedElement);
            Assert.IsTrue(Entry("Todo").Selected, "the current value is marked");
        }

        [TestMethod]
        public void TheChoicesOpenUnderTheCell()
        {
            _grid.BeginEdit(1, 1);
            Layout();

            VisualElement cell = RowOf("Cook").CellAt(1);
            VisualElement panel = _grid.ChoiceMenu.Panel;

            Assert.AreEqual(cell.X, panel.X, 0.5f);
            Assert.AreEqual(cell.Y + cell.ActualHeight, panel.Y, 0.5f);
        }

        [TestMethod]
        public void TheChoicesFollowTheirRowWhenItMovesWhileTheyAreOpen()
        {
            _grid.BeginEdit(2, 1);
            Layout();

            _grid.SortBy(0);
            _grid.SortBy(0);
            Layout();

            VisualElement cell = RowOf("Write").CellAt(1);

            Assert.AreNotEqual(2, _grid.SelectedIndex, "the sort moved Write, or this proves nothing");
            Assert.AreEqual(cell.Y + cell.ActualHeight, _grid.ChoiceMenu.Panel.Y, 0.5f,
                "the row element under the menu now shows another item");
        }

        [TestMethod]
        public void PickingAChoiceWritesItBackAndGivesTheFocusBack()
        {
            _grid.BeginEdit(1, 1);
            Layout();

            Key(Ixen.Core.Input.Key.Down);
            Key(Ixen.Core.Input.Key.Enter);

            Assert.AreEqual("Doing", _tasks[1].Status);
            Assert.IsFalse(_grid.IsChoosing);
            Assert.AreSame(_grid, _surface.FocusedElement, "the keys belong to the grid again");
            Assert.AreEqual("Doing", RowOf("Cook").CellAt(1).Text);
        }

        [TestMethod]
        public void EscapeLeavesTheValueAsItWas()
        {
            _grid.BeginEdit(1, 1);
            Layout();

            Key(Ixen.Core.Input.Key.Down);
            Key(Ixen.Core.Input.Key.Escape);

            Assert.AreEqual("Todo", _tasks[1].Status);
            Assert.IsFalse(_grid.IsChoosing);
            Assert.AreSame(_grid, _surface.FocusedElement);
        }

        [TestMethod]
        public void F2OnTheSelectedRowOpensTheFirstEditableColumn()
        {
            _grid.Select(2);
            _grid.Focus();
            Layout();

            Key(Ixen.Core.Input.Key.F2);

            Assert.IsTrue(_grid.IsChoosing);
            Assert.AreSame(Entry("Doing"), _surface.FocusedElement);
        }

        [TestMethod]
        public void WhileChoosingTheGridDoesNotMoveItsSelection()
        {
            _grid.BeginEdit(1, 1);
            Layout();

            Key(Ixen.Core.Input.Key.End);

            Assert.AreEqual(1, _grid.SelectedIndex, "keys the menu does not want must not reach the grid");
        }

        [TestMethod]
        public void AClickOnAnEntryPicksIt()
        {
            _grid.BeginEdit(0, 1);
            Layout();

            MenuItem todo = Entry("Todo");
            float x = todo.X + 4;
            float y = todo.Y + 4;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreEqual("Todo", _tasks[0].Status);
        }

        [TestMethod]
        public void AColumnWithChoicesButNoCommitIsNotEditable()
        {
            var column = new DataColumn { Choices = Statuses };

            Assert.IsFalse(column.IsChoice);
            Assert.IsFalse(column.IsEditable);
        }

        [TestMethod]
        public void ATextEditStartsWithItsOwnRowsTextWhenTheSelectionJumpsToIt()
        {
            _grid.SetSource(_tasks,
                new DataColumn
                {
                    Header = "Title",
                    Bind = (cell, item) => cell.Text = ((Task)item).Title,
                    Commit = (item, title) => ((Task)item).Title = title
                });
            Layout();

            _grid.Select(0);
            Layout();
            _grid.BeginEdit(2, 0);

            Assert.AreEqual("Write", _grid.Editor.Text,
                "the jump refreshes the rows, and until the next frame their indices name other items");
        }

        [TestMethod]
        public void TheMenuIsBuiltOnlyWhenFirstAskedFor()
        {
            Assert.IsNull(_grid.ChoiceMenu, "a grid that never chooses pays for no menu");
        }
    }
}
