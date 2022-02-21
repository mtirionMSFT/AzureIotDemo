namespace AzureIoTDemo.Model
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;

    internal class HubContext
    {
        public DeviceClient? Device { get; set; } = null;
        public TwinCollection DesiredProperties { get; set; } = new TwinCollection();
        public Random Rand { get; set; } = new Random();
        public int MessageId { get; set; }
    }
}