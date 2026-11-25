using System;
using System.Numerics;

namespace GearVR_Controller
{
    // Integrates rotation relative to the last screen centre. Gravity supplies the
    // vertical axis, so the controller can be held at different roll angles.
    internal sealed class MotionPointer
    {
        private Vector3 gravity;
        private Vector3 gyroBias;
        private Vector3 calibrationSum;
        private int calibrationCount;
        private long lastTicks;
        private double x, y;
        private bool initialized;

        public void Reset()
        {
            gravity = Vector3.Zero;
            gyroBias = Vector3.Zero;
            calibrationSum = Vector3.Zero;
            calibrationCount = 0;
            lastTicks = 0;
            initialized = false;
        }

        public bool Initialized => initialized;
        public void SetPosition(double horizontal, double vertical)
        {
            x = Math.Clamp(horizontal, -0.75, 0.75);
            y = Math.Clamp(vertical, -0.50, 0.50);
            initialized = true;
        }

        public void BeginCalibration() { calibrationSum = Vector3.Zero; calibrationCount = 0; }

        public void Calibrate()
        {
            // Only use stationary samples; moving during recenter must not introduce bias.
            if (calibrationCount >= 10) gyroBias = calibrationSum / calibrationCount;
            x = y = 0;
            initialized = true;
        }

        public bool Update(Vector3 accel, Vector3 gyro, long ticks, long frequency, bool calibrating, double sensitivity, out double horizontal, out double vertical)
        {
            horizontal = x; vertical = y;
            if (accel.LengthSquared() > 0.01f)
            {
                Vector3 measured = Vector3.Normalize(accel);
                gravity = gravity.LengthSquared() < 0.01f ? measured : Vector3.Normalize(gravity * 0.95f + measured * 0.05f);
            }
            if (calibrating && gyro.Length() < 0.15f) { calibrationSum += gyro; calibrationCount++; }
            if (lastTicks == 0) { lastTicks = ticks; return false; }
            double dt = (double)(ticks - lastTicks) / frequency;
            lastTicks = ticks;
            if (!initialized || dt <= 0 || dt > 0.1 || gravity.LengthSquared() < 0.01f) return false;
            Vector3 horizontalAxis = Vector3.Cross(Vector3.UnitY, gravity);
            if (horizontalAxis.LengthSquared() < 0.04f) return false;
            horizontalAxis = Vector3.Normalize(horizontalAxis);
            Vector3 corrected = gyro - gyroBias;
            double yaw = -Vector3.Dot(corrected, gravity);
            double pitch = -Vector3.Dot(corrected, horizontalAxis);
            const double deadZone = 0.02;
            if (Math.Abs(yaw) < deadZone) yaw = 0;
            if (Math.Abs(pitch) < deadZone) pitch = 0;
            x = Math.Clamp(x + yaw * dt * sensitivity, -0.75, 0.75);
            y = Math.Clamp(y + pitch * dt * sensitivity, -0.50, 0.50);
            horizontal = x; vertical = y;
            return yaw != 0 || pitch != 0;
        }
    }
}
