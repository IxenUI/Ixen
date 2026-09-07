using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class SiblingCombinatorTests
    {
        private const int VIEWPORT = 200;
        private const string MARK = "#222222";

        private IxenSurface _surface;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static VisualElement Box(string name, params string[] classes)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (string c in classes)
            {
                box.Classes.Add(c);
            }

            return box;
        }

        private void Apply(string xns, VisualElement root)
        {
            var registry = new StyleRegistry();
            registry.Add(Compile(xns));

            _surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        [TestMethod]
        public void EveryRowButTheFirstOne()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement one = Box("row");
            VisualElement two = Box("row");
            VisualElement three = Box("row");

            card.AddChildren(one, two, three);
            root.AddChild(card);

            Apply("row {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(one), "nothing precedes the first row");
            Assert.AreEqual(MARK, BackgroundOf(two));
            Assert.AreEqual(MARK, BackgroundOf(three),
                "that is the separator-between-items rule, which neither gap nor last-child can "
                + "express: a line between rows and none after the last");
        }

        [TestMethod]
        public void ASiblingIsNotADescendant()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement row = Box("row");

            card.AddChild(row);
            root.AddChild(card);

            Apply("card {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row),
                "a nested selector that opens with a combinator is relative to the selector it "
                + "is nested in, exactly as the child marker already is - so this asks for a row "
                + "that FOLLOWS a card, and a row inside one is not that");
        }

        [TestMethod]
        public void AdjacentIsExactlyOneHop()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement first = Box("row");
            VisualElement second = Box("row");

            card.AddChildren(head, first, second);
            root.AddChild(card);

            Apply("head {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(first));
            Assert.IsNull(BackgroundOf(second), "the second row does not follow the head");
        }

        [TestMethod]
        public void AGeneralSiblingReachesEveryLaterOne()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement first = Box("row");
            VisualElement second = Box("row");

            card.AddChildren(head, first, second);
            root.AddChild(card);

            Apply("head {\r\n    ~ row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(first));
            Assert.AreEqual(MARK, BackgroundOf(second),
                "the general form walks back over every earlier sibling rather than one");
        }

        [TestMethod]
        public void SomethingInBetweenBreaksTheAdjacentHopAndNotTheGeneralOne()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement spacer = Box("spacer");
            VisualElement row = Box("row");

            card.AddChildren(head, spacer, row);
            root.AddChild(card);

            Apply("head {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row), "the spacer sits between them");

            Apply("head {\r\n    ~ row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(row));
        }

        [TestMethod]
        public void ASiblingOnlyLooksBackwards()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement row = Box("row");
            VisualElement head = Box("head");

            card.AddChildren(row, head);
            root.AddChild(card);

            Apply("head {\r\n    ~ row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row),
                "the row comes before the head, and there is no combinator in CSS or here that "
                + "reaches an earlier sibling from a later one");
        }

        [TestMethod]
        public void ItComposesWithAnAncestorAbove()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement head = Box("head");
            VisualElement row = Box("row");

            page.AddChildren(head, row);
            root.AddChild(page);

            Apply("page {\r\n    head {\r\n        + row { background: " + MARK + " }\r\n    }\r\n}",
                root);

            Assert.AreEqual(MARK, BackgroundOf(row),
                "the cursor steps sideways for the marked hop and then upwards for the rest, so "
                + "the two kinds of segment chain in one scope");
        }

        [TestMethod]
        public void TheAncestorAboveIsStillADescendantMatch()
        {
            VisualElement root = Box("root");
            VisualElement page = Box("page");
            VisualElement filler = Box("filler");
            VisualElement head = Box("head");
            VisualElement row = Box("row");

            filler.AddChildren(head, row);
            page.AddChild(filler);
            root.AddChild(page);

            Apply("page {\r\n    head {\r\n        + row { background: " + MARK + " }\r\n    }\r\n}",
                root);

            Assert.AreEqual(MARK, BackgroundOf(row),
                "the marker binds one hop and leaves everything above it loose, which is the rule "
                + "the child combinator already follows");
        }

        [TestMethod]
        public void ItWorksOnAClassAndOnAType()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head", "title");
            VisualElement row = Box("row");

            card.AddChildren(head, row);
            root.AddChild(card);

            Apply(".title {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(row));

            Apply("head {\r\n    + .plain { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row), "the row is not plain");

            row.AddClass("plain");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(MARK, BackgroundOf(row),
                "the relation is about the hop, not about what kind of selector sits on either "
                + "side of it");
        }

        [TestMethod]
        public void AStateOnTheSiblingIsHonoured()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement row = Box("row");

            card.AddChildren(head, row);
            root.AddChild(card);

            Apply("head:hover {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row));

            head.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(MARK, BackgroundOf(row),
                "which is the one thing a state cannot do on its own: react to the state of "
                + "something that is not an ancestor");
        }

        [TestMethod]
        public void AClassOnTheSiblingIsHonouredToo()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement row = Box("row");

            card.AddChildren(head, row);
            root.AddChild(card);

            Apply(".open {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row));

            head.AddClass("open");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(MARK, BackgroundOf(row),
                "AddClass marks the element and its subtree, and a later sibling is in neither - "
                + "so the style pass has to carry the dirtiness sideways as well as downwards");
        }

        [TestMethod]
        public void ASiblingScopeReachesInsideTheRowItStyles()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement row = Box("row");
            VisualElement label = Box("label");

            row.AddChild(label);
            card.AddChildren(head, row);
            root.AddChild(card);

            Apply("head:hover {\r\n    + row {\r\n        label { background: " + MARK
                + " }\r\n    }\r\n}", root);

            Assert.IsNull(BackgroundOf(label));

            head.AddState("hover");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(MARK, BackgroundOf(label),
                "the scope is +head/row, so what the state changes is the resolved style of the "
                + "label rather than of the row - the whole subtree of a later sibling has to be "
                + "carried, not just the sibling");
        }

        [TestMethod]
        public void InsertingARowRestylesWhatFollowsIt()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement head = Box("head");
            VisualElement row = Box("row");

            card.AddChildren(head, row);
            root.AddChild(card);

            Apply("head {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(row));

            card.InsertChild(1, Box("spacer"));
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(BackgroundOf(row),
                "the spacer now sits between them, and the element that lost the rule is not the "
                + "one that was added - so a splice has to mark every child, which is the same "
                + "answer the structural pseudo-classes already needed");
        }

        [TestMethod]
        public void AKeyedReorderRestylesTheNeighbours()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement wide = Box("row", "wide");
            VisualElement first = Box("row");
            VisualElement second = Box("row");

            card.AddChildren(wide, first, second);
            root.AddChild(card);

            Apply(".wide {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.AreEqual(MARK, BackgroundOf(first));
            Assert.IsNull(BackgroundOf(second));

            card.SpliceChildren(0, 3, new List<VisualElement> { first, wide, second });
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(BackgroundOf(first),
                "a keyed reorder is a list splice: nothing is detached, nothing is added, and so "
                + "nothing is marked for a restyle - which is the one shape the follow pass alone "
                + "cannot see, since it looks for a child that is already dirty");

            Assert.AreEqual(MARK, BackgroundOf(second),
                "and the wide row now precedes this one instead");
        }

        [TestMethod]
        public void AStructuralPseudoClassWorksOnTheSiblingToo()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement one = Box("row");
            VisualElement two = Box("row");
            VisualElement three = Box("row");

            card.AddChildren(one, two, three);
            root.AddChild(card);

            Apply("row:first-child {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(one));
            Assert.AreEqual(MARK, BackgroundOf(two), "the second row follows the first one");
            Assert.IsNull(BackgroundOf(three), "and the third follows the second");
        }

        [TestMethod]
        public void ANegationWorksOnTheSiblingToo()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement wide = Box("row", "wide");
            VisualElement plain = Box("row");
            VisualElement last = Box("row");

            card.AddChildren(wide, plain, last);
            root.AddChild(card);

            Apply("row:not(.wide) {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(wide));
            Assert.IsNull(BackgroundOf(plain), "the row before it is wide");
            Assert.AreEqual(MARK, BackgroundOf(last));
        }

        [TestMethod]
        public void ASpaceAfterTheMarkerIsAllowed()
        {
            var spaced = new XnsSource("head {\r\n    +    row { background: " + MARK + " }\r\n}");
            var tight = new XnsSource("head {\r\n    +row { background: " + MARK + " }\r\n}");
            var general = new XnsSource("head {\r\n    ~ row { background: " + MARK + " }\r\n}");

            spaced.Compile();
            tight.Compile();
            general.Compile();

            Assert.IsFalse(spaced.HasErrors);
            Assert.IsFalse(tight.HasErrors);
            Assert.IsFalse(general.HasErrors);
        }

        [TestMethod]
        public void AMarkerAtTopLevelIsASyntaxError()
        {
            var adjacent = new XnsSource("+ row { background: " + MARK + " }");
            var general = new XnsSource("~ row { background: " + MARK + " }");

            adjacent.Compile();
            general.Compile();

            Assert.IsTrue(adjacent.HasErrors,
                "there is nothing above a top-level selector for the hop to be measured from");
            Assert.IsTrue(general.HasErrors);
        }

        [TestMethod]
        public void AFlatFormIsStillNotALanguage()
        {
            var source = new XnsSource("row + row { background: " + MARK + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "XNS has no flat descendant form either, so a combinator is only ever the first "
                + "thing in a nested selector");
        }

        [TestMethod]
        public void TheNameLosesTheMarkerAndTheScopeKeepsIt()
        {
            StyleClass adjacent = Compile("head {\r\n    + row { background: " + MARK + " }\r\n}")
                .Classes.Single();

            Assert.AreEqual("row", adjacent.Name,
                "the marker is a property of the scope, not part of the name anything looks up");

            Assert.AreEqual("+head", adjacent.Scope,
                "it moves to the segment above, which is the one the hop is measured from");

            StyleClass general = Compile("head {\r\n    ~ row { background: " + MARK + " }\r\n}")
                .Classes.Single();

            Assert.AreEqual("~head", general.Scope);
        }

        [TestMethod]
        public void TheMarkerBelongsToItsOwnEntryInAList()
        {
            ClassesSet set = Compile("head {\r\n    + row, chip { background: " + MARK + " }\r\n}");

            StyleClass row = set.Classes.Single(c => c.Name == "row");
            StyleClass chip = set.Classes.Single(c => c.Name == "chip");

            Assert.AreEqual("+head", row.Scope);
            Assert.AreEqual("head", chip.Scope,
                "the marker was written on one entry of the list, so it stays there");
        }

        [TestMethod]
        public void AScrollbarIsNotASibling()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement row = Box("row");

            card.Scrollable = true;
            card.AddChild(row);
            root.AddChild(card);

            Apply("#Scrollbar {\r\n    + row { background: " + MARK + " }\r\n}", root);

            Assert.IsNull(BackgroundOf(row),
                "chrome lives in a second list the layout ignores, so it has no position among "
                + "the children and cannot be anybody sibling - the same rule nth-child already "
                + "follows");
        }

        [TestMethod]
        public void ASiblingRuleBeatsThePlainRuleForTheSameName()
        {
            VisualElement root = Box("root");
            VisualElement card = Box("card");
            VisualElement one = Box("row");
            VisualElement two = Box("row");

            card.AddChildren(one, two);
            root.AddChild(card);

            Apply("row { background: #111111 }\r\nrow {\r\n    + row { background: " + MARK
                + " }\r\n}", root);

            Assert.AreEqual("#111111", BackgroundOf(one));
            Assert.AreEqual(MARK, BackgroundOf(two),
                "a scoped rule is applied after the unscoped one of the same family, so the "
                + "sibling form narrows what the plain rule set");
        }

        [TestMethod]
        public void TheGateIsOffUntilASiblingRuleIsRegistered()
        {
            var child = new StyleRegistry();
            child.Add(Compile("card {\r\n    > row { background: " + MARK + " }\r\n}"));

            Assert.IsFalse(child.HasSiblingClasses,
                "a sheet with no sibling rule pays one bool test per element and no follow pass");

            var stepped = new StyleRegistry();
            stepped.Add(Compile("row:nth-child(2n+1) {\r\n    label { background: " + MARK
                + " }\r\n}"));

            Assert.IsFalse(stepped.HasSiblingClasses,
                "and a plus inside an nth-child argument is not a combinator: the marker is only "
                + "ever the first character of a segment, so the gate reads the position and not "
                + "just the character");

            var sibling = new StyleRegistry();
            sibling.Add(Compile("head {\r\n    ~ row { background: " + MARK + " }\r\n}"));

            Assert.IsTrue(sibling.HasSiblingClasses);
        }

        [TestMethod]
        public void AContainerCannotStyleItsOwnSibling()
        {
            var source = new XnsSource("card {\r\n    @container (max-width: 200px) {\r\n"
                + "        + row { background: " + MARK + " }\r\n    }\r\n}");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "a container query asks about the element the block is nested inside, so that "
                + "element has to contain what the rule styles - a sibling of the card is not "
                + "inside it, and answering about the card anyway under this syntax would be the "
                + "quiet kind of divergence this language refuses");
        }

        [TestMethod]
        public void ButASiblingDeeperInsideAContainerIsFine()
        {
            var source = new XnsSource("card {\r\n    @container (max-width: 200px) {\r\n"
                + "        list {\r\n            + row { background: " + MARK + " }\r\n"
                + "        }\r\n    }\r\n}");

            source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }
    }
}
