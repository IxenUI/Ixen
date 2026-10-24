using Android.Content;
using Android.Database;
using Android.OS;
using System;

namespace Ixen.View.Android
{
    internal sealed class PreferenceObserver : ContentObserver
    {
        private readonly Action _changed;
        private ContentResolver _resolver;

        private PreferenceObserver(Action changed)
            : base(new Handler(Looper.MainLooper))
        {
            _changed = changed;
        }

        internal static PreferenceObserver Register(Context context, Action changed,
            params global::Android.Net.Uri[] settings)
        {
            ContentResolver resolver = context?.ContentResolver;

            if (resolver == null)
            {
                return null;
            }

            var observer = new PreferenceObserver(changed) { _resolver = resolver };

            foreach (global::Android.Net.Uri uri in settings)
            {
                if (uri != null)
                {
                    resolver.RegisterContentObserver(uri, false, observer);
                }
            }

            return observer;
        }

        internal void Release()
        {
            _resolver?.UnregisterContentObserver(this);
            _resolver = null;
        }

        public override void OnChange(bool selfChange)
        {
            _changed?.Invoke();
        }
    }
}
