using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System;
using System.IO;

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


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect on start-up if needed
            if (User.Default.AutoConnect)
            {
                MainProgram gvrc = MainProgram.GetInstance();
                gvrc.Pair_Connect();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save user settings on exit
            User.Default.Save();
            
            MainProgram.GetInstance().Disconnect();
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
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
                    tray.Icon = new System.Drawing.Icon(iconPath);
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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            User.Default.Save();
            MainProgram gvrc = MainProgram.GetInstance();
            gvrc.Pair_Connect();

        }

        private void PowerOffButton_Click(object sender, RoutedEventArgs e)
        {
            MainProgram.GetInstance().Disconnect();
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
