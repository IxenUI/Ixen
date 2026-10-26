using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class RowCommandTests
    {
        private RowCommandComponent _component;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _component = new RowCommandComponent();
            _component.Rows.Add(new ListItem { Id = 1, Name = "apples", Count = 2 });
            _component.Rows.Add(new ListItem { Id = 2, Name = "pears", Count = 0 });
            _component.Rows.Add(new ListItem { Id = 3, Name = "plums", Count = 5 });

            _surface = new IxenSurface(_component) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(300, 300);
        }

        private VisualElement[] Buttons()
            => _component.View.FindByName("rows_root").ChildElements.Where(e => e.Name == "row_remove").ToArray();

        private VisualElement Button(string name) => Buttons().First(e => e.Text == name);

        [TestMethod]
        public void EachRowRemovesItself()
        {
            Button("plums").PerformClick();
            _surface.ComputeLayout(300, 300);

            CollectionAssert.AreEqual(new[] { "apples", "pears" }, Buttons().Select(e => e.Text).ToArray(),
                "command-parameter binds the row, so the one command knows which row it is acting on");
        }

        [TestMethod]
        public void ARowThatCannotGoIsDisabled()
        {
            Assert.IsFalse(Button("pears").IsEnabled, "the command answers no for a row with nothing left");
            Assert.IsTrue(Button("apples").IsEnabled);

            Button("pears").PerformClick();
            _surface.ComputeLayout(300, 300);

            Assert.AreEqual(3, Buttons().Length);
        }

        [TestMethod]
        public void ALiteralParameterIsText()
        {
            _component.View.FindByName("row_literal").PerformClick();

            Assert.AreEqual("pears", _component.Echoed, "a value written in XNL is a string, as everywhere else");
        }

        [TestMethod]
        public void SetStateAsksAgainWithNothingElseToCall()
        {
            Assert.IsTrue(Button("apples").IsEnabled);

            _component.Update(() => _component.Rows[0].Count = 0);
            _surface.ComputeLayout(300, 300);

            Assert.IsFalse(Button("apples").IsEnabled,
                "the row is the same object, but the binding replays and the command is asked again");
        }

        [TestMethod]
        public void TheParameterFollowsARowAfterAReorder()
        {
            _component.Update(() =>
            {
                _component.Rows.Reverse();
                _component.Rows.RemoveAt(2);
            });
            _surface.ComputeLayout(300, 300);

            Button("plums").PerformClick();
            _surface.ComputeLayout(300, 300);

            CollectionAssert.AreEqual(new[] { "pears" }, Buttons().Select(e => e.Text).ToArray());
        }
    }
}
