using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Ixen.Platform
{
    public sealed class IxenHost
    {
        private readonly IxenSurface _surface;
        private readonly Action _requestRepaint;

        public IxenHost(IxenSurface surface, Action requestRepaint, IScheduler scheduler = null,
            IClipboard clipboard = null, Action<CursorKind> setCursor = null, IImageSource images = null)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _requestRepaint = requestRepaint;

            if (setCursor != null)
            {
                _surface.CursorSetter = setCursor;
            }

            if (scheduler != null)
            {
                _surface.Scheduler = new HostScheduler(this, scheduler);
            }

            if (clipboard != null)
            {
                _surface.Clipboard = clipboard;
            }

            if (images != null)
            {
                _surface.ImageSource = images;
            }
        }

        private sealed class HostScheduler : IScheduler
        {
            private readonly IxenHost _host;
            private readonly IScheduler _inner;

            internal HostScheduler(IxenHost host, IScheduler inner)
            {
                _host = host;
                _inner = inner;
            }

            public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
                => _inner.Schedule(delayMilliseconds, repeat, () =>
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception error)
                    {
                        _host.Fail(IxenErrorPhase.Timer, error);
                    }
                    finally
                    {
                        _host.RepaintIfDirty();
                    }
                });
        }

        public event EventHandler<IxenErrorEventArgs> UnhandledError;

        private bool Report(IxenErrorPhase phase, Exception error)
        {
            EventHandler<IxenErrorEventArgs> handler = UnhandledError;

            if (handler == null)
            {
                return false;
            }

            var args = new IxenErrorEventArgs(phase, error);

            handler(this, args);

            return args.Handled;
        }

        private void Fail(IxenErrorPhase phase, Exception error)
        {
            if (!Report(phase, error))
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
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

            try
            {
                _surface.ComputeLayout(width, height);
                _surface.Render(canvas);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Frame, error);
            }
        }

        public void PointerMove(float x, float y, PointerKind kind = PointerKind.Mouse)
        {
            try
            {
                _surface.PointerMove(x, y, kind);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void PointerDown(float x, float y, PointerButton button,
            PointerKind kind = PointerKind.Mouse)
        {
            try
            {
                _surface.PointerDown(x, y, button, kind);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void PointerUp(float x, float y, PointerButton button,
            PointerKind kind = PointerKind.Mouse)
        {
            try
            {
                _surface.PointerUp(x, y, button, kind);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void PointerWheel(float x, float y, float deltaX, float deltaY,
            KeyModifiers modifiers = KeyModifiers.None)
        {
            try
            {
                _surface.PointerWheel(x, y, deltaX, deltaY, modifiers);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void PointerLeave()
        {
            try
            {
                _surface.PointerLeaveSurface();
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void PointerCaptureLost()
        {
            try
            {
                _surface.PointerCaptureLost();
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Pointer, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public VisualElement FocusedElement => _surface.FocusedElement;

        public void Focus(VisualElement element)
        {
            try
            {
                _surface.Focus(element);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void KeyDown(Key key, KeyModifiers modifiers, bool? isRepeat = null)
        {
            try
            {
                _surface.KeyDown(key, modifiers, isRepeat);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void KeyUp(Key key, KeyModifiers modifiers)
        {
            try
            {
                _surface.KeyUp(key, modifiers);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void Composition(string text, int caret)
        {
            try
            {
                _surface.Composition(text, caret);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void CommitComposition(string text)
        {
            try
            {
                _surface.CommitComposition(WithoutControlCharacters(text));
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void CancelComposition()
        {
            try
            {
                _surface.CancelComposition();
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
        }

        public void TextInput(string text)
        {
            string filtered = WithoutControlCharacters(text);

            if (string.IsNullOrEmpty(filtered))
            {
                return;
            }

            try
            {
                _surface.TextInput(filtered);
            }
            catch (Exception error)
            {
                Fail(IxenErrorPhase.Keyboard, error);
            }
            finally
            {
                RepaintIfDirty();
            }
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
