using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using SkiaSharp;
using System;

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

        private void RepaintIfDirty()
        {
            if (_requestRepaint != null && _surface.IsDirty)
            {
                _requestRepaint();
            }
        }
    }
}
