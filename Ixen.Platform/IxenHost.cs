using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using SkiaSharp;
using System;
using System.Text;

namespace Ixen.Platform
{
    public sealed class IxenHost
    {
        private readonly IxenSurface _surface;
        private readonly Action _requestRepaint;

        public IxenHost(IxenSurface surface, Action requestRepaint)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _requestRepaint = requestRepaint;
        }

        public IxenSurface Surface => _surface;

        public VisualElement Root
        {
            get => _surface.Root;
            set => _surface.Root = value;
        }

        public void Paint(SKCanvas canvas, int width, int height)
        {
            if (canvas == null || width <= 0 || height <= 0)
            {
                return;
            }

            _surface.ComputeLayout(width, height);
            _surface.Render(canvas);
        }

        public void PointerMove(float x, float y)
        {
            _surface.PointerMove(x, y);
            RepaintIfDirty();
        }

        public void PointerDown(float x, float y, PointerButton button)
        {
            _surface.PointerDown(x, y, button);
            RepaintIfDirty();
        }

        public void PointerUp(float x, float y, PointerButton button)
        {
            _surface.PointerUp(x, y, button);
            RepaintIfDirty();
        }

        public void PointerWheel(float x, float y, float deltaX, float deltaY)
        {
            _surface.PointerWheel(x, y, deltaX, deltaY);
            RepaintIfDirty();
        }

        public void PointerLeave()
        {
            _surface.PointerLeaveSurface();
            RepaintIfDirty();
        }

        public void PointerCaptureLost()
        {
            _surface.PointerCaptureLost();
            RepaintIfDirty();
        }

        public VisualElement FocusedElement => _surface.FocusedElement;

        public void Focus(VisualElement element)
        {
            _surface.Focus(element);
            RepaintIfDirty();
        }

        public void KeyDown(Key key, KeyModifiers modifiers)
        {
            _surface.KeyDown(key, modifiers);
            RepaintIfDirty();
        }

        public void KeyUp(Key key, KeyModifiers modifiers)
        {
            _surface.KeyUp(key, modifiers);
            RepaintIfDirty();
        }

        public void TextInput(string text)
        {
            string filtered = WithoutControlCharacters(text);

            if (string.IsNullOrEmpty(filtered))
            {
                return;
            }

            _surface.TextInput(filtered);
            RepaintIfDirty();
        }

        private static string WithoutControlCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            int kept = 0;

            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    kept++;
                }
            }

            if (kept == text.Length)
            {
                return text;
            }

            if (kept == 0)
            {
                return null;
            }

            var builder = new StringBuilder(kept);

            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private void RepaintIfDirty()
        {
            if (_requestRepaint != null && _surface.IsDirty)
            {
                _requestRepaint();
            }
        }
    }
}
