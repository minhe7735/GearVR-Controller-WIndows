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
            keyCodeAutoCompleteBoxBinding.Source = Settings.Default;
            keyCodeAutoCompleteBoxBinding.Path = new PropertyPath(v);
            keyCodeAutoCompleteBoxBinding.Mode = BindingMode.TwoWay;
            keyCodeAutoCompleteBoxBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(keyCodeAutoCompleteBox, AutoCompleteBox.TextProperty, keyCodeAutoCompleteBoxBinding);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (KeyCodeCollection.GetKeyCodes().ContainsKey(keyCodeAutoCompleteBox.Text) && !keyCodeAutoCompleteBox.Text.StartsWith("--"))
            {
                Settings.Default.Save();
                this.Close();
            }
            else if (Settings.Default.currentButton == "scrollWheel" && int.TryParse(keyCodeAutoCompleteBox.Text, out _) )
            {
                Settings.Default.Save();
                this.Close();
            }
            else if (keyCodeAutoCompleteBox.Text.StartsWith("Launch:"))
            {
                Settings.Default.Save();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid Input");
            }
        }

        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooseFile = new();
            chooseFile.Filter = "All Files (*.*)|*.*";
            chooseFile.DereferenceLinks = false;
            Nullable<bool> result = chooseFile.ShowDialog();
            if (result == true)
            {
                string filename = chooseFile.FileName;
                keyCodeAutoCompleteBox.Text = "Launch: " + filename;
            }
        }
    }

}
