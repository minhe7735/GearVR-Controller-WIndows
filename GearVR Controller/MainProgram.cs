using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;



namespace GearVR_Controller
{
    public class MainProgram
    {
        readonly SensorData sensorData = SensorData.GetInstance();

        private static readonly MainProgram instance = new();
        private MainProgram() { OffTimer.Elapsed += OffTimer_Elapsed; }
        public static MainProgram GetInstance() { return instance; }

        readonly System.Timers.Timer OffTimer = new(Settings.Default._TimeOut) { AutoReset = false };
        readonly object inputLock = new();
        readonly Dictionary<string, string> activeButtons = new();
        readonly Dictionary<string, int> activeInputs = new();
        static readonly Dictionary<string, short> keyCodes = KeyCodeCollection.GetKeyCodes();
        readonly SemaphoreSlim connectionGate = new(1, 1);
        int connectionGeneration;
        long lastPacketTimestamp;
        readonly MotionPointer motion = new();
        readonly IdleActivity activity = new();

        public void ResetIdleTimer()
        {
            lock (inputLock) activity.Reset(Stopwatch.GetTimestamp());
        }

        public bool DisconnectIfIdle(TimeSpan timeout)
        {
            lock (inputLock)
            {
                if (!Settings.Default._Connected || !activity.IsIdle(Stopwatch.GetTimestamp(), Stopwatch.Frequency, timeout)) return false;
                Disconnect();
                return true;
            }
        }
        long homePressedAt;
        bool homeHeld;
        bool homeRecentered;
        bool pointerPending;
        double pointerX, pointerY, targetX, targetY;
        long lastPointerTick;

        // Called on the UI dispatcher, independently of the Bluetooth packet rate.
        public void RenderMotionPointer()
        {
            lock (inputLock)
            {
                if (!pointerPending) return;
                long now = Stopwatch.GetTimestamp();
                double elapsed = lastPointerTick == 0 ? 0.008 : Math.Min(0.05, (double)(now - lastPointerTick) / Stopwatch.Frequency);
                lastPointerTick = now;
                double blend = 1.0 - Math.Exp(-elapsed / 0.022);
                pointerX += (targetX - pointerX) * blend;
                pointerY += (targetY - pointerY) * blend;
                if (Math.Abs(pointerX - targetX) < 0.5 && Math.Abs(pointerY - targetY) < 0.5)
                {
                    pointerX = targetX; pointerY = targetY; pointerPending = false; lastPointerTick = 0;
                }
                SetCursorPos((int)Math.Round(pointerX), (int)Math.Round(pointerY));
            }
        }

        private void QueuePointer(double x, double y)
        {
            if (!pointerPending && GetCursorPos(out Point current))
            {
                pointerX = current.X; pointerY = current.Y; lastPointerTick = 0;
            }
            targetX = x; targetY = y; pointerPending = true;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X; public int Y; }
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        private static DesktopBounds GetDesktopBounds() => new(
            GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(78), GetSystemMetrics(79));

        private static Point CurrentMonitorCenter(DesktopBounds desktop)
        {
            if (GetCursorPos(out Point cursor))
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                IntPtr monitor = MonitorFromPoint(cursor, 2); // nearest monitor
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                    return new Point { X = info.Monitor.Left + (info.Monitor.Right - info.Monitor.Left) / 2,
                                       Y = info.Monitor.Top + (info.Monitor.Bottom - info.Monitor.Top) / 2 };
            }
            return new Point { X = desktop.Left + desktop.Width / 2, Y = desktop.Top + desktop.Height / 2 };
        }

        public void ResetMotionPointer()
        {
            lock (inputLock) { motion.Reset(); homeHeld = false; homeRecentered = false; pointerPending = false; lastPointerTick = 0; }
        }

        private void UpdateMotionPointer(byte[] data, long now, bool home)
        {
            if (!User.Default.MotionPointer)
            {
                UpdateButton("home", home, User.Default.HomeButton);
                return;
            }
            if (home && !homeHeld)
            {
                homeHeld = true;
                pointerPending = false; lastPointerTick = 0;
                homeRecentered = false;
                homePressedAt = now;
                motion.BeginCalibration();
            }
            else if (!home && homeHeld)
            {
                if (!homeRecentered)
                {
                    UpdateButton("home", true, User.Default.HomeButton);
                    UpdateButton("home", false, User.Default.HomeButton);
                }
                homeHeld = false;
            }

            DesktopBounds desktop = GetDesktopBounds();
            if (!desktop.IsValid) return;
            if (!motion.Initialized && GetCursorPos(out Point cursor))
                motion.SetPosition(desktop.ToMotionX(cursor.X), desktop.ToMotionY(cursor.Y));
            Vector3 accel = new(ReadSensor(data, 4), ReadSensor(data, 6), ReadSensor(data, 8));
            const float gyroScale = 0.017453292f / 14.285f;
            Vector3 gyro = new Vector3(ReadSensor(data, 10), ReadSensor(data, 12), ReadSensor(data, 14)) * gyroScale;
            bool moved = motion.Update(accel, gyro, now, Stopwatch.Frequency, homeHeld && !homeRecentered, Math.Clamp(User.Default.MotionSensitivity, 0.25, 3.0), out double x, out double y);
            if (homeHeld && !homeRecentered && (double)(now - homePressedAt) / Stopwatch.Frequency >= 1.0)
            {
                motion.Calibrate();
                homeRecentered = true;
                pointerPending = false; lastPointerTick = 0;
                Point center = CurrentMonitorCenter(desktop);
                motion.SetPosition(desktop.ToMotionX(center.X), desktop.ToMotionY(center.Y));
                SetCursorPos(center.X, center.Y);
            }
            else if (moved && !homeHeld)
                QueuePointer(desktop.ToScreenX(x), desktop.ToScreenY(y));
        }

        private static short ReadSensor(byte[] data, int offset) => (short)(data[offset] | (data[offset + 1] << 8));

        private void SetStatus(bool connected)
        {
            Settings.Default._Connected = connected;
            Settings.Default._Status = connected ? "#00BB00" : "#BB0000";
            Settings.Default._StatusEffect = connected ? "#00FF00" : "#FF0000";
        }

        private void OffTimer_Elapsed(object source, ElapsedEventArgs e)
        {
            lock (inputLock)
            {
                if ((Stopwatch.GetTimestamp() - lastPacketTimestamp) * 1000.0 / Stopwatch.Frequency < Settings.Default._TimeOut) return;
                ReleaseAllInputs();
                iTrackingTouch = false;
                motion.Reset(); activity.Stop(); homeHeld = false; homeRecentered = false; pointerPending = false; lastPointerTick = 0;
                SetStatus(false);
            }
        }

        private static ulong MAC802DOT3(string address)
        {
            string hex = (address ?? "").Replace(":", "").Replace("-", "").Trim();
            if (hex.Length != 12 || !ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong result))
                throw new ArgumentException("Introduce una direccion MAC valida de 12 digitos hexadecimales.");
            return result;
        }

        private static void SendInput(string input, bool down)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            if (input == "Toggle")
            {
                if (down) Settings.Default._UseWheel = !Settings.Default._UseWheel;
            }
            else if (input.StartsWith("Launch:", StringComparison.OrdinalIgnoreCase))
            {
                if (down)
                {
                    try { Process.Start(new ProcessStartInfo(input.Substring(7).Trim()) { UseShellExecute = true }); }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                }
            }
            else if (input.StartsWith("Mouse ", StringComparison.Ordinal))
            {
                if (keyCodes.TryGetValue("--Don't Use This " + input + (down ? " DOWN" : " UP"), out short flag))
                    mouse_event(flag, 0, 0, 0, 0);
            }
            else if (input == "X1 mouse button" || input == "X2 mouse button")
                mouse_event(keyCodes[down ? "--Don't Use This X DOWN" : "--Don't Use This X UP"], 0, 0, keyCodes[input], 0);
            else if (keyCodes.TryGetValue(input, out short key))
            {
                // Navigation and Windows keys use the extended-key flag.
                bool extended = key == 0x21 || key == 0x22 || key == 0x23 || key == 0x24 ||
                    (key >= 0x25 && key <= 0x28) || key == 0x2D || key == 0x2E ||
                    key == 0x5B || key == 0x5C || key == 0x5D || key == 0x6F;
                keybd_event((byte)key, 0, (extended ? 1u : 0u) | (down ? 0u : 2u), 0);
            }
        }

        private static void SendWheel(string axis, int strength)
        {
            mouse_event(keyCodes[axis == "HWheel" ? "--Don't Use This Mouse hwheel" : "--Don't Use This Mouse wheel"], 0, 0, User.Default.ScrollWheelSpeed * strength, 0);
        }

        private void UpdateButton(string button, bool pressed, string mapping)
        {
            if (pressed && !activeButtons.ContainsKey(button))
            {
                if (string.IsNullOrWhiteSpace(mapping)) return;
                activeButtons[button] = mapping;
                activeInputs.TryGetValue(mapping, out int count);
                activeInputs[mapping] = count + 1;
                if (count == 0) SendInput(mapping, true);
            }
            else if (!pressed && activeButtons.TryGetValue(button, out string held))
            {
                activeButtons.Remove(button);
                int count = activeInputs[held] - 1;
                if (count == 0) { activeInputs.Remove(held); SendInput(held, false); }
                else activeInputs[held] = count;
            }
        }

        private void ReleaseAllInputs()
        {
            foreach (string input in activeInputs.Keys.ToArray()) SendInput(input, false);
            activeButtons.Clear();
            activeInputs.Clear();
        }

        private string TouchMapping(bool trigger)
        {
            int x = sensorData.AxisX, y = sensorData.AxisY;
            if (trigger && x == 0 && y == 0) return User.Default.TriggerButton;
            if ((x - 157) * (x - 157) + (y - 157) * (y - 157) <= 6400)
                return trigger ? User.Default.TriggerMidButton : User.Default.TouchMidButton;
            if (sensorData.ShiftedX < 0 && sensorData.ShiftedY >= 0) return trigger ? User.Default.TriggerLeftButton : User.Default.TouchLeftButton;
            if (sensorData.ShiftedX > 0 && sensorData.ShiftedY <= 0) return trigger ? User.Default.TriggerRightButton : User.Default.TouchRightButton;
            if (sensorData.ShiftedX > 0 && sensorData.ShiftedY >= 0) return trigger ? User.Default.TriggerBotButton : User.Default.TouchBotButton;
            return trigger ? User.Default.TriggerTopButton : User.Default.TouchTopButton;
        }

        bool iTrackingTouch = false;

        private void TrackPad()
        {
            if (sensorData.AxisX == 0 && sensorData.AxisY == 0)
            {
                // No finger on our trackpad, nothing for us to do here
                iTrackingTouch = false;
                return;
            }

            if (!iTrackingTouch)
            {
                // Tracking just started
                iTrackingTouch = true;
                // Mark our tracking start position
                sensorData._AxisX = sensorData.AxisX;
                sensorData._AxisY = sensorData.AxisY;
                // Still nothing to do here until finger moves
                return;
            }

            // Finger was moved, compute our deltas
            sensorData.Delta_X = sensorData.AxisX - sensorData._AxisX;
            sensorData.Delta_Y = sensorData.AxisY - sensorData._AxisY;
            //
            int absDeltaX = Math.Abs(sensorData.Delta_X);
            int absDeltaY = Math.Abs(sensorData.Delta_Y);

            // Wheel trigger thresholds are intended to provide stability and avoid bounce back
            // TODO: Put them in settings I guess
            const int KWheelThresholdX = 10; // Horizontal
            const int KWheelThresholdY = 10; // Vertical
            const int KMouseThreshold = 20;
            const double KMouseAcceleration = 0.1;

            bool consumedX = false;
            bool consumedY = false;

            if (Settings.Default._UseWheel)
            {   
                if ((absDeltaX > KWheelThresholdX) 
                    // We either scroll vertically or horizontally not both
                    && absDeltaX > absDeltaY)
                {
                    SendWheel("HWheel", sensorData.Delta_X);
                    consumedX = true;
                    consumedY = true;
                }

                if ((absDeltaY > KWheelThresholdY)
                    // We either scroll vertically or horizontally not both
                    && absDeltaY > absDeltaX)
                {
                    SendWheel("VWheel", sensorData.Delta_Y);
                    consumedX = true;
                    consumedY = true;
                }
            }
            else
            {
                consumedX = true;
                consumedY = true;
                // Mouse pointer mouve
                int offsetX = sensorData.Delta_X;
                int offsetY = sensorData.Delta_Y;
                if (absDeltaX > KMouseThreshold)
                {
                    offsetX *= (int)Math.Round(absDeltaX * KMouseAcceleration);
                    //Debug.Print("AccX");
                }

                if (absDeltaY > KMouseThreshold)
                {
                    offsetY *= (int)Math.Round(absDeltaY * KMouseAcceleration);
                    //Debug.Print("AccY");
                }

                if (offsetX != 0 || offsetY != 0)
                {
                    mouse_event(0x0001, offsetX, offsetY, 0, 0);
                }                
            }
            
            // Mark our last positions if needed
            if (consumedX)
            {
                sensorData._AxisX = sensorData.AxisX;                
            }

            if (consumedY)
            {
                sensorData._AxisY = sensorData.AxisY;
            }
        }

        public static readonly Guid guid_controller_service = Guid.Parse(Settings.Default._Service);
        public static readonly Guid guid_controller_write = Guid.Parse(Settings.Default._Write);
        public static readonly Guid guid_controller_notify = Guid.Parse(Settings.Default._Notify);

        private static GattCharacteristic write_characteristic = null;
        private static GattCharacteristic notify_characteristic = null;
        private static BluetoothLEDevice device = null;

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);


        private static int FwheelPos(int x, int y)
        {
            int pos;
            if (x == 0 && y == 0) { }
            Complex cnum = new(x - 157, y - 157);
            pos = (int)Math.Floor((cnum.Phase * 180 / Math.PI) / 360.0 * User.Default.ScrollWheelSeg);
            return pos;
        }

        private readonly byte[] byte_values = new byte[60];
        private void SelectedCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            lock (inputLock)
            {
            if (sender != notify_characteristic) return;
            if (byte_values.Length == args.CharacteristicValue.Length)
            {
                if (!Settings.Default._Connected)
                {
                    Settings.Default._Connected = true;
                    Settings.Default._Status = "#00BB00";
                    Settings.Default._StatusEffect = "#00FF00";
                }

                lastPacketTimestamp = Stopwatch.GetTimestamp();
                OffTimer.Stop();
                OffTimer.Interval = Settings.Default._TimeOut;
                OffTimer.Start();

                DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(byte_values);

                sensorData.AxisX = (((byte_values[54] & 0xF) << 6) + ((byte_values[55] & 0xFC) >> 2)) & 0x3FF;
                sensorData.AxisY = (((byte_values[55] & 0x3) << 8) + ((byte_values[56] & 0xFF) >> 0)) & 0x3FF;
                activity.Observe(lastPacketTimestamp,
                    new Vector3(ReadSensor(byte_values, 4), ReadSensor(byte_values, 6), ReadSensor(byte_values, 8)),
                    new Vector3(ReadSensor(byte_values, 10), ReadSensor(byte_values, 12), ReadSensor(byte_values, 14)),
                    (byte_values[58] & 0x3f) != 0, sensorData.AxisX != 0 || sensorData.AxisY != 0);
                if (User.Default.Accel)
                {
                    sensorData.AccelX = (int)(ReadSensor(byte_values, 4) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelY = (int)(ReadSensor(byte_values, 6) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelZ = (int)(ReadSensor(byte_values, 8) * 10000.0 * 9.80665 / 2048.0);
                }
                if (User.Default.Gyro)
                {
                    sensorData.GyroX = (int)(ReadSensor(byte_values, 10) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroY = (int)(ReadSensor(byte_values, 12) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroZ = (int)(ReadSensor(byte_values, 14) * 10000.0 * 0.017453292 / 14.285);
                }
                if (User.Default.Magnet)
                {
                    sensorData.MagnetX = (int)(ReadSensor(byte_values, 32) * 0.06);
                    sensorData.MagnetY = (int)(ReadSensor(byte_values, 34) * 0.06);
                    sensorData.MagnetZ = (int)(ReadSensor(byte_values, 36) * 0.06);
                }

                Settings.Default._TriggerButton = ((byte_values[58] & 1) == 1);
                Settings.Default._HomeButton = ((byte_values[58] & 2) == 2);
                Settings.Default._BackButton = ((byte_values[58] & 4) == 4);
                Settings.Default._TouchpadButton = ((byte_values[58] & 8) == 8);
                Settings.Default._VolumeUpButton = ((byte_values[58] & 16) == 16);
                Settings.Default._VolumeDownButton = ((byte_values[58] & 32) == 32);
                Settings.Default._NoButton = ((byte_values[58] & 64) == 64);

                sensorData.ShiftedX = (sensorData.AxisX - 157) + (sensorData.AxisY - 157);
                sensorData.ShiftedY = sensorData.AxisY - sensorData.AxisX;

                UpdateButton("touch", Settings.Default._TouchpadButton, TouchMapping(false));
                UpdateButton("trigger", Settings.Default._TriggerButton, TouchMapping(true));
                UpdateMotionPointer(byte_values, lastPacketTimestamp, Settings.Default._HomeButton);
                UpdateButton("back", Settings.Default._BackButton, User.Default.BackButton);
                UpdateButton("up", Settings.Default._VolumeUpButton, User.Default.VolumeUpButton);
                UpdateButton("down", Settings.Default._VolumeDownButton, User.Default.VolumeDownButton);

                if (!User.Default.MotionPointer || Settings.Default._UseWheel) TrackPad();
                else iTrackingTouch = false;
            }
            }
        }
        public void Disconnect()
        {
            Interlocked.Increment(ref connectionGeneration);
            lock (inputLock)
            {
                OffTimer.Stop();
                if (notify_characteristic != null)
                    notify_characteristic.ValueChanged -= SelectedCharacteristic_ValueChanged;
                ReleaseAllInputs();
                iTrackingTouch = false;
                motion.Reset(); activity.Stop(); homeHeld = false; homeRecentered = false; pointerPending = false; lastPointerTick = 0;
                SetStatus(false);
                notify_characteristic?.Service?.Dispose();
                notify_characteristic = null;
                write_characteristic = null;
                device?.Dispose();
                device = null;
            }
        }

        private void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            if (args.PairingKind == DevicePairingKinds.ConfirmOnly) args.Accept();
        }

        public async Task Pair_Connect(CancellationToken cancellationToken)
        {
            int generation = Interlocked.Increment(ref connectionGeneration);
            await connectionGate.WaitAsync(cancellationToken);
            BluetoothLEDevice candidate = null;
            GattDeviceService service = null;
            GattCharacteristic notifications = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != connectionGeneration) return;
                // Dispose the previous session without invalidating this attempt.
                lock (inputLock)
                {
                    OffTimer.Stop();
                    if (notify_characteristic != null) notify_characteristic.ValueChanged -= SelectedCharacteristic_ValueChanged;
                    ReleaseAllInputs();
                    iTrackingTouch = false;
                    motion.Reset(); activity.Stop(); homeHeld = false; homeRecentered = false; pointerPending = false; lastPointerTick = 0;
                    SetStatus(false);
                    notify_characteristic?.Service?.Dispose();
                    device?.Dispose();
                    notify_characteristic = null; write_characteristic = null; device = null;
                }
                candidate = await BluetoothLEDevice.FromBluetoothAddressAsync(MAC802DOT3(User.Default.MACAddressText)).AsTask(cancellationToken);
                if (candidate == null) throw new InvalidOperationException("Windows no encontro el mando. Despiertalo y comprueba la direccion MAC y Bluetooth.");
                if (!candidate.DeviceInformation.Pairing.IsPaired)
                {
                    var pairing = candidate.DeviceInformation.Pairing.Custom;
                    pairing.PairingRequested += PairingRequestedHandler;
                    try
                    {
                        var result = await pairing.PairAsync(DevicePairingKinds.ConfirmOnly, DevicePairingProtectionLevel.None).AsTask(cancellationToken);
                        if (result.Status != DevicePairingResultStatus.Paired && result.Status != DevicePairingResultStatus.AlreadyPaired)
                            throw new InvalidOperationException("No se pudo emparejar el mando: " + result.Status);
                    }
                    finally { pairing.PairingRequested -= PairingRequestedHandler; }
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != connectionGeneration) return;
                var services = await candidate.GetGattServicesForUuidAsync(guid_controller_service, BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
                    throw new InvalidOperationException("No se encontro el servicio del mando: " + services.Status + ". Despierta el mando y vuelve a intentar.");
                service = services.Services[0];
                var chars = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                if (chars.Status != GattCommunicationStatus.Success)
                    throw new InvalidOperationException("No se pudieron leer las caracteristicas Bluetooth: " + chars.Status);
                var writer = chars.Characteristics.FirstOrDefault(c => c.Uuid == guid_controller_write);
                notifications = chars.Characteristics.FirstOrDefault(c => c.Uuid == guid_controller_notify);
                if (writer == null || notifications == null) throw new InvalidOperationException("Faltan caracteristicas Bluetooth del mando.");
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != connectionGeneration) return;
                notifications.ValueChanged += SelectedCharacteristic_ValueChanged;
                var status = await notifications.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(cancellationToken);
                if (status != GattCommunicationStatus.Success) throw new InvalidOperationException("No se pudieron activar las notificaciones: " + status);
                lock (inputLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (generation != connectionGeneration) return;
                    device = candidate; notify_characteristic = notifications; write_characteristic = writer;
                }
                foreach (byte command in new byte[] { 0x01, 0x06, 0x07, 0x08 })
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (generation != connectionGeneration) return;
                    status = await writer.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(new byte[] { command, 0x00 })).AsTask(cancellationToken);
                    if (status != GattCommunicationStatus.Success) throw new InvalidOperationException("Fallo la inicializacion del mando: " + status);
                }
                cancellationToken.ThrowIfCancellationRequested();
                candidate = null; service = null; notifications = null;
            }
            catch
            {
                if (generation == connectionGeneration) Disconnect();
                throw;
            }
            finally
            {
                if (notifications != null) notifications.ValueChanged -= SelectedCharacteristic_ValueChanged;
                service?.Dispose();
                candidate?.Dispose();
                connectionGate.Release();
            }
        }
    }
}
