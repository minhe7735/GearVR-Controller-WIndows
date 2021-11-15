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
                if (User.Default.Axis) OnPropertyChanged();
            }
        }

        private int axisY;
        public int AxisY
        {
            get { return axisY; }
            set
            {
                axisY = value;
                if (User.Default.Axis) OnPropertyChanged();
            }
        }

        private int accelX;
        public int AccelX
        {
            get { return accelX; }
            set
            {
                accelX = value;
                if (User.Default.Accel) OnPropertyChanged();
            }
        }

        private int accelY;
        public int AccelY
        {
            get { return accelY; }
            set
            {
                accelY = value;
                if (User.Default.Accel) OnPropertyChanged();
            }
        }

        private int accelZ;
        public int AccelZ
        {
            get { return accelZ; }
            set
            {
                accelZ = value;
                if (User.Default.Accel) OnPropertyChanged();
            }
        }

        private int gyroX;
        public int GyroX
        {
            get { return gyroX; }
            set
            {
                gyroX = value;
                if (User.Default.Gyro) OnPropertyChanged();
            }
        }

        private int gyroY;
        public int GyroY
        {
            get { return gyroY; }
            set
            {
                gyroY = value;
                if (User.Default.Gyro) OnPropertyChanged();
            }
        }

        private int gyroZ;
        public int GyroZ
        {
            get { return gyroZ; }
            set
            {
                gyroZ = value;
                if (User.Default.Gyro) OnPropertyChanged();
            }
        }

        private int magnetX;
        public int MagnetX
        {
            get { return magnetX; }
            set
            {
                magnetX = value;
                if (User.Default.Magnet) OnPropertyChanged();
            }
        }

        private int magnetY;
        public int MagnetY
        {
            get { return magnetY; }
            set
            {
                magnetY = value;
                if (User.Default.Magnet) OnPropertyChanged();
            }
        }

        private int magnetZ;
        public int MagnetZ
        {
            get { return magnetZ; }
            set
            {
                magnetZ = value;
                if (User.Default.Magnet) OnPropertyChanged();
            }
        }

        private int _axisX;
        public int _AxisX
        {
            get { return _axisX; }
            set { _axisX = value; }
        }

        private int _axisY;
        public int _AxisY
        {
            get { return _axisY; }
            set { _axisY = value; }
        }

        private int _altX;
        public int _AltX
        {
            get { return _altX; }
            set { _altX = value; }
        }

        private int _altY;
        public int _AltY
        {
            get { return _altY; }
            set { _altY = value; }
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

        private int shiftedX;
        public int ShiftedX
        {
            get { return shiftedX; }
            set { shiftedX = value; }
        }

        private int shiftedY;
        public int ShiftedY
        {
            get { return shiftedY; }
            set { shiftedY = value; }
        }
    }
}
