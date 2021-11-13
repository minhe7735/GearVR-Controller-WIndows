using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System;

namespace GearVR_Controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SensorData sensorData = SensorData.GetInstance();
            this.DataContext = sensorData;

            this.StateChanged += new EventHandler(Window_StateChanged);
        }
        void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    break;
                case WindowState.Minimized:
                    Application.Current.MainWindow.Hide();
                    TaskbarIcon tray = new();
                    tray.Icon = new System.Drawing.Icon("C:/Users/mh773/source/repos/GearVR Controller/GearVR Controller/logo.ico");
                    tray.ToolTipText = "GVR Controller";
                    tray.TrayMouseDoubleClick += (s, e) =>
                    {
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                        tray.Dispose();
                    };
                    break;
                case WindowState.Normal:

                    break;
            }
        }

        private void scrollWheel_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("scrollWheel");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }
        private void touchTopButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("touchTopButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void touchRightButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("touchRightButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void touchBotButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("touchBotButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void touchLeftButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("touchLeftButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void touchMidButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("touchMidButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("backButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void homeButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("homeButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void volumeUpButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("volumeUpButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void volumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("volumeDownButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerTopButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerTopButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerRightButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerRightButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerBotButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerBotButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerLeftButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerLeftButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void triggerMidButton_Click(object sender, RoutedEventArgs e)
        {
            Key changeBinding = new("triggerMidButton");
            changeBinding.Owner = this;
            changeBinding.ShowDialog();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            MainProgram gvrc = MainProgram.getInstance();
            gvrc.Pair_Connect();

        }

        private void powerOffButton_Click(object sender, RoutedEventArgs e)
        {
            MainProgram.getInstance().Disconnect();
        }

        private void axisCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void gyroCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void magnetCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        private void accelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }
    }

}
