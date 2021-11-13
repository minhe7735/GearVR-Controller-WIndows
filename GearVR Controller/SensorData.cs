namespace GearVR_Controller
{
    public class SensorData : ObservableObject
    {
        private static SensorData instance = new();
        private SensorData() { }
        public static SensorData GetInstance() { return instance; }

        private int axisX;
        public int AxisX
        {
            get { return axisX; }
            set
            {
                axisX = value;
                if (Settings.Default.Axis) OnPropertyChanged();
            }
        }

        private int axisY;
        public int AxisY
        {
            get { return axisY; }
            set
            {
                axisY = value;
                if (Settings.Default.Axis) OnPropertyChanged();
            }
        }

        private int accelX;
        public int AccelX
        {
            get { return accelX; }
            set
            {
                accelX = value;
                if (Settings.Default.Accel) OnPropertyChanged();
            }
        }

        private int accelY;
        public int AccelY
        {
            get { return accelY; }
            set
            {
                accelY = value;
                if (Settings.Default.Accel) OnPropertyChanged();
            }
        }

        private int accelZ;
        public int AccelZ
        {
            get { return accelZ; }
            set
            {
                accelZ = value;
                if (Settings.Default.Accel) OnPropertyChanged();
            }
        }

        private int gyroX;
        public int GyroX
        {
            get { return gyroX; }
            set
            {
                gyroX = value;
                if (Settings.Default.Gyro) OnPropertyChanged();
            }
        }

        private int gyroY;
        public int GyroY
        {
            get { return gyroY; }
            set
            {
                gyroY = value;
                if (Settings.Default.Gyro) OnPropertyChanged();
            }
        }

        private int gyroZ;
        public int GyroZ
        {
            get { return gyroZ; }
            set
            {
                gyroZ = value;
                if (Settings.Default.Gyro) OnPropertyChanged();
            }
        }

        private int magnetX;
        public int MagnetX
        {
            get { return magnetX; }
            set
            {
                magnetX = value;
                if (Settings.Default.Magnet) OnPropertyChanged();
            }
        }

        private int magnetY;
        public int MagnetY
        {
            get { return magnetY; }
            set
            {
                magnetY = value;
                if (Settings.Default.Magnet) OnPropertyChanged();
            }
        }

        private int magnetZ;
        public int MagnetZ
        {
            get { return magnetZ; }
            set
            {
                magnetZ = value;
                if (Settings.Default.Magnet) OnPropertyChanged();
            }
        }

        private int __axisX;
        public int __AxisX
        {
            get { return __axisX; }
            set { __axisX = value; }
        }

        private int __axisY;
        public int __AxisY
        {
            get { return __axisY; }
            set { __axisY = value; }
        }

        private int __altX;
        public int __AltX
        {
            get { return __altX; }
            set { __altX = value; }
        }

        private int __altY;
        public int __AltY
        {
            get { return __altY; }
            set { __altY = value; }
        }

        private int wheelPos;
        public int WheelPos
        {
            get { return wheelPos; }
            set { wheelPos = value; }
        }

        private int _wheelPos;
        public int _WheelPos
        {
            get { return _wheelPos; }
            set { _wheelPos = value; }
        }

        private int delta_X;

        public int Delta_X
        {
            get { return delta_X; }
            set { delta_X = value; }
        }

        private int delta_Y;

        public int Delta_Y
        {
            get { return delta_Y; }
            set { delta_Y = value; }
        }


    }
}
