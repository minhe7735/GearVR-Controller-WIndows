using System;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;

namespace GearVR_Controller
{
    // Passive listening does not connect to the device or send BLE scan requests.
    internal sealed class WakeDetector : IDisposable
    {
        private readonly object sync = new();
        private readonly ulong address;
        private readonly BluetoothLEAdvertisementWatcher watcher;
        private readonly WakeSignal signal;
        private Timer timer;
        private bool disposed;
        public event Action Ready;
        public event Action Awakened;
        public event Action<string> Failed;

        public WakeDetector(ulong address)
        {
            this.address = address;
            signal = new WakeSignal(Environment.TickCount64, 10000);
            watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Passive };
            watcher.Received += OnReceived;
            watcher.Stopped += OnStopped;
        }

        public void Start()
        {
            watcher.Start();
            timer = new Timer(CheckSilence, null, 1000, 1000);
        }

        private void CheckSilence(object ignored)
        {
            bool ready;
            lock (sync) { if (disposed) return; ready = signal.CheckQuiet(Environment.TickCount64); }
            if (ready) Ready?.Invoke();
        }

        private void OnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.BluetoothAddress != address) return;
            bool awakened;
            lock (sync) { if (disposed) return; awakened = signal.Advertisement(Environment.TickCount64); }
            if (awakened) Awakened?.Invoke();
        }

        private void OnStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            lock (sync) { if (disposed) return; }
            Failed?.Invoke(args.Error.ToString());
        }

        public void Dispose()
        {
            lock (sync) { if (disposed) return; disposed = true; }
            timer?.Dispose();
            watcher.Received -= OnReceived;
            watcher.Stopped -= OnStopped;
            watcher.Stop();
        }
    }
}
