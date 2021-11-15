using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GearVR_Controller
{
    /// <summary>
    /// Interaction logic for key.xaml
    /// </summary>
    public partial class Key : Window
    {

        public Key(string v)
        {
            InitializeComponent();
            Settings.Default.currentButton = v;
            Binding keyCodeAutoCompleteBoxBinding = new();
            keyCodeAutoCompleteBoxBinding.Source = User.Default;
            keyCodeAutoCompleteBoxBinding.Path = new PropertyPath(v);
            keyCodeAutoCompleteBoxBinding.Mode = BindingMode.TwoWay;
            keyCodeAutoCompleteBoxBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(KeyCodeAutoCompleteBox, AutoCompleteBox.TextProperty, keyCodeAutoCompleteBoxBinding);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (KeyCodeCollection.GetKeyCodes().ContainsKey(KeyCodeAutoCompleteBox.Text) && !KeyCodeAutoCompleteBox.Text.StartsWith("--"))
            {
                User.Default.Save();
                this.Close();
            }
            else if (Settings.Default.currentButton.StartsWith("ScrollWheel") && int.TryParse(KeyCodeAutoCompleteBox.Text, out _))
            {
                User.Default.Save();
                this.Close();
            }
            else if (KeyCodeAutoCompleteBox.Text.StartsWith("Launch:"))
            {
                User.Default.Save();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid Input");
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooseFile = new();
            chooseFile.Filter = "All Files (*.*)|*.*";
            chooseFile.DereferenceLinks = false;
            Nullable<bool> result = chooseFile.ShowDialog();
            if (result == true)
            {
                string filename = chooseFile.FileName;
                KeyCodeAutoCompleteBox.Text = "Launch: " + filename;
            }
        }
    }

}
