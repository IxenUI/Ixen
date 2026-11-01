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
    internal class Chore
    {
        internal string Title;
        internal bool Done;
    }

    [TestClass]
    public class DataGridCheckTests
    {
        private VisualElement _root;
        private DataGrid _grid;
        private List<Chore> _chores;
        private IxenSurface _surface;
        private int _toggles;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _chores = new List<Chore>
            {
                new Chore { Title = "Sweep", Done = true },
                new Chore { Title = "Cook" },
                new Chore { Title = "Write" }
            };

            _grid = new DataGrid { Name = "grid", RowHeight = 20, MultiSelect = true };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };

            _root.AddChild(_grid);

            _grid.SetSource(_chores, TitleColumn(), DoneColumn(true));

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private static DataColumn TitleColumn()
            => new DataColumn
            {
                Header = "Title",
                Bind = (cell, item) => cell.Text = ((Chore)item).Title,
                Compare = (a, b) => string.CompareOrdinal(((Chore)a).Title, ((Chore)b).Title)
            };

        private DataColumn DoneColumn(bool toggleable)
            => new DataColumn
            {
                Header = "Done",
                Checked = item => ((Chore)item).Done,
                Toggle = toggleable
                    ? (item, done) =>
                    {
                        _toggles++;
                        ((Chore)item).Done = done;
                    }
                    : null
            };

        private void Layout() => _surface.ComputeLayout(400, 400);

        private DataGridRow RowOf(string title)
            => _grid.Body.RealisedRows.Cast<DataGridRow>().First(row => row.CellAt(0).Text == title);

        private CheckBox CheckOf(string title)
            => RowOf(title).CellAt(1).ChildElements.OfType<CheckBox>().SingleOrDefault();

        private string Done() => string.Join(",", _chores.Where(c => c.Done).Select(c => c.Title));

        [TestMethod]
        public void ACheckColumnShowsACheckBoxThatReadsTheItem()
        {
            Assert.IsTrue(CheckOf("Sweep").Checked);
            Assert.IsFalse(CheckOf("Cook").Checked);
            Assert.IsNull(RowOf("Sweep").CellAt(0).ChildElements.OfType<CheckBox>().SingleOrDefault(),
                "a text column holds no box");
        }

        [TestMethod]
        public void ClickingTheBoxWritesTheItemBack()
        {
            CheckOf("Cook").Activate();
            Layout();

            Assert.AreEqual("Sweep,Cook", Done());
            Assert.IsTrue(CheckOf("Cook").Checked);
            Assert.AreEqual(1, _toggles);
        }

        [TestMethod]
        public void TheBoxIsNotATabStopOfItsOwn()
        {
            Assert.IsFalse(CheckOf("Cook").Focusable, "the grid is one tab stop; a box per row would be dozens");
        }

        [TestMethod]
        public void WithoutToggleTheBoxIsReadOnly()
        {
            _grid.SetSource(_chores, TitleColumn(), DoneColumn(false));
            Layout();

            CheckBox box = CheckOf("Cook");

            Assert.IsFalse(box.IsEnabled);

            box.Activate();

            Assert.AreEqual("Sweep", Done());
        }

        [TestMethod]
        public void SpaceTogglesEverySelectedRowTowardsTheOppositeOfTheCurrentOne()
        {
            _grid.Select(1);
            _grid.ToggleSelection(2);
            _grid.Focus();

            Assert.IsTrue(_surface.KeyDown(Key.Space, KeyModifiers.None));
            Layout();

            Assert.AreEqual("Sweep,Cook,Write", Done(), "Write is current and unchecked, so both become checked");
            Assert.IsTrue(CheckOf("Write").Checked, "the boxes on screen follow at once");

            _surface.KeyDown(Key.Space, KeyModifiers.None);
            Layout();

            Assert.AreEqual("Sweep", Done());
        }

        [TestMethod]
        public void SpaceIsLeftAloneWhenNoColumnCanBeToggled()
        {
            _grid.SetSource(_chores, TitleColumn(), DoneColumn(false));
            Layout();
            _grid.Select(1);
            _grid.Focus();

            Assert.IsFalse(_surface.KeyDown(Key.Space, KeyModifiers.None));
        }

        [TestMethod]
        public void ARecycledRowFollowsItsNewItem()
        {
            _grid.SortBy(0);
            Layout();

            Assert.IsTrue(CheckOf("Sweep").Checked, "sorting moves Sweep to another row, and its box with it");
            Assert.IsFalse(CheckOf("Cook").Checked);

            CheckOf("Write").Activate();

            Assert.IsTrue(_chores[2].Done, "the click lands on the item the row shows now");
        }

        [TestMethod]
        public void ColumnsThatStopBeingChecksLoseTheirBox()
        {
            _grid.SetSource(_chores, TitleColumn(), TitleColumn());
            Layout();

            Assert.IsNull(CheckOf("Sweep"));
        }

        [TestMethod]
        public void ACheckColumnIsNotATextEditor()
        {
            DataColumn column = DoneColumn(true);

            column.Commit = (item, text) => { };

            Assert.IsFalse(column.IsEditable, "F2 would open a text field over a box");
        }

        [TestMethod]
        public void UnderTheThemeTheBoxIsDressedAndSitsInTheMiddleOfItsRow()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());
            _surface.Styles = registry;
            _root.Invalidate();
            Layout();

            CheckBox box = CheckOf("Cook");
            DataGridRow row = RowOf("Cook");

            Assert.AreEqual(18f, box.ActualWidth, "the theme's #CheckBox rule reached it, so it has a size to click");
            Assert.AreEqual(row.Y + (row.ActualHeight - box.ActualHeight) / 2, box.Y, 0.5f,
                "it sits in the middle of the row, as the text beside it does");
        }

        [TestMethod]
        public void AClickOnTheBoxTogglesItAndSelectsTheRow()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());
            _surface.Styles = registry;
            _root.Invalidate();
            Layout();

            CheckBox box = CheckOf("Write");
            float x = box.X + 9;
            float y = box.Y + 9;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreEqual("Sweep,Write", Done());
            Assert.AreSame(_chores[2], _grid.SelectedItem, "the click reaches the row as well");
        }

        [TestMethod]
        public void AScreenReaderMeetsACheckBoxInTheCell()
        {
            AccessibleNode cell = AccessibleSearch.Find(_surface.BuildAccessibilityTree(),
                node => node.Role == AccessibleRole.TableCell && node.Children.Any(c => c.Role == AccessibleRole.CheckBox)
                    && node.RowIndex == 1);

            Assert.IsNotNull(cell);
            Assert.AreEqual(1, cell.ColumnIndex);
            Assert.IsTrue(cell.Children.First(c => c.Role == AccessibleRole.CheckBox).HasState(AccessibleStates.Checked));
        }
    }
}
