using System;
using System.Runtime.InteropServices;

namespace Pancetta.Windows
{
    public enum ACLineStatus : byte
    {
        Offline = 0, Online = 1, Unknown = 255
    }

    public enum BatteryFlag : byte
    {
        High = 1,
        Low = 2,
        Critical = 4,
        Charging = 8,
        NoSystemBattery = 128,
        Unknown = 255
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PowerState
    {
        public ACLineStatus ACLineStatus;
        public BatteryFlag BatteryFlag;
        public Byte BatteryLifePercent;
        public Byte Reserved1;
        public Int32 BatteryLifeTime;
        public Int32 BatteryFullLifeTime;
    }

    public class BatteryStatus
    {
        [DllImport("Kernel32", EntryPoint = "GetSystemPowerStatus")]
        private static extern bool GetSystemPowerStatusRef(PowerState sps);

        public static bool OnBattery
        {
            get
            {
                var state = new PowerState();
                var ret = GetSystemPowerStatusRef(state);
                if (ret == true)
                {
                    return state.ACLineStatus == ACLineStatus.Offline;
                }

                return true;
            }
        }
    }
}
