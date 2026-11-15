using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ContextMenuTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private IxenSurface _surface;
        private Menu _menu;
        private ContextEventArgs _asked;

        private void Host(VisualElement control)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _menu = new Menu { Name = "menu" };
            _menu.AddChild(new MenuItem { Text = "Play next" });
            _root.AddChildren(control, _menu);
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };
            Layout();

            control.ContextRequested += (sender, e) =>
            {
                _asked = e;
                e.Handled = true;
                _menu.OpenAt(e.X, e.Y);
            };
        }

        private void Layout()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        [TestMethod]
        public void FromTheKeyboardAGridAsksAtItsCurrentRow()
        {
            var people = new List<Person>
            {
                new Person { Name = "Yeats" }, new Person { Name = "Blake" }, new Person { Name = "Keats" },
                new Person { Name = "Donne" }, new Person { Name = "Hardy" }
            };

            var grid = new DataGrid { Name = "grid", RowHeight = 20 };
            grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            grid.SetSource(people, new DataColumn { Header = "Name", Bind = (cell, item) => cell.Text = ((Person)item).Name });
            Host(grid);

            _surface.Focus(grid);
            grid.Select(3);
            Layout();

            Assert.IsTrue(_surface.KeyDown(Key.ContextMenu, KeyModifiers.None));
            Layout();

            VisualElement row = RowShowing(grid.Body, "Donne");

            Assert.IsTrue(_asked.FromKeyboard);
            Assert.AreSame(grid, _asked.Source);
            Assert.AreEqual(row.Y + row.ActualHeight / 2, _asked.Y, 0.01f, "the menu opens on Donne's row, not mid-grid");
            Assert.IsTrue(_menu.Open);
        }

        [TestMethod]
        public void FromTheKeyboardATreeAsksAtItsSelectedRow()
        {
            var roots = new List<Folder>();

            foreach (string name in new[] { "Library", "Playlists", "Radio", "Podcasts" })
            {
                roots.Add(new Folder { Name = name });
            }

            var tree = new TreeView { Name = "tree", ItemHeight = 20 };
            tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            tree.SetRoots(roots, item => (IList)((Folder)item).Children, () => new VisualElement(),
                (content, node) => content.Text = ((Folder)node.Item).Name);
            Host(tree);

            _surface.Focus(tree);
            tree.Select(2);
            Layout();

            Assert.IsTrue(_surface.KeyDown(Key.F10, KeyModifiers.Shift));

            VisualElement row = null;

            foreach (VisualElement realised in tree.Rows.RealisedRows)
            {
                if (Shows(realised, "Radio"))
                {
                    row = realised;
                }
            }

            Assert.AreEqual(row.Y + row.ActualHeight / 2, _asked.Y, 0.01f);
        }

        [TestMethod]
        public void ARightClickOnARowAsksWhereThePointerIs()
        {
            var people = new List<Person> { new Person { Name = "Yeats" }, new Person { Name = "Blake" } };

            var grid = new DataGrid { Name = "grid", RowHeight = 20 };
            grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            grid.SetSource(people, new DataColumn { Header = "Name", Bind = (cell, item) => cell.Text = ((Person)item).Name });
            Host(grid);

            VisualElement row = RowShowing(grid.Body, "Blake");
            float x = row.X + 10;
            float y = row.Y + 5;

            _surface.PointerDown(x, y, PointerButton.Right);
            _surface.PointerUp(x, y, PointerButton.Right);

            Assert.IsFalse(_asked.FromKeyboard);
            Assert.AreEqual(y, _asked.Y);
            Assert.AreSame(people[1], grid.SelectedItem, "the row under a right press is the one the menu is about");
        }

        private static bool Shows(VisualElement element, string text)
        {
            if (element.Text == text)
            {
                return true;
            }

            foreach (VisualElement child in element.ChildElements)
            {
                if (Shows(child, text))
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement RowShowing(VirtualList body, string text)
        {
            foreach (VisualElement realised in body.RealisedRows)
            {
                if (Shows(realised, text))
                {
                    return realised;
                }
            }

            return null;
        }
    }
}
