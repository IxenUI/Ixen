using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Ixen.Core
{
    internal static class SystemNetwork
    {
        private static readonly List<WeakReference<Connectivity>> _watchers = new List<WeakReference<Connectivity>>();
        private static bool _subscribed;

        internal static bool IsAvailable() => NetworkInterface.GetIsNetworkAvailable();

        internal static void Watch(Connectivity connectivity)
        {
            lock (_watchers)
            {
                _watchers.Add(new WeakReference<Connectivity>(connectivity));

                if (_subscribed)
                {
                    return;
                }

                _subscribed = true;

                try
                {
                    NetworkChange.NetworkAddressChanged += OnChanged;
                    NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;
                }
                catch (Exception)
                {
                }
            }
        }

        private static void OnAvailabilityChanged(object sender, NetworkAvailabilityEventArgs args) => Notify();

        private static void OnChanged(object sender, EventArgs args) => Notify();

        private static void Notify()
        {
            var alive = new List<Connectivity>();

            lock (_watchers)
            {
                for (int index = _watchers.Count - 1; index >= 0; index--)
                {
                    if (_watchers[index].TryGetTarget(out Connectivity connectivity))
                    {
                        alive.Add(connectivity);
                    }
                    else
                    {
                        _watchers.RemoveAt(index);
                    }
                }
            }

            foreach (Connectivity connectivity in alive)
            {
                connectivity.Signal();
            }
        }
    }
}
