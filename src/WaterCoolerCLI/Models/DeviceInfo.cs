using WaterCoolerCLI.Invoke;

namespace WaterCoolerCLI.Models
{

    public class DeviceInfo
    {
        public uint VID { get; set; }

        public uint PID { get; set; }

        public string ModelName { get; set; }

        public int DeviceType { get; set; }

        public HidDriver HidDriver { get; set; }

        public GDriverInfo GDriverInfo { get; set; }
    }

}
