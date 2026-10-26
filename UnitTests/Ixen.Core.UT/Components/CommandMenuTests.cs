using Ixen.Core.Components;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class CommandMenuTests
    {
        private CommandMenuComponent _component;
        private IxenSurface _surface;
        private string _ran;

        [TestInitialize]
        public void Setup()
        {
            _component = new CommandMenuComponent();
            _component.Commands.Add(Command("Cut"));
            _component.Commands.Add(Command("Copy"));

            _surface = new IxenSurface(_component) { Styles = new StyleRegistry() };
            Layout();
        }

        private Command Command(string label) => new Command(() => _ran = label) { Label = label };

        private void Layout() => _surface.ComputeLayout(300, 300);

        private RoutedPanel Menu => (RoutedPanel)_component.View.FindByName("menu");

        private string Items() => string.Join(",", Menu.Inner.ChildElements.Select(e => e.Text));

        [TestMethod]
        public void TheItemsLandInTheRoutedPanelBetweenTheStaticOnes()
        {
            Assert.AreEqual("first,Cut,Copy,last", Items(), "each item takes its command's label");
            Assert.AreEqual(1, Menu.ChildElements.Count, "nothing lands beside the panel");
        }

        [TestMethod]
        public void AddingRemovingAndReorderingKeepThePlace()
        {
            _component.Update(() => _component.Commands.Add(Command("Paste")));
            Layout();
            Assert.AreEqual("first,Cut,Copy,Paste,last", Items());

            _component.Update(() => _component.Commands.RemoveAt(0));
            Layout();
            Assert.AreEqual("first,Copy,Paste,last", Items());

            _component.Update(() => _component.Commands.Reverse());
            Layout();
            Assert.AreEqual("first,Paste,Copy,last", Items(),
                "a keyed reorder splices inside the routed panel, not at the offsets of the outer element");

            _component.Update(() => _component.Commands.Clear());
            Layout();
            Assert.AreEqual("first,last", Items());
        }

        [TestMethod]
        public void EachItemRunsItsOwnCommand()
        {
            Menu.Inner.ChildElements.First(e => e.Text == "Copy").PerformClick();

            Assert.AreEqual("Copy", _ran);
        }
    }
}
