using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Platform;
using SkiaSharp.Views.Android;

namespace Ixen.View.Android
{
    public class IxenView : FrameLayout
    {
        private SKCanvasView _skCanvasView;
        private IxenHost _host;

        public IxenView(Context context)
            : base(context)
        {
            Init();
        }

        public IxenView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init();
        }

        public IxenView(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Init();
        }

        protected IxenView(nint javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

        private bool _softKeyboardShown;

        private void Init()
        {
            Focusable = true;
            FocusableInTouchMode = true;

            _skCanvasView = new SKCanvasView(Context);

            _host = new IxenHost(new IxenSurface(), _skCanvasView.Invalidate,
                new AndroidScheduler(), new AndroidClipboard(Context), null,
                new AssetImageSource(Context?.Assets));

            _skCanvasView.PaintSurface += OnPaintSurface;
            _skCanvasView.Touch += OnTouch;

            AddView(_skCanvasView);
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            float density = Context?.Resources?.DisplayMetrics?.Density ?? 1f;

            _host.Surface.Scale = density > 0 ? density : 1f;
            _host.Paint(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        }

        private void OnTouch(object sender, TouchEventArgs e)
        {
            MotionEvent motion = e.Event;

            if (motion == null)
            {
                return;
            }

            float x = motion.GetX();
            float y = motion.GetY();

            switch (motion.Action)
            {
                case MotionEventActions.Down:
                    _host.PointerDown(x, y, PointerButton.Left);
                    break;

                case MotionEventActions.Move:
                    _host.PointerMove(x, y);
                    break;

                case MotionEventActions.Up:
                    _host.PointerUp(x, y, PointerButton.Left);
                    _host.PointerLeave();
                    SyncSoftKeyboard();
                    break;

                case MotionEventActions.Cancel:
                    _host.PointerCaptureLost();
                    break;

                default:
                    return;
            }

            e.Handled = true;
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            if (e == null || e.Action != MotionEventActions.Scroll)
            {
                return base.OnGenericMotionEvent(e);
            }

            float deltaX = e.GetAxisValue(Axis.Hscroll);
            float deltaY = e.GetAxisValue(Axis.Vscroll);

            if (deltaX == 0 && deltaY == 0)
            {
                return base.OnGenericMotionEvent(e);
            }

            _host.PointerWheel(e.GetX(), e.GetY(), deltaX, deltaY,
                AndroidKeys.ToModifiers(e.MetaState));

            return true;
        }

        public override bool OnCheckIsTextEditor() => true;

        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            if (outAttrs != null)
            {
                outAttrs.InputType = InputTypes.ClassText | InputTypes.TextFlagNoSuggestions;
                outAttrs.ImeOptions = ImeFlags.NoExtractUi;
            }

            return new BaseInputConnection(this, false);
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e == null || AndroidKeys.IsSystemKey(e.KeyCode))
            {
                return base.DispatchKeyEvent(e);
            }

            KeyModifiers modifiers = AndroidKeys.ToModifiers(e.MetaState);

            switch (e.Action)
            {
                case KeyEventActions.Down:
                    _host.KeyDown(AndroidKeys.ToKey(e.KeyCode), modifiers);

                    int unicode = e.UnicodeChar;

                    if (unicode != 0)
                    {
                        _host.TextInput(((char)unicode).ToString());
                    }

                    return true;

                case KeyEventActions.Up:
                    _host.KeyUp(AndroidKeys.ToKey(e.KeyCode), modifiers);
                    return true;
            }

            return base.DispatchKeyEvent(e);
        }

        private void SyncSoftKeyboard()
        {
            bool wanted = _host.FocusedElement is TextField;

            if (wanted == _softKeyboardShown)
            {
                return;
            }

            _softKeyboardShown = wanted;

            if (!(Context?.GetSystemService(Context.InputMethodService) is InputMethodManager manager))
            {
                return;
            }

            if (wanted)
            {
                RequestFocus();
                manager.ShowSoftInput(this, ShowFlags.Implicit);
                return;
            }

            manager.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);
        }

        public VisualElement Root
        {
            get => _host.Root;
            set => _host.Root = value;
        }

        public Component RootComponent
        {
            set => _host.Root = value?.Initialize();
        }
    }
}
