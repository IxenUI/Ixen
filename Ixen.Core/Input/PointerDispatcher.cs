using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Input
{
    internal class PointerDispatcher
    {
        private readonly List<VisualElement> _leftChain = new();
        private readonly List<VisualElement> _enteredChain = new();

        private VisualElement _hovered;
        private VisualElement _pressed;

        internal VisualElement Hovered => _hovered;
        internal VisualElement Pressed => _pressed;

        internal void Move(VisualElement root, float x, float y)
        {
            VisualElement hit = HitTester.HitTest(root, x, y);

            UpdateHover(hit, x, y);
            Bubble(hit, new PointerEventArgs(x, y, PointerButton.None, hit), PointerEventKind.Move);
        }

        internal void LeaveSurface()
        {
            UpdateHover(null, 0, 0);
            _pressed = null;
        }

        internal void Down(VisualElement root, float x, float y, PointerButton button)
        {
            VisualElement hit = HitTester.HitTest(root, x, y);

            UpdateHover(hit, x, y);

            _pressed = hit;

            Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Down);
        }

        internal void Up(VisualElement root, float x, float y, PointerButton button)
        {
            VisualElement hit = HitTester.HitTest(root, x, y);

            UpdateHover(hit, x, y);
            Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Up);

            bool isClick = hit != null && hit == _pressed;

            _pressed = null;

            if (isClick)
            {
                Bubble(hit, new PointerEventArgs(x, y, button, hit), PointerEventKind.Click);
            }
        }

        private static void Bubble(VisualElement hit, PointerEventArgs args, PointerEventKind kind)
        {
            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                switch (kind)
                {
                    case PointerEventKind.Down:
                        element.RaisePointerDown(args);
                        break;

                    case PointerEventKind.Up:
                        element.RaisePointerUp(args);
                        break;

                    case PointerEventKind.Move:
                        element.RaisePointerMove(args);
                        break;

                    case PointerEventKind.Click:
                        element.RaisePointerClick(args);
                        break;
                }

                if (args.Handled)
                {
                    return;
                }
            }
        }

        private void UpdateHover(VisualElement hit, float x, float y)
        {
            if (hit == _hovered)
            {
                return;
            }

            VisualElement left = _hovered;

            _leftChain.Clear();
            _enteredChain.Clear();

            for (VisualElement element = left; element != null; element = element.Parent)
            {
                _leftChain.Add(element);
            }

            for (VisualElement element = hit; element != null; element = element.Parent)
            {
                _enteredChain.Add(element);
            }

            int shared = 0;

            while (shared < _leftChain.Count
                && shared < _enteredChain.Count
                && _leftChain[_leftChain.Count - 1 - shared] == _enteredChain[_enteredChain.Count - 1 - shared])
            {
                shared++;
            }

            _hovered = hit;

            if (_leftChain.Count > shared)
            {
                var leaveArgs = new PointerEventArgs(x, y, PointerButton.None, left);

                for (int i = 0; i < _leftChain.Count - shared; i++)
                {
                    _leftChain[i].RaisePointerLeave(leaveArgs);
                }
            }

            if (_enteredChain.Count > shared)
            {
                var enterArgs = new PointerEventArgs(x, y, PointerButton.None, hit);

                for (int i = _enteredChain.Count - shared - 1; i >= 0; i--)
                {
                    _enteredChain[i].RaisePointerEnter(enterArgs);
                }
            }
        }
    }
}
