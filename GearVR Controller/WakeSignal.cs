using System;

namespace GearVR_Controller
{
    // A wake is only meaningful after the device has stopped advertising.
    internal sealed class WakeSignal
    {
        private readonly long quietPeriodMilliseconds;
        private long lastAdvertisement;
        private bool armed;
        private bool fired;

        public WakeSignal(long startedAt, long quietPeriodMilliseconds, bool alreadyQuiet = false)
        {
            lastAdvertisement = startedAt;
            this.quietPeriodMilliseconds = quietPeriodMilliseconds;
            armed = alreadyQuiet;
        }

        public bool CheckQuiet(long now)
        {
            if (armed || fired || now - lastAdvertisement < quietPeriodMilliseconds) return false;
            armed = true;
            return true;
        }

        public bool Advertisement(long now)
        {
            lastAdvertisement = now;
            if (!armed || fired) return false;
            fired = true;
            return true;
        }
    }
}
