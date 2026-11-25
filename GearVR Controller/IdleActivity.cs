using System;
using System.Numerics;

namespace GearVR_Controller
{
    // Tracks actual controller activity rather than the arrival of BLE packets.
    internal sealed class IdleActivity
    {
        private Vector3 gravity;
        private bool hasGravity;
        private long lastActivity;
        private bool started;

        public void Reset(long timestamp)
        {
            lastActivity = timestamp;
            started = true;
            gravity = Vector3.Zero;
            hasGravity = false;
        }

        public void Stop() { started = false; hasGravity = false; }

        public void Observe(long timestamp, Vector3 acceleration, Vector3 gyro, bool button, bool touching)
        {
            if (!started) Reset(timestamp);
            if (!hasGravity && acceleration.LengthSquared() > 1) { gravity = acceleration; hasGravity = true; }
            // Sensor units: 2048 counts/g and 14.285 counts/(degree/s).
            // Thresholds ignore small stationary sensor noise while counting deliberate motion.
            bool moved = gyro.LengthSquared() > 86f * 86f;
            if (hasGravity && acceleration.LengthSquared() > 1)
            {
                moved |= Vector3.DistanceSquared(acceleration, gravity) > 350f * 350f;
                gravity += (acceleration - gravity) * 0.02f;
            }
            if (button || touching || moved) lastActivity = timestamp;
        }

        public bool IsIdle(long timestamp, long frequency, TimeSpan timeout) =>
            started && timestamp >= lastActivity && (double)(timestamp - lastActivity) / frequency >= timeout.TotalSeconds;
    }
}
