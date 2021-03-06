﻿using System;
using System.Net.NetworkInformation;
using Couchbase.Lite.Util;

#if __ANDROID__
using Android.App;
using Android.Net;
using Android.Content;
using Android.Webkit;
#endif
namespace Couchbase.Lite
{

    // NOTE: The Android specific #ifdefs are in here solely
    //       to work around Xamarin.Android bug 
    //       https://bugzilla.xamarin.com/show_bug.cgi?id=1969

    // NOTE: The issue above was fixed as of Xamarin.Android 4.18

    /// <summary>
    /// This uses the NetworkAvailability API to listen for network reachability
    /// change events and fires off changes internally.
    /// </summary>
    internal sealed class NetworkReachabilityManager : INetworkReachabilityManager
    {
        #if __ANDROID__
        private class AndroidNetworkChangeReceiver : BroadcastReceiver
        {
            const string Tag = "AndroidNetworkChangeReceiver";

            readonly private Action<NetworkReachabilityStatus> _callback;

            object lockObject;

            private volatile Boolean _ignoreNotifications;

            private NetworkReachabilityStatus _lastStatus;

            public AndroidNetworkChangeReceiver(Action<NetworkReachabilityStatus> callback)
            {
                _callback = callback;
                _ignoreNotifications = true;

                lockObject = new object();
            }

            public override void OnReceive(Context context, Intent intent)
            {
                Log.D(Tag + ".OnReceive", "Received intent: {0}", intent.ToString());

                if (_ignoreNotifications) {
                    Log.D(Tag + ".OnReceive", "Ignoring received intent: {0}", intent.ToString());
                    _ignoreNotifications = false;
                    return;
                }

                Log.D(Tag + ".OnReceive", "Received intent: {0}", intent.ToString());

                var status = intent.GetBooleanExtra(ConnectivityManager.ExtraNoConnectivity, false)
                    ? NetworkReachabilityStatus.Unreachable
                    : NetworkReachabilityStatus.Reachable;

                if (!status.Equals(_lastStatus))
                {
                    _callback(status);
                }

                lock(lockObject) 
                {
                    _lastStatus = status;
                }
            }

            public void EnableListening()
            {
                _ignoreNotifications = false;
            }

            public void DisableListening()
            {
                _ignoreNotifications = true;
            }
        }

        private AndroidNetworkChangeReceiver _receiver;

        #endif

        #region INetworkReachabilityManager implementation

        public event EventHandler<NetworkReachabilityChangeEventArgs> StatusChanged;

        /// <summary>This method starts listening for network connectivity state changes.</summary>
        /// <remarks>This method starts listening for network connectivity state changes.</remarks>
        public void StartListening()
        {
            #if __ANDROID__
            if (_receiver != null) {
                return; // We only need one handler.
            }
            var intent = new IntentFilter(ConnectivityManager.ConnectivityAction);
            _receiver = new AndroidNetworkChangeReceiver(InvokeNetworkChangeEvent);
            Application.Context.RegisterReceiver(_receiver, intent);
            #else
            if (_isListening) {
                return;
            }
            NetworkChange.NetworkAvailabilityChanged += OnNetworkChange;
            _isListening = true;
            #endif
        }

        /// <summary>This method stops this class from listening for network changes.</summary>
        /// <remarks>This method stops this class from listening for network changes.</remarks>
        public void StopListening()
        {
            #if __ANDROID__
            if (_receiver == null) {
                return;
            }
            _receiver.DisableListening();
            Application.Context.UnregisterReceiver(_receiver);
            _receiver = null;
            #else
            if (!_isListening) {
                return;
            }
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkChange;          
            #endif
        }
        #endregion

        #region Private Members

        #if !__ANDROID__
        private volatile Boolean _isListening;
        #endif

        #endregion

        /// <summary>Notify listeners that the network is now reachable/unreachable.</summary>
        internal void OnNetworkChange(Object sender, NetworkAvailabilityEventArgs args)
        {
            var status = args.IsAvailable
                ? NetworkReachabilityStatus.Reachable
                : NetworkReachabilityStatus.Unreachable;
            
            InvokeNetworkChangeEvent(status);
        }

        void InvokeNetworkChangeEvent(NetworkReachabilityStatus status)
        {
            var evt = StatusChanged;
            if (evt == null)
            {
                return;
            }
            var eventArgs = new NetworkReachabilityChangeEventArgs(status);
            evt(this, eventArgs);
        }
    }
}

