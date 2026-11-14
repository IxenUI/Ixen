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
    public class TypeAheadTests
    {
        private const int VIEWPORT = 400;

        private static readonly string[] ALBUMS =
        {
            "Abbey Road", "Back in Black", "Bad", "Beatles 1", "Blue", "Bob", "Thriller", "the Wall"
        };

        private VisualElement _root;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        private void Host(VisualElement control)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.AddChild(control);
            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };
            Layout();
        }

        private void Layout()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        private void Type(string text)
        {
            foreach (char c in text)
            {
                _surface.TextInput(c.ToString());
                Layout();
            }
        }

        private VirtualList List()
        {
            var list = new VirtualList { Name = "albums", ItemHeight = 20, SearchText = index => ALBUMS[index] };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.SetItems(ALBUMS, () => new VisualElement { Focusable = true }, (cell, index) => cell.Text = ALBUMS[index]);
            Host(list);
            return list;
        }

        private static int Focused(IxenSurface surface, VirtualList list)
        {
            VisualElement focused = surface.FocusedElement;

            return focused != null && focused.Parent == list ? list.IndexOfRow(focused) : -1;
        }

        [TestMethod]
        public void TypingAWordJumpsToTheFirstEntryThatStartsWithIt()
        {
            VirtualList list = List();
            list.MoveFocusTo(0);
            Layout();

            Type("bl");

            Assert.AreEqual(4, Focused(_surface, list), "\"bl\" is Blue, not Back in Black");
        }

        [TestMethod]
        public void TheSameLetterAgainCyclesThroughItsEntries()
        {
            VirtualList list = List();
            list.MoveFocusTo(0);
            Layout();

            Type("b");
            Assert.AreEqual(1, Focused(_surface, list));

            Type("b");
            Assert.AreEqual(2, Focused(_surface, list), "a second b goes to the next entry starting with b");

            Type("b");
            Assert.AreEqual(3, Focused(_surface, list));
        }

        [TestMethod]
        public void APauseStartsANewSearch()
        {
            VirtualList list = List();
            list.MoveFocusTo(0);
            Layout();

            Type("th");
            Assert.AreEqual(6, Focused(_surface, list));

            _scheduler.Tick();
            Type("ab");

            Assert.AreEqual(0, Focused(_surface, list), "after the pause, \"ab\" is a new word, not \"thab\"");
        }

        [TestMethod]
        public void CaseIsIgnoredAndTheCurrentEntryStaysWhileItStillMatches()
        {
            VirtualList list = List();
            list.MoveFocusTo(0);
            Layout();

            Type("T");
            Assert.AreEqual(6, Focused(_surface, list));

            Type("he w");
            Assert.AreEqual(7, Focused(_surface, list), "a space inside the search is part of it: \"the w\" reaches the Wall");
        }

        [TestMethod]
        public void ASpaceInsideASearchDoesNotPressTheRow()
        {
            int pressed = 0;
            var list = new VirtualList { Name = "albums", ItemHeight = 20, SearchText = index => ALBUMS[index] };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.SetItems(ALBUMS, () =>
            {
                var row = new VisualElement();
                row.PointerClick += (sender, e) => pressed++;
                return row;
            }, (cell, index) => cell.Text = ALBUMS[index]);
            Host(list);
            list.MoveFocusTo(0);
            Layout();

            Type("the");
            _surface.KeyDown(Key.Space, KeyModifiers.None);
            Type(" w");

            Assert.AreEqual(0, pressed, "the space continues the search");
            Assert.AreEqual(7, Focused(_surface, list));

            _scheduler.Tick();
            _surface.KeyDown(Key.Space, KeyModifiers.None);

            Assert.AreEqual(1, pressed, "after the pause, Space presses the focused row again");
        }

        [TestMethod]
        public void NoMatchMovesNothing()
        {
            VirtualList list = List();
            list.MoveFocusTo(2);
            Layout();

            Type("z");

            Assert.AreEqual(2, Focused(_surface, list));
        }

        [TestMethod]
        public void ATreeSelectsByTypedText()
        {
            var roots = new List<Folder>();

            foreach (string album in ALBUMS)
            {
                roots.Add(new Folder { Name = album });
            }

            var tree = new TreeView { Name = "tree", ItemHeight = 20, SearchText = item => ((Folder)item).Name };
            tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            tree.SetRoots(roots, item => (IList)((Folder)item).Children, () => new VisualElement(),
                (content, node) => content.Text = ((Folder)node.Item).Name);
            Host(tree);

            _surface.Focus(tree);
            tree.Select(0);
            Layout();

            Type("bea");

            Assert.AreEqual(3, tree.SelectedIndex);
        }

        [TestMethod]
        public void AGridSearchesItsFirstColumnByDefault()
        {
            var people = new List<Person>
            {
                new Person { Name = "Yeats", Age = 73 },
                new Person { Name = "Blake", Age = 69 },
                new Person { Name = "Keats", Age = 25 },
                new Person { Name = "Kipling", Age = 70 }
            };

            var grid = new DataGrid { Name = "grid", RowHeight = 20 };
            grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            grid.SetSource(people, new DataColumn
            {
                Header = "Name",
                Bind = (cell, item) => cell.Text = ((Person)item).Name,
                Value = item => ((Person)item).Name
            });
            Host(grid);

            _surface.Focus(grid);
            grid.Select(0);
            Layout();

            Type("ki");

            Assert.AreSame(people[3], grid.SelectedItem, "\"ki\" passes Keats for Kipling");
        }
    }
}
