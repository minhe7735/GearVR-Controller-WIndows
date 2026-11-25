using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;

namespace GearVR_Controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TaskbarIcon tray;
        private readonly DispatcherTimer pointerTimer;
        private readonly DispatcherTimer reconnectTimer;
        private CancellationTokenSource connectionAttempt;
        private CancellationTokenSource discoveryAttempt;
        private bool reconnectEnabled;
        private bool exiting;
        private bool startupReady;
        private const string StartupValue = "GearVR Controller";
        private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = SensorData.GetInstance();
            tray = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico")),
                ToolTipText = "Gear VR Controller"
            };
            var menu = new ContextMenu();
            var show = new MenuItem { Header = "Abrir" };
            show.Click += (s, e) => ShowWindow();
            var exit = new MenuItem { Header = "Salir" };
            exit.Click += (s, e) => { exiting = true; Close(); };
            menu.Items.Add(show);
            menu.Items.Add(exit);
            tray.ContextMenu = menu;
            tray.TrayMouseDoubleClick += (s, e) => ShowWindow();
            StateChanged += Window_StateChanged;
            Application.Current.SessionEnding += (s, e) => { exiting = true; User.Default.Save(); MainProgram.GetInstance().Disconnect(); tray.Dispose(); };
            using (var key = Registry.CurrentUser.OpenSubKey(StartupKey))
                StartupCheckBox.IsChecked = string.Equals(key?.GetValue(StartupValue) as string, "\"" + Path.Combine(AppContext.BaseDirectory, "GearVR Controller.exe") + "\" --startup", StringComparison.OrdinalIgnoreCase);
            startupReady = true;
            pointerTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(8) };
            pointerTimer.Tick += (s, e) => MainProgram.GetInstance().RenderMotionPointer();
            pointerTimer.Start();
            reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            reconnectTimer.Tick += async (s, e) =>
            {
                if (Settings.Default._Connected && connectionAttempt == null) ConnectionMessage.Text = "Mando conectado.";
                if (reconnectEnabled && User.Default.AutoConnect && !Settings.Default._Connected && connectionAttempt == null && discoveryAttempt == null)
                {
                    if (string.IsNullOrWhiteSpace(User.Default.MACAddressText) && DetectedDevices.Visibility != Visibility.Visible)
                        await DetectControllersAsync(false);
                    if (!string.IsNullOrWhiteSpace(User.Default.MACAddressText)) await ConnectAsync();
                }
            };
            reconnectTimer.Start();
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "--startup"))
            {
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        private void ShowWindow()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized) Hide();
            reconnectEnabled = User.Default.AutoConnect;
            if (string.IsNullOrWhiteSpace(User.Default.MACAddressText)) await DetectControllersAsync(false);
            if (reconnectEnabled && !string.IsNullOrWhiteSpace(User.Default.MACAddressText)) await ConnectAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!exiting)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                return;
            }
            reconnectEnabled = false;
            connectionAttempt?.Cancel();
            discoveryAttempt?.Cancel();
            reconnectTimer.Stop();
            pointerTimer.Stop();
            User.Default.Save();
            MainProgram.GetInstance().Disconnect();
            tray.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) { Hide(); ShowInTaskbar = false; }
        }

        private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!startupReady) return;
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(StartupKey);
                if (StartupCheckBox.IsChecked == true)
                {
                    string executable = Path.Combine(AppContext.BaseDirectory, "GearVR Controller.exe");
                    if (!File.Exists(executable)) throw new FileNotFoundException("No se encontr\u00f3 el ejecutable de la aplicaci\u00f3n.");
                    key.SetValue(StartupValue, "\"" + executable + "\" --startup");
                }
                else key.DeleteValue(StartupValue, false);
            }
            catch (Exception ex)
            {
                startupReady = false;
                StartupCheckBox.IsChecked = !(StartupCheckBox.IsChecked == true);
                startupReady = true;
                MessageBox.Show(this, ex.Message, "Inicio con Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SensitivitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded) Dispatcher.BeginInvoke(new Action(() => User.Default.Save()));
        }

        private void ScrollWheelSpeed_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("ScrollWheelSpeed");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }
        private void ScrollWheelSeg_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("ScrollWheelSeg");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }
        private void TouchTopButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TouchTopButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TouchRightButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TouchRightButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TouchBotButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TouchBotButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TouchLeftButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TouchLeftButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TouchMidButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TouchMidButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("BackButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("HomeButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void VolumeUpButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("VolumeUpButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void VolumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("VolumeDownButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerTopButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerTopButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerRightButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerRightButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerBotButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerBotButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerLeftButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerLeftButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void TriggerMidButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("TriggerMidButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private async void DetectButton_Click(object sender, RoutedEventArgs e)
        {
            await DetectControllersAsync(true);
        }

        private async Task DetectControllersAsync(bool selectSingle)
        {
            if (discoveryAttempt != null) return;
            using var attempt = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            discoveryAttempt = attempt;
            DetectButton.IsEnabled = false;
            ConnectionMessage.Text = "Buscando mandos Gear VR...";
            try
            {
                var devices = await ControllerDiscovery.FindAsync(attempt.Token);
                DetectedDevices.ItemsSource = devices;
                DetectedDevices.Visibility = devices.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                if (devices.Count == 1)
                {
                    if (selectSingle || string.IsNullOrWhiteSpace(User.Default.MACAddressText))
                    {
                        User.Default.MACAddressText = devices[0];
                        User.Default.Save();
                    }
                    ConnectionMessage.Text = "Mando detectado: " + devices[0];
                }
                else if (devices.Count > 1)
                {
                    DetectedDevices.SelectedIndex = -1;
                    ConnectionMessage.Text = "Selecciona tu mando de la lista.";
                    DetectedDevices.IsDropDownOpen = IsVisible;
                }
                else ConnectionMessage.Text = "No se encontro ningun mando. Despiertalo y vuelve a buscar.";
            }
            catch (OperationCanceledException) { ConnectionMessage.Text = "La busqueda tardo demasiado. Intentalo de nuevo."; }
            catch (Exception ex) { ConnectionMessage.Text = "No se pudo buscar el mando: " + ex.Message; }
            finally { discoveryAttempt = null; DetectButton.IsEnabled = true; }
        }

        private async void DetectedDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DetectedDevices.SelectedItem is string address)
            {
                User.Default.MACAddressText = address;
                User.Default.Save();
                ConnectionMessage.Text = "Mando seleccionado: " + address;
                if (reconnectEnabled && User.Default.AutoConnect && discoveryAttempt == null) await ConnectAsync();
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(User.Default.MACAddressText)) await DetectControllersAsync(true);
            if (string.IsNullOrWhiteSpace(User.Default.MACAddressText)) return;
            User.Default.Save();
            reconnectEnabled = true;
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (connectionAttempt != null || discoveryAttempt != null || string.IsNullOrWhiteSpace(User.Default.MACAddressText)) return;
            using var attempt = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            connectionAttempt = attempt;
            ConnectButton.IsEnabled = false;
            ConnectionMessage.Text = "Buscando el mando...";
            try
            {
                await MainProgram.GetInstance().Pair_Connect(attempt.Token);
                if (attempt.IsCancellationRequested) return;
                ConnectionMessage.Text = "Conectado. Esperando datos del mando...";
            }
            catch (OperationCanceledException)
            {
                ConnectionMessage.Text = !reconnectEnabled ? "Desconectado." : User.Default.AutoConnect
                    ? "Sin respuesta. Volveremos a intentarlo automaticamente."
                    : "Sin respuesta. Despierta el mando y vuelve a intentar.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ConnectionMessage.Text = reconnectEnabled && User.Default.AutoConnect
                    ? "Mando no disponible. Volveremos a intentarlo autom\u00e1ticamente."
                    : "No se pudo conectar: " + ex.Message;
            }
            finally
            {
                if (ReferenceEquals(connectionAttempt, attempt)) connectionAttempt = null;
                ConnectButton.IsEnabled = true;
            }
        }

        private void PowerOffButton_Click(object sender, RoutedEventArgs e)
        {
            reconnectEnabled = false;
            connectionAttempt?.Cancel();
            MainProgram.GetInstance().Disconnect();
            ConnectionMessage.Text = "Desconectado.";
        }

        private async void AutoConnect_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            User.Default.AutoConnect = CheckBoxAutoConnect.IsChecked == true;
            User.Default.Save();
            reconnectEnabled = User.Default.AutoConnect;
            if (reconnectEnabled && !Settings.Default._Connected) await ConnectAsync();
        }

        private void MotionPointerCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
            MainProgram.GetInstance().ResetMotionPointer();
        }

        private void AxisCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
        }

        private void GyroCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
        }

        private void MagnetCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
        }

        private void AccelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
        }
    }

}
