using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;

namespace GearVR_Controller
{
    internal static class ControllerDiscovery
    {
        private static bool IsController(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.IndexOf("Gear VR Controller", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string FormatAddress(ulong address)
        {
            string hex = address.ToString("X12");
            return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
        }

        // Include known devices even when asleep, and advertisements from nearby devices.
        public static async Task<IReadOnlyList<string>> FindAsync(CancellationToken token)
        {
            var addresses = new HashSet<ulong>();
            var sync = new object();
            var watcher = new BluetoothLEAdvertisementWatcher();
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Received += (sender, args) =>
            {
                if (IsController(args.Advertisement.LocalName) && args.BluetoothAddress != 0)
                    lock (sync) addresses.Add(args.BluetoothAddress);
            };
            bool watching = false;
            try
            {
                try { watcher.Start(); watching = true; }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()).AsTask(token);
                foreach (var info in devices.Where(d => IsController(d.Name)))
                {
                    token.ThrowIfCancellationRequested();
                    using var device = await BluetoothLEDevice.FromIdAsync(info.Id).AsTask(token);
                    if (device != null && device.BluetoothAddress != 0)
                        lock (sync) addresses.Add(device.BluetoothAddress);
                }
                if (watching) await Task.Delay(TimeSpan.FromSeconds(4), token);
            }
            finally { if (watching) watcher.Stop(); }
            lock (sync) return addresses.OrderBy(a => a).Select(FormatAddress).ToArray();
        }
    }
}
