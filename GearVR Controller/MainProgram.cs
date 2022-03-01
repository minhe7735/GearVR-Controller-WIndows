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



namespace GearVR_Controller
{
    public class MainProgram
    {
        readonly SensorData sensorData = SensorData.GetInstance();

        private static readonly MainProgram instance = new();
        private MainProgram() { }
        public static MainProgram GetInstance() { return instance; }

        readonly Timer OffTimer = new(Settings.Default._TimeOut);
        private void OffTimer_Elapsed(object source, ElapsedEventArgs e)
        {
            Settings.Default._Connected = false;
            Settings.Default._Status = "#BB0000";
            Settings.Default._StatusEffect = "#FF0000";
        }
        private static ulong MAC802DOT3(string macAddress) //https://stackoverflow.com/a/50519302
        {
            string hex = macAddress.Replace(":", "");
            return Convert.ToUInt64(hex, 16);
        }
        private static void SendInput(string input, int aStrength = 0)
        {
            if (input == null) return;
            string[] words = input.Split(' ');
            switch (words[0])
            {
                case "VWheel":
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This Mouse wheel"], 0, 0, User.Default.ScrollWheelSpeed * aStrength, 0);
                    break;
                case "HWheel":
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This Mouse hwheel"], 0, 0, User.Default.ScrollWheelSpeed * aStrength, 0);
                    break;
                case "Toggle":
                    if (input != Settings.Default._CurrentInput)
                    {
                        Settings.Default._UseWheel = !Settings.Default._UseWheel;
                    }
                    break;
                case "X1":

                case "X2":
                    if (input == Settings.Default._CurrentInput)
                    {
                        mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This X DOWN"], 0, 0, (byte)KeyCodeCollection.GetKeyCodes()[input], 0);
                    }
                    else
                    {
                        mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This X UP"], 0, 0, (byte)KeyCodeCollection.GetKeyCodes()[input], 0);
                    }
                    break;
                case "Mouse":
                    if (input == Settings.Default._CurrentInput)
                    {
                        mouse_event((byte)KeyCodeCollection.GetKeyCodes()["--Don't Use This " + input + " DOWN"], 0, 0, 0, 0);
                    }
                    else
                    {
                        mouse_event((byte)KeyCodeCollection.GetKeyCodes()["--Don't Use This " + input + " UP"], 0, 0, 0, 0);
                    }
                    break;
                case "Launch:":
                    if (input != Settings.Default._CurrentInput)
                    {
                        Process P = new();
                        P.StartInfo.UseShellExecute = true;
                        P.StartInfo.FileName = input[(words[0].Length + 1)..];
                        P.Start();
                    }
                    break;
                default:
                    if (input == Settings.Default._CurrentInput)
                    {
                        keybd_event((byte)KeyCodeCollection.GetKeyCodes()[input], 0, 1 | 0, 0);
                        Settings.Default._TimePrev = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    }
                    else
                    {
                        keybd_event((byte)KeyCodeCollection.GetKeyCodes()[input], 0, 1 | 2, 0);
                    }
                    break;
            }
        }
        private static bool TimePassed()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds() - Settings.Default._TimePrev > 200;
        }
        private static void HandleButtonDown(string button, string status)
        {
            Settings.Default._CurrentInput = button;
            SendInput(button);
            Settings.Default[status] = false;
        }
        private static void HandleButtonUp(string status)
        {
            Settings.Default._Release = Settings.Default._CurrentInput;
            Settings.Default._CurrentInput = null;
            SendInput(Settings.Default._Release);
            Settings.Default[status] = true;
        }
        private void TouchpadButton()
        {
            if (Settings.Default._TouchpadButton
                    && ((Math.Pow(sensorData.AxisX - 157, 2) + Math.Pow(sensorData.AxisY - 157, 2) <= 6400
                    && Settings.Default._TchBtn)
                    || (Settings.Default._CurrentInput == User.Default.TouchMidButton
                    && TimePassed()
                    && !User.Default.TouchMidButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.TouchMidButton, "_TchBtn");
            }
            else if (Settings.Default._TouchpadButton
                && ((sensorData.ShiftedX < 0
                && sensorData.ShiftedY >= 0
                && Settings.Default._TchBtn)
                || (Settings.Default._CurrentInput == User.Default.TouchLeftButton
                && TimePassed()
                && !User.Default.TouchLeftButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TouchLeftButton, "_TchBtn");
            }
            else if (Settings.Default._TouchpadButton
                && ((sensorData.ShiftedX > 0
                && sensorData.ShiftedY <=
                0 && Settings.Default._TchBtn)
                || (Settings.Default._CurrentInput == User.Default.TouchRightButton
                && TimePassed()
                && !User.Default.TouchRightButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TouchRightButton, "_TchBtn");
            }
            else if (Settings.Default._TouchpadButton
                && ((sensorData.ShiftedX > 0
                && sensorData.ShiftedY >= 0
                && Settings.Default._TchBtn)
                || (Settings.Default._CurrentInput == User.Default.TouchBotButton
                && TimePassed()
                && !User.Default.TouchBotButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TouchBotButton, "_TchBtn");
            }
            else if (Settings.Default._TouchpadButton
                && ((sensorData.ShiftedX < 0
                && sensorData.ShiftedY <= 0
                && Settings.Default._TchBtn)
                || (Settings.Default._CurrentInput == User.Default.TouchTopButton
                && TimePassed()
                && !User.Default.TouchTopButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TouchTopButton, "_TchBtn");
            }
            else if (!Settings.Default._TouchpadButton && !Settings.Default._TchBtn)
            {
                HandleButtonUp("_TchBtn");
            }
        }
        private void TriggerButton()
        {
            if (Settings.Default._TriggerButton
                    && ((sensorData.AxisX == 0
                    && sensorData.AxisY == 0
                    && Settings.Default._TrigBtn)
                    || (Settings.Default._CurrentInput == User.Default.TriggerButton
                    && TimePassed()
                    && !User.Default.TriggerButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.TriggerButton, "_TrigBtn");
            }
            else if (Settings.Default._TriggerButton
                && ((Math.Pow(sensorData.AxisX - 157, 2) + Math.Pow(sensorData.AxisY - 157, 2) <= 6400
                && Settings.Default._TrigBtn)
                || (Settings.Default._CurrentInput == User.Default.TriggerMidButton
                && TimePassed()
                && !User.Default.TriggerMidButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TriggerMidButton, "_TrigBtn");
            }
            else if (Settings.Default._TriggerButton
                && ((sensorData.ShiftedX < 0
                && sensorData.ShiftedY >= 0
                && Settings.Default._TrigBtn)
                || (Settings.Default._CurrentInput == User.Default.TriggerLeftButton
                && TimePassed()
                && !User.Default.TriggerLeftButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TriggerLeftButton, "_TrigBtn");
            }
            else if (Settings.Default._TriggerButton
                && ((sensorData.ShiftedX > 0
                && sensorData.ShiftedY <= 0
                && Settings.Default._TrigBtn)
                || (Settings.Default._CurrentInput == User.Default.TriggerRightButton
                && TimePassed()
                && !User.Default.TriggerRightButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TriggerRightButton, "_TrigBtn");
            }
            else if (Settings.Default._TriggerButton
                && ((sensorData.ShiftedX > 0
                && sensorData.ShiftedY >= 0
                && Settings.Default._TrigBtn)
                || (Settings.Default._CurrentInput == User.Default.TriggerBotButton
                && TimePassed()
                && !User.Default.TriggerBotButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TriggerBotButton, "_TrigBtn");
            }
            else if (Settings.Default._TriggerButton
                && ((sensorData.ShiftedX < 0
                && sensorData.ShiftedY <= 0
                && Settings.Default._TrigBtn)
                || (Settings.Default._CurrentInput == User.Default.TriggerTopButton
                && TimePassed()
                && !User.Default.TriggerTopButton.StartsWith("Mouse")
                )))
            {
                HandleButtonDown(User.Default.TriggerTopButton, "_TrigBtn");
            }
            else if (!Settings.Default._TriggerButton && !Settings.Default._TrigBtn)
            {
                HandleButtonUp("_TrigBtn");
            }
        }
        private static void HomeButton()
        {
            if (Settings.Default._HomeButton
                    && (Settings.Default._HomeBtn
                    || (Settings.Default._CurrentInput == User.Default.HomeButton
                    && TimePassed()
                    && !User.Default.HomeButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.HomeButton, "_HomeBtn");
            }
            else if (!Settings.Default._HomeButton && !Settings.Default._HomeBtn)
            {
                HandleButtonUp("_HomeBtn");
            }
        }
        private static void BackButton()
        {
            if (Settings.Default._BackButton
                    && (Settings.Default._BackBtn
                    || (Settings.Default._CurrentInput == User.Default.BackButton
                    && TimePassed()
                    && !User.Default.BackButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.BackButton, "_BackBtn");
            }
            else if (!Settings.Default._BackButton && !Settings.Default._BackBtn)
            {
                HandleButtonUp("_BackBtn");
            }
        }
        private static void VolumeUpButton()
        {
            if (Settings.Default._VolumeUpButton
                    && (Settings.Default._VolUpBtn
                    || (Settings.Default._CurrentInput == User.Default.VolumeUpButton
                    && TimePassed()
                    && !User.Default.VolumeUpButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.VolumeUpButton, "_VolUpBtn");
            }
            else if (!Settings.Default._VolumeUpButton && !Settings.Default._VolUpBtn)
            {
                HandleButtonUp("_VolUpBtn");
            }
        }
        private static void VolumeDownButton()
        {
            if (Settings.Default._VolumeDownButton
                    && (Settings.Default._VolDownBtn
                    || (Settings.Default._CurrentInput == User.Default.VolumeDownButton
                    && TimePassed()
                    && !User.Default.VolumeDownButton.StartsWith("Mouse")
                    )))
            {
                HandleButtonDown(User.Default.VolumeDownButton, "_VolDownBtn");
            }
            else if (!Settings.Default._VolumeDownButton && !Settings.Default._VolDownBtn)
            {
                HandleButtonUp("_VolDownBtn");
            }
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
                    SendInput("HWheel", sensorData.Delta_X);
                    consumedX = true;
                    consumedY = true;
                }

                if ((absDeltaY > KWheelThresholdY)
                    // We either scroll vertically or horizontally not both
                    && absDeltaY > absDeltaX)
                {
                    SendInput("VWheel", sensorData.Delta_Y);
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
            if (byte_values.Length == args.CharacteristicValue.Length)
            {
                if (!Settings.Default._Connected)
                {
                    Settings.Default._Connected = true;
                    Settings.Default._Status = "#00BB00";
                    Settings.Default._StatusEffect = "#00FF00";
                }

                OffTimer.Interval = Settings.Default._TimeOut;

                DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(byte_values);

                sensorData.AxisX = (((byte_values[54] & 0xF) << 6) + ((byte_values[55] & 0xFC) >> 2)) & 0x3FF;
                sensorData.AxisY = (((byte_values[55] & 0x3) << 8) + ((byte_values[56] & 0xFF) >> 0)) & 0x3FF;
                if (User.Default.Accel)
                {
                    sensorData.AccelX = (int)(((byte_values[4] << 8) + byte_values[5]) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelY = (int)(((byte_values[6] << 8) + byte_values[7]) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelZ = (int)(((byte_values[8] << 8) + byte_values[9]) * 10000.0 * 9.80665 / 2048.0);
                }
                if (User.Default.Gyro)
                {
                    sensorData.GyroX = (int)(((byte_values[10] << 8) + byte_values[11]) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroY = (int)(((byte_values[12] << 8) + byte_values[13]) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroZ = (int)(((byte_values[14] << 8) + byte_values[15]) * 10000.0 * 0.017453292 / 14.285);
                }
                if (User.Default.Magnet)
                {
                    sensorData.MagnetX = (int)(((byte_values[32] << 8) + byte_values[33]) * 0.06);
                    sensorData.MagnetY = (int)(((byte_values[34] << 8) + byte_values[35]) * 0.06);
                    sensorData.MagnetZ = (int)(((byte_values[36] << 8) + byte_values[37]) * 0.06);
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

                //Buttons
                TouchpadButton();
                TriggerButton();
                HomeButton();
                BackButton();
                VolumeUpButton();
                VolumeDownButton();

                if (Settings.Default._NoButton)
                {
                    Settings.Default._VolDownBtn = true;
                    Settings.Default._VolUpBtn = true;
                    Settings.Default._BackBtn = true;
                    Settings.Default._HomeBtn = true;
                    Settings.Default._TchBtn = true;
                    Settings.Default._TrigBtn = true;
                }

                TrackPad();
            }

        }
        private static async void SendToCharacteristic(byte[] value)
        {
            if (write_characteristic == null) return;
            try
            {
                await write_characteristic.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(value));
            }
            catch (Exception e)
            {
                Debug.WriteLine("INFO: write to characteristic " + e.Message);
            }
        }
        private static void SendKeepAlive()
        {
            /*if (DateTime.Now > MainProgram.time):
				self.__time = round(time.time()) + 10*/
            //SendToCharacteristic(new byte[] {0x04, 0x00}, 4);
        }
        public void Disconnect()
        {
            SendToCharacteristic(new byte[] { 0x00, 0x00 });

            device?.Dispose();
            device = null;
            notify_characteristic?.Service?.Dispose();
            notify_characteristic = null;
            write_characteristic = null;
        }
        private async Task Get_Characteristics(GattDeviceService myService)
        {
            var accessRequestResponse = await myService.RequestAccessAsync();
            GattCharacteristicsResult CharResult = await myService.GetCharacteristicsAsync();// GetCharacteristicsAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached)
            if (CharResult.Status == GattCommunicationStatus.Success)
            {
                foreach (GattCharacteristic c in CharResult.Characteristics)
                {
                    if (c.Uuid == guid_controller_write)
                    {
                        write_characteristic = c;
                        continue;
                    }
                    if (c.Uuid == guid_controller_notify)
                    {
                        notify_characteristic = c;
                        continue;
                    }
                }
                if (write_characteristic != null && notify_characteristic != null) try
                    {
                        SendToCharacteristic(new byte[] { 0x01, 0x00 }); //enable sending data
                        SendToCharacteristic(new byte[] { 0x06, 0x00 });
                        SendToCharacteristic(new byte[] { 0x07, 0x00 });
                        SendToCharacteristic(new byte[] { 0x08, 0x00 }); //enable VR mode, high frequency event data update
                        // Write the ClientCharacteristicConfigurationDescriptor in order for server to send notifications.               
                        var result = await notify_characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        if (result == GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine("Successfully registered for notifications");
                            notify_characteristic.ValueChanged += SelectedCharacteristic_ValueChanged;
                            OffTimer.Start();
                            OffTimer.Elapsed += OffTimer_Elapsed;
                        }
                        else
                        {
                            Debug.WriteLine($"Error registering for notifications: {result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // This usually happens when not all characteristics are found
                        // or selected characteristic has no Notify.

                        Debug.Write(ex.ToString());
                        await Task.Delay(100);
                        await Get_Characteristics(myService); //try again !!! Add a max try counter to prevent infinite loop!!!!!!!
                    }
            }
            else
            {
                Debug.WriteLine($"Restricted service: Status={CharResult.Status} Can't read characteristics. Press Control+Shift+F5 before starting program again.");
            }
        }
        private void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            Debug.WriteLine("PairingRequestedHandler executed");
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' where there is no Windows Consent UX, you may want to provide your own confirmation.
                    Debug.WriteLine($"Windows.Devices.Enumeration.DevicePairingKinds.ConfirmOnly");
                    args.Accept();
                    break;

                case DevicePairingKinds.ProvidePin:
                    Debug.WriteLine($"Windows.Devices.Enumeration.DevicePairingKinds.ProvidePin");
                    var collectPinDeferral = args.GetDeferral();
                    string pin = "0000";
                    args.Accept(pin);
                    collectPinDeferral.Complete();
                    break;

                default:
                    Debug.WriteLine($"Windows.Devices.Enumeration.DevicePairingKinds = {args.PairingKind}");
                    break;
            }
        }
        async public void Pair_Connect() // https://stackoverflow.com/questions/35461817/uwp-bluetoothdevice-frombluetoothaddressasync-throws-0x80070002-INFO-on-no
        {
            if (device != null)
            {
                Disconnect();
                await Task.Delay(200);
            }
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(MAC802DOT3(User.Default.MACAddressText));
            DeviceInformation infDev = device.DeviceInformation; //We need this aux object to perform pairing
            DevicePairingKinds ceremoniesSelected = DevicePairingKinds.ConfirmOnly /*| Windows.Devices.Enumeration.DevicePairingKinds.ProvidePin*/; //Only confirm pairing, we'll provide PIN from app
            DevicePairingProtectionLevel protectionLevel = DevicePairingProtectionLevel.None; //Encryption; //Encrypted connection
            DeviceInformationCustomPairing customPairing = infDev.Pairing.Custom; //Our app takes control of pairing, not OS
            customPairing.PairingRequested += PairingRequestedHandler; //Our pairing request handler
            Debug.WriteLine("PairingRequestedHandler registered");
            DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel); //launch pairing
            customPairing.PairingRequested -= PairingRequestedHandler;
            Debug.WriteLine("PairingRequestedHandler unregistered");
            if ((result.Status == DevicePairingResultStatus.Paired) || (result.Status == DevicePairingResultStatus.AlreadyPaired))
            {
                GattDeviceServicesResult serviceResult = null;
                if (device != null)
                {
                    int servicesCount = 3;//Fill in the amount of services from your device!!!!!
                    int tryCount = 0;
                    bool connected = false;
                    while (!connected)//This is to make sure all services are found.
                    {
                        tryCount++;
                        serviceResult = await device.GetGattServicesAsync();
                        if (serviceResult.Status == GattCommunicationStatus.Success && serviceResult.Services.Count >= servicesCount)
                        {
                            connected = true;
                            Debug.WriteLine("Connected in " + tryCount + " tries");
                        }
                        if (tryCount > 15)//make this larger if faild
                        {
                            Debug.WriteLine("Failed to connect to device");
                            return;
                        }
                    }
                    if (connected)
                    {
                        for (int i = 0; i < serviceResult.Services.Count; i++)
                        {
                            var service = serviceResult.Services[i];
                            //This must be the service that contains the Gatt-Characteristic you want to read from or write to !!!!!!!.
                            if (service.Uuid.ToString() == "4f63756c-7573-2054-6872-65656d6f7465")
                            {
                                Debug.WriteLine("Get_Characteristics(service);");
                                await Get_Characteristics(service);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine($"Pairing failed, status={result.Status}");
            }
        }
    }
}

