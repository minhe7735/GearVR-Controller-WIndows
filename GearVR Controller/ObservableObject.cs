using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GearVR_Controller
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string sensorName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(sensorName));
        }
    }
}
