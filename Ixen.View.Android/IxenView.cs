using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.Accessibility;
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
        private TextField _editing;
        private IxenAccessibilityProvider _accessibility;

        private void Init()
        {
            Focusable = true;
            FocusableInTouchMode = true;

            _skCanvasView = new SKCanvasView(Context);

            _host = new IxenHost(new IxenSurface(), _skCanvasView.Invalidate,
                new AndroidScheduler(), new AndroidClipboard(Context), null,
                new AssetImageSource(Context?.Assets), _skCanvasView.PostInvalidate);

            IxenSynchronizationContext.Install(_host.Surface);

            _host.Surface.ReducedMotion = PrefersReducedMotion();

            _skCanvasView.PaintSurface += OnPaintSurface;
            _skCanvasView.Touch += OnTouch;

            AddView(_skCanvasView);
        }

        private bool PrefersReducedMotion()
        {
            ContentResolver resolver = Context?.ContentResolver;

            if (resolver == null)
            {
                return false;
            }

            try
            {
                return global::Android.Provider.Settings.Global.GetFloat(resolver,
                    global::Android.Provider.Settings.Global.AnimatorDurationScale, 1f) == 0f;
            }
            catch (Java.Lang.Exception)
            {
                return false;
            }
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

            int index = motion.ActionIndex;

            switch (motion.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    _host.PointerDown(motion.GetX(index), motion.GetY(index), PointerButton.Left,
                        PointerKind.Touch, motion.GetPointerId(index));
                    break;

                case MotionEventActions.Move:
                    for (int moved = 0; moved < motion.PointerCount; moved++)
                    {
                        _host.PointerMove(motion.GetX(moved), motion.GetY(moved), PointerKind.Touch,
                            motion.GetPointerId(moved));
                    }
                    break;

                case MotionEventActions.PointerUp:
                    _host.PointerUp(motion.GetX(index), motion.GetY(index), PointerButton.Left,
                        PointerKind.Touch, motion.GetPointerId(index));
                    break;

                case MotionEventActions.Up:
                    _host.PointerUp(motion.GetX(index), motion.GetY(index), PointerButton.Left,
                        PointerKind.Touch, motion.GetPointerId(index));
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

        public override AccessibilityNodeProvider AccessibilityNodeProvider
            => _accessibility ??= new IxenAccessibilityProvider(this, _host.Surface);

        public override bool OnCheckIsTextEditor() => true;

        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            if (outAttrs != null)
            {
                outAttrs.InputType = InputTypeFor(_host.FocusedElement as TextField);
                outAttrs.ImeOptions = ImeFlags.NoExtractUi;

                if (_host.FocusedElement is TextField field)
                {
                    outAttrs.InitialSelStart = field.SelectionStart;
                    outAttrs.InitialSelEnd = field.SelectionStart + field.SelectionLength;
                }
            }

            return new IxenInputConnection(this, _host);
        }

        private static InputTypes InputTypeFor(TextField field)
        {
            if (field == null)
            {
                return InputTypes.ClassText;
            }

            if (field.Password)
            {
                return InputTypes.ClassText
                    | InputTypes.TextVariationPassword
                    | InputTypes.TextFlagNoSuggestions;
            }

            return field.Multiline
                ? InputTypes.ClassText | InputTypes.TextFlagMultiLine
                : InputTypes.ClassText;
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
                    _host.KeyDown(AndroidKeys.ToKey(e.KeyCode), modifiers, e.RepeatCount > 0);

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
            var field = _host.FocusedElement as TextField;
            bool wanted = field != null;
            bool moved = field != _editing;

            if (wanted == _softKeyboardShown && !moved)
            {
                return;
            }

            _softKeyboardShown = wanted;
            _editing = field;

            if (!(Context?.GetSystemService(Context.InputMethodService) is InputMethodManager manager))
            {
                return;
            }

            if (moved && wanted)
            {
                manager.RestartInput(this);
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
