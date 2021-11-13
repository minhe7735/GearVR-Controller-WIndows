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



namespace GearVR_Controller
{
    public class MainProgram
    {
        SensorData sensorData = SensorData.GetInstance();

        private static MainProgram instance = new();
        private MainProgram() { }
        public static MainProgram getInstance() { return instance; }
        public static ulong MAC802DOT3(string macAddress) //https://stackoverflow.com/a/50519302
        {
            string hex = macAddress.Replace(":", "");
            return Convert.ToUInt64(hex, 16);
        }
        private static void sendInput(string input)
        {
            string[] words = input.Split(' ');
            switch (words[0])
            {
                case "SWheelUP":
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This Mouse wheel"], 0, 0, Settings.Default.scrollWheel, 0);
                    break;
                case "SWheelDOWN":
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This Mouse wheel"], 0, 0, -Settings.Default.scrollWheel, 0);
                    break;
                case "Toggle":
                    Settings.Default._useWheel = !Settings.Default._useWheel;
                    break;
                case "X1":

                case "X2":
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This X DOWN"], 0, 0, (byte)KeyCodeCollection.GetKeyCodes()[input], 0);
                    mouse_event(KeyCodeCollection.GetKeyCodes()["--Don't Use This X UP"], 0, 0, (byte)KeyCodeCollection.GetKeyCodes()[input], 0);
                    break;
                case "Mouse":
                    mouse_event((byte)KeyCodeCollection.GetKeyCodes()["--Don't Use This " + input + " DOWN"], 0, 0, 0, 0);
                    mouse_event((byte)KeyCodeCollection.GetKeyCodes()["--Don't Use This " + input + " UP"], 0, 0, 0, 0);
                    break;
                case "Launch:":
                    Process P = new();
                    P.StartInfo.UseShellExecute = true;
                    P.StartInfo.FileName = input[(words[0].Length + 1)..];
                    P.Start();
                    break;
                default:
                    keybd_event((byte)KeyCodeCollection.GetKeyCodes()[input], 0, 1 | 0, 0);
                    keybd_event((byte)KeyCodeCollection.GetKeyCodes()[input], 0, 1 | 2, 0);
                    break;
            }
        }

        public static readonly Guid guid_controller_data_service = Guid.Parse("4f63756c-7573-2054-6872-65656d6f7465");
        public static readonly Guid guid_controller_setup_characteristic = Guid.Parse("c8c51726-81bc-483b-a052-f7a14ea3d282");
        public static readonly Guid guid_controller_data_characteristic = Guid.Parse("c8c51726-81bc-483b-a052-f7a14ea3d281");

        private static GattCharacteristic setup_characteristic = null;
        private static GattCharacteristic data_characteristic = null;
        private static BluetoothLEDevice device = null;

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        // # circle segments from 0 .. self.Settings.Default._numberOfWheelPositions clockwise
        private static int fwheelPos(int x, int y)
        {
            int pos = 0;
            if (x == 0 && y == 0) { pos = -1; }
            Complex cnum = new(x - 157, y - 157);
            pos = (int)Math.Floor((cnum.Phase * 180 / Math.PI) / 360.0 * Settings.Default._numberOfWheelPositions);
            return pos;
        }

        private byte[] byte_values = new byte[60];
        private void SelectedCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (byte_values.Length == args.CharacteristicValue.Length)
            {

                DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(byte_values);

                sensorData.AxisX = (((byte_values[54] & 0xF) << 6) + ((byte_values[55] & 0xFC) >> 2)) & 0x3FF;
                sensorData.AxisY = (((byte_values[55] & 0x3) << 8) + ((byte_values[56] & 0xFF) >> 0)) & 0x3FF;
                if (Settings.Default.Accel)
                {
                    sensorData.AccelX = (int)(((byte_values[4] << 8) + byte_values[5]) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelY = (int)(((byte_values[6] << 8) + byte_values[7]) * 10000.0 * 9.80665 / 2048.0);
                    sensorData.AccelZ = (int)(((byte_values[8] << 8) + byte_values[9]) * 10000.0 * 9.80665 / 2048.0);
                }
                if (Settings.Default.Gyro)
                {
                    sensorData.GyroX = (int)(((byte_values[10] << 8) + byte_values[11]) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroY = (int)(((byte_values[12] << 8) + byte_values[13]) * 10000.0 * 0.017453292 / 14.285);
                    sensorData.GyroZ = (int)(((byte_values[14] << 8) + byte_values[15]) * 10000.0 * 0.017453292 / 14.285);
                }
                if (Settings.Default.Magnet)
                {
                    sensorData.MagnetX = (int)(((byte_values[32] << 8) + byte_values[33]) * 0.06);
                    sensorData.MagnetY = (int)(((byte_values[34] << 8) + byte_values[35]) * 0.06);
                    sensorData.MagnetZ = (int)(((byte_values[36] << 8) + byte_values[37]) * 0.06);
                }

                Settings.Default._triggerButton = ((byte_values[58] & 1) == 1);
                Settings.Default._homeButton = ((byte_values[58] & 2) == 2);
                Settings.Default._backButton = ((byte_values[58] & 4) == 4);
                Settings.Default._touchpadButton = ((byte_values[58] & 8) == 8);
                Settings.Default._volumeUpButton = ((byte_values[58] & 16) == 16);
                Settings.Default._volumeDownButton = ((byte_values[58] & 32) == 32);
                Settings.Default._NoButton = ((byte_values[58] & 64) == 64);

                int X = (sensorData.AxisX - 157) + (sensorData.AxisY - 157);
                int Y = sensorData.AxisY - sensorData.AxisX;

                // touchPad buttons
                if ( Math.Pow(sensorData.AxisX - 157, 2)  + Math.Pow(sensorData.AxisY - 157, 2) <= 6400  && Settings.Default._touchpadButton && Settings.Default._tchbtn)
                {
                    sendInput(Settings.Default.touchMidButton);
                    Settings.Default._tchbtn = false;
                }
                else if (X < 0 && Y >= 0 && Settings.Default._touchpadButton && Settings.Default._tchbtn)
                {
                    sendInput(Settings.Default.touchLeftButton);
                    Settings.Default._tchbtn = false;
                }
                else if (X > 0 && Y <= 0 && Settings.Default._touchpadButton && Settings.Default._tchbtn)
                {
                    sendInput(Settings.Default.touchRightButton);
                    Settings.Default._tchbtn = false;
                }
                else if (X > 0 && Y >= 0 && Settings.Default._touchpadButton && Settings.Default._tchbtn)
                {
                    sendInput(Settings.Default.touchBotButton);
                    Settings.Default._tchbtn = false;
                }
                else if (X < 0 && Y <= 0 && Settings.Default._touchpadButton && Settings.Default._tchbtn)
                {
                    sendInput(Settings.Default.touchTopButton);
                    Settings.Default._tchbtn = false;
                }
                else if (!Settings.Default._touchpadButton && !Settings.Default._tchbtn)
                {
                    Settings.Default._tchbtn = true;
                }

                // trigger buttons
                if (sensorData.AxisX == 0 && sensorData.AxisY == 0 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerButton);
                    Settings.Default._trig = false;
                }
                else if (Math.Pow(sensorData.AxisX - 157, 2) + Math.Pow(sensorData.AxisY - 157, 2) <= 6400 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerMidButton);
                    Settings.Default._trig = false;
                }
                else if (X < 0 && Y >= 0 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerLeftButton);
                    Settings.Default._trig = false;
                }
                else if (X > 0 && Y <= 0 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerRightButton);
                    Settings.Default._trig = false;
                }
                else if (X > 0 && Y >= 0 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerBotButton);
                    Settings.Default._trig = false;
                }
                else if (X < 0 && Y <= 0 && Settings.Default._triggerButton && Settings.Default._trig)
                {
                    sendInput(Settings.Default.triggerTopButton);
                    Settings.Default._trig = false;
                }
                else if (!Settings.Default._triggerButton && !Settings.Default._trig)
                {
                    Settings.Default._trig = true;
                }


                sensorData.WheelPos = fwheelPos(sensorData.AxisX, sensorData.AxisY);

                sensorData.Delta_X = sensorData.AxisX - sensorData.__AxisX;
                sensorData.Delta_Y = sensorData.AxisY - sensorData.__AxisY;
                sensorData.Delta_X = (int)Math.Round(sensorData.Delta_X * 1.2);
                sensorData.Delta_Y = (int)Math.Round(sensorData.Delta_Y * 1.2);

                if (Settings.Default._useWheel)
                {
                    if (Math.Abs(sensorData._WheelPos - sensorData.WheelPos) > 1 && Math.Abs((sensorData._WheelPos + 1) % Settings.Default._numberOfWheelPositions - (sensorData.WheelPos + 1) % Settings.Default._numberOfWheelPositions) > 1)
                    {
                        sensorData._WheelPos = sensorData.WheelPos;
                        return;
                    }
                    if ((sensorData._WheelPos - sensorData.WheelPos) == 1 || ((sensorData._WheelPos + 1) % Settings.Default._numberOfWheelPositions - (sensorData.WheelPos + 1) % Settings.Default._numberOfWheelPositions) == 1)
                    {
                        sensorData._WheelPos = sensorData.WheelPos;
                        sendInput("SWheelUP");
                        return;
                    }
                    if ((sensorData.WheelPos - sensorData._WheelPos) == 1 || ((sensorData.WheelPos + 1) % Settings.Default._numberOfWheelPositions - (sensorData._WheelPos + 1) % Settings.Default._numberOfWheelPositions) == 1)
                    {
                        sensorData._WheelPos = sensorData.WheelPos;
                            sendInput("SWheelDOWN");
                        return;
                    }
                    return;
                }

                //TODO
                //if (Settings.Default._triggerButton && !Settings.Default._triggerButton_latch) { sendInput(Settings.Default.triggerButton); Settings.Default._triggerButton_latch = true; }
                //else if (!Settings.Default._triggerButton && Settings.Default._triggerButton_latch) { Settings.Default._triggerButton_latch = false; Debug.WriteLine("released"); }

                if (Settings.Default._homeButton && Settings.Default._volbtn)
                {
                    sendInput(Settings.Default.homeButton);
                    Settings.Default._volbtn = false;
                    return;
                }

                if (Settings.Default._backButton && Settings.Default._volbtn)
                {
                    sendInput(Settings.Default.backButton);

                    Settings.Default._volbtn = false;
                    return;
                }

                if (Settings.Default._volumeDownButton && Settings.Default._volbtn)
                {
                    sendInput(Settings.Default.volumeDownButton);
                    Settings.Default._volbtn = false;
                    return;
                }

                if (Settings.Default._volumeUpButton && Settings.Default._volbtn)
                {
                    sendInput(Settings.Default.volumeUpButton);
                    Settings.Default._volbtn = false;
                    return;
                }

                if (Settings.Default._NoButton)
                {
                    Settings.Default._volbtn = true;
                    Settings.Default._tchbtn = true;
                    Settings.Default._trig = true;
                }

                // No standalone button handling behind this point

                if (sensorData.AxisX == 0 && sensorData.AxisY == 0)
                {
                    Settings.Default._reset = true;
                    return;
                }

                if (Settings.Default._reset)
                {
                    Settings.Default._reset = false;
                    sensorData.__AxisX = sensorData.AxisX;
                    sensorData.__AxisY = sensorData.AxisY;
                    sensorData.__AltX = sensorData.GyroX;
                    sensorData.__AltY = sensorData.GyroY;
                    return;
                }

                mouse_event(0x0001, sensorData.Delta_X, sensorData.Delta_Y, 0, 0);

                sensorData.__AxisX = sensorData.AxisX;
                sensorData.__AxisY = sensorData.AxisY;
                sensorData.__AltX = sensorData.GyroX;
                sensorData.__AltY = sensorData.GyroY;
            }

        }
        private async void SendToCharacteristic(byte[] value)
        {
            if (setup_characteristic == null) return;
            try
            {
                await setup_characteristic.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(value)); 
            }
            catch (Exception e)
            {
                Debug.WriteLine("INFO: write to characteristic " + e.Message);
            }
        }
        private void SendKeepAlive()
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
            data_characteristic?.Service?.Dispose();
            data_characteristic = null;
            setup_characteristic = null;
        }
        private async Task Get_Characteristics(GattDeviceService myService)
        {
            var accessRequestResponse = await myService.RequestAccessAsync();
            GattCharacteristicsResult CharResult = await myService.GetCharacteristicsAsync();// GetCharacteristicsAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached)
            if (CharResult.Status == GattCommunicationStatus.Success)
            {
                foreach (GattCharacteristic c in CharResult.Characteristics)
                {
                    if (c.Uuid == guid_controller_setup_characteristic)
                    {
                        setup_characteristic = c;
                        continue;
                    }
                    if (c.Uuid == guid_controller_data_characteristic)
                    {
                        data_characteristic = c;
                        continue;
                    }
                }
                if (setup_characteristic != null && data_characteristic != null) try
                    {
                        SendToCharacteristic(new byte[] { 0x01, 0x00 }); //enable sending data
                        SendToCharacteristic(new byte[] { 0x06, 0x00 });
                        SendToCharacteristic(new byte[] { 0x07, 0x00 });
                        SendToCharacteristic(new byte[] { 0x08, 0x00 }); //enable VR mode, high frequency event data update
                        // Write the ClientCharacteristicConfigurationDescriptor in order for server to send notifications.               
                        var result = await data_characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        if (result == GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine("Successfully registered for notifications");
                            data_characteristic.ValueChanged += SelectedCharacteristic_ValueChanged;

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
        private async void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
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
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(MAC802DOT3(Settings.Default.macAddressText));
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

