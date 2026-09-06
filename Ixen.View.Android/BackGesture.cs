using Android.App;
using Android.Window;
using Ixen.Core.Input;
using Java.Lang;

namespace Ixen.View.Android
{
    internal class BackGesture : Object, IOnBackInvokedCallback
    {
        private const int PRIORITY = 0;

        private readonly IxenView _view;
        private readonly Activity _activity;

        private BackGesture(IxenView view, Activity activity)
        {
            _view = view;
            _activity = activity;
        }

        internal static BackGesture Register(IxenView view)
        {
            if (!System.OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return null;
            }

            if (!(view.Context is Activity activity) || activity.OnBackInvokedDispatcher == null)
            {
                return null;
            }

            var gesture = new BackGesture(view, activity);

            activity.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(PRIORITY, gesture);

            return gesture;
        }

        internal void Release()
        {
            if (System.OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                _activity.OnBackInvokedDispatcher?.UnregisterOnBackInvokedCallback(this);
            }
        }

        public void OnBackInvoked()
        {
            if (_view.Offer(Key.Back))
            {
                return;
            }

            _activity.Finish();
        }
    }
}
