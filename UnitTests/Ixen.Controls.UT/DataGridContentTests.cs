using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DataGridContentTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 20;

        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _created;
        private readonly List<VisualElement> _bound = new();

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = Enumerable.Range(0, 40).Select(i => new Person { Name = "poet " + i, Age = i }).ToList();

            _grid = new DataGrid { Name = "grid", RowHeight = ROW };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 130 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            _root.AddChild(_grid);

            _grid.SetSource(_people, SongColumn(), AgeColumn());

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private DataColumn SongColumn() => new DataColumn
        {
            Header = "Song",
            Create = () =>
            {
                _created++;

                var song = new VisualElement { Name = "song" };
                song.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
                song.AddChild(new VisualElement { Name = "title" });
                song.AddChild(new VisualElement { Name = "artist" });

                return song;
            },
            Bind = (content, item) =>
            {
                _bound.Add(content);
                content.ChildElements[0].Text = ((Person)item).Name;
                content.ChildElements[1].Text = "age " + ((Person)item).Age;
            },
            Value = item => ((Person)item).Name,
            Commit = (item, text) => ((Person)item).Name = text,
            Compare = (a, b) => ((Person)b).Age.CompareTo(((Person)a).Age)
        };

        private static DataColumn AgeColumn() => new DataColumn
        {
            Header = "Age",
            Width = 60,
            Bind = (cell, item) => cell.Text = ((Person)item).Age.ToString()
        };

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private DataGridRow RowShowing(int index)
            => (DataGridRow)_grid.Body.RealisedRows.First(row => _grid.Body.IndexOfRow(row) == index);

        private string TitleOf(int index) => _grid.ContentOf(RowShowing(index), 0).ChildElements[0].Text;

        [TestMethod]
        public void BindReceivesTheContentAndTheContentIsInTheCell()
        {
            DataGridRow row = RowShowing(0);
            VisualElement content = _grid.ContentOf(row, 0);

            Assert.IsNotNull(content);
            Assert.AreEqual("song", content.Name);
            Assert.AreSame(row.CellAt(0), content.Parent);
            Assert.IsTrue(_bound.Contains(content), "the fill function is handed what Create built, not the cell");
            Assert.AreEqual("poet 0", content.ChildElements[0].Text);
            Assert.AreEqual("age 0", content.ChildElements[1].Text);
            Assert.IsNull(_grid.ContentOf(row, 1), "a column without Create keeps writing on its cell");
            Assert.AreEqual("0", row.CellAt(1).Text);
        }

        [TestMethod]
        public void ContentIsBuiltOncePerCellAndRefilledAsRowsAreRecycled()
        {
            int realised = _grid.Body.RealisedCount;

            Assert.AreEqual(realised, _created, "one per realised row");

            for (int step = 1; step <= 10; step++)
            {
                _grid.Body.ScrollY = step * 3 * ROW;
                Layout();
            }

            Assert.AreEqual(realised, _created, "scrolling through thirty rows built nothing more");
            Assert.AreEqual("poet " + _grid.Body.FirstRealised, TitleOf(_grid.Body.FirstRealised),
                "and the recycled content shows the item now in its row");
        }

        [TestMethod]
        public void ASortKeepsEachContentOnItsItem()
        {
            _grid.SortBy(0);
            Layout();

            Assert.AreEqual("poet 39", TitleOf(0));
            Assert.AreEqual("poet 38", TitleOf(1));
        }

        [TestMethod]
        public void EditingTakesTheContentOutAndPutsItBack()
        {
            DataGridRow row = RowShowing(2);
            VisualElement content = _grid.ContentOf(row, 0);

            _grid.BeginEdit(2, 0);
            Layout();

            Assert.IsNull(content.Parent, "the editor takes the cell alone");
            Assert.IsTrue(row.CellAt(0).ChildElements.Any(child => child is TextField));
            Assert.AreEqual("poet 2", row.CellAt(0).ChildElements.OfType<TextField>().First().Text,
                "a content column says what it edits through Value");

            _grid.CancelEdit();
            Layout();

            Assert.AreSame(row.CellAt(0), content.Parent, "and the content is back");
            Assert.AreEqual(1, row.CellAt(0).ChildElements.Count);
        }

        [TestMethod]
        public void ACommittedEditShowsInTheContent()
        {
            _grid.BeginEdit(1, 0);
            Layout();

            TextField editor = RowShowing(1).CellAt(0).ChildElements.OfType<TextField>().First();
            editor.Text = "Hopkins";
            _grid.CommitEdit();
            Layout();

            Assert.AreEqual("Hopkins", TitleOf(1));
        }

        [TestMethod]
        public void ChangingTheColumnsReplacesTheContent()
        {
            _grid.SetSource(_people, AgeColumn(), SongColumn());
            Layout();

            DataGridRow row = RowShowing(0);

            Assert.IsNull(_grid.ContentOf(row, 0), "the first column has no content any more");
            Assert.AreEqual(0, row.CellAt(0).ChildElements.Count, "and its old content left the cell");
            Assert.AreEqual("poet 0", _grid.ContentOf(row, 1).ChildElements[0].Text);
        }
    }
}
