using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;

namespace GearVR_Controller
{
    internal sealed class BluetoothRestartResult
    {
        public bool Success { get; init; }
        public bool RadioOn { get; init; }
        public string Message { get; init; }
    }

    internal interface IBluetoothRadioControl
    {
        RadioState State { get; }
        Task<RadioAccessStatus> SetStateAsync(RadioState state);
    }

    internal sealed class WindowsBluetoothRadio : IBluetoothRadioControl
    {
        private readonly Radio radio;
        public WindowsBluetoothRadio(Radio radio) { this.radio = radio; }
        public RadioState State => radio.State;
        public async Task<RadioAccessStatus> SetStateAsync(RadioState state) => await radio.SetStateAsync(state);
    }

    internal static class BluetoothRadioRestart
    {
        // Do not initiate radio changes when Bluetooth was already off or permission denied.
        public static async Task<BluetoothRestartResult> RestartAsync()
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync();
            if (adapter == null) return new BluetoothRestartResult { Message = "No se encontro un adaptador Bluetooth." };
            var radio = await adapter.GetRadioAsync();
            if (radio == null) return new BluetoothRestartResult { Message = "Windows no expuso el control del adaptador Bluetooth." };
            if (radio.State != RadioState.On)
                return new BluetoothRestartResult { Message = "Bluetooth ya estaba apagado o no disponible; no se modifico." };
            var access = await Radio.RequestAccessAsync();
            if (access != RadioAccessStatus.Allowed)
                return new BluetoothRestartResult { RadioOn = radio.State == RadioState.On, Message = "Windows denego el permiso para controlar Bluetooth: " + access };

            return await RestartControlledAsync(new WindowsBluetoothRadio(radio));
        }

        internal static async Task<BluetoothRestartResult> RestartControlledAsync(IBluetoothRadioControl radio)
        {
            if (radio.State != RadioState.On)
                return new BluetoothRestartResult { Message = "Bluetooth ya estaba apagado o no disponible; no se modifico." };
            bool offRequested = false;
            string failure = null;
            try
            {
                // Once Off is requested, always attempt to restore On even on failure.
                offRequested = true;
                var offResult = await radio.SetStateAsync(RadioState.Off);
                if (offResult != RadioAccessStatus.Allowed) failure = "Windows rechazo apagar Bluetooth: " + offResult;
                else if (!await WaitForStateAsync(radio, RadioState.Off)) failure = "Bluetooth no confirmo el apagado.";
            }
            catch (Exception ex) { failure = "No se pudo apagar Bluetooth: " + ex.Message; }
            finally
            {
                if (offRequested)
                {
                    try
                    {
                        var onResult = await radio.SetStateAsync(RadioState.On);
                        if (onResult != RadioAccessStatus.Allowed || !await WaitForStateAsync(radio, RadioState.On))
                            failure = "No se pudo restaurar Bluetooth. Activalo manualmente en Windows.";
                    }
                    catch (Exception) { failure = "No se pudo restaurar Bluetooth. Activalo manualmente en Windows."; }
                }
            }
            bool on = radio.State == RadioState.On;
            return new BluetoothRestartResult { Success = failure == null && on, RadioOn = on, Message = failure };
        }

        private static async Task<bool> WaitForStateAsync(IBluetoothRadioControl radio, RadioState target)
        {
            for (int i = 0; i < 50; i++)
            {
                if (radio.State == target) return true;
                await Task.Delay(200);
            }
            return radio.State == target;
        }
    }
}
