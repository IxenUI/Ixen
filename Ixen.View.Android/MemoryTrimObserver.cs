using Android.Content;
using Android.Content.Res;
using System;
using System.Runtime.InteropServices;

namespace Ixen.View.Android
{
    internal sealed class MemoryTrimObserver : Java.Lang.Object, IComponentCallbacks2
    {
        private readonly Action _release;
        private Context _context;

        private MemoryTrimObserver(Action release)
        {
            _release = release;
        }

        internal static MemoryTrimObserver Register(Context context, Action release)
        {
            Context application = context?.ApplicationContext ?? context;

            if (application == null)
            {
                return null;
            }

            var observer = new MemoryTrimObserver(release) { _context = application };

            application.RegisterComponentCallbacks(observer);

            return observer;
        }

        internal void Release()
        {
            _context?.UnregisterComponentCallbacks(this);
            _context = null;
        }

        public void OnTrimMemory(TrimMemory level)
        {
            if (level >= TrimMemory.RunningCritical)
            {
                ReleaseAndPurge();
            }
        }

        public void OnLowMemory()
        {
            ReleaseAndPurge();
        }

        private void ReleaseAndPurge()
        {
            _release?.Invoke();

            if (System.OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                Mallopt(M_PURGE, 0);
            }
        }

        private const int M_PURGE = -101;

        [DllImport("libc", EntryPoint = "mallopt")]
        private static extern int Mallopt(int param, int value);

        public void OnConfigurationChanged(Configuration newConfig)
        {
        }
    }
}
