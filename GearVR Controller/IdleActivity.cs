using System;

namespace GearVR_Controller
{
    // BLE reports and motion sensor noise do not count as user activity.
    internal sealed class IdleActivity
    {
        private long lastActivity;
        private bool started;

        public void Reset(long timestamp) { lastActivity = timestamp; started = true; }
        public void Stop() { started = false; }

        public void Observe(long timestamp, bool button, bool touching)
        {
            if (!started) Reset(timestamp);
            if (button || touching) lastActivity = timestamp;
        }

        public bool IsIdle(long timestamp, long frequency, TimeSpan timeout) =>
            started && timestamp >= lastActivity && (double)(timestamp - lastActivity) / frequency >= timeout.TotalSeconds;
    }
}
