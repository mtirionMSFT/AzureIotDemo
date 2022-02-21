namespace AzureIoTDemo.Model
{
    using CommandLine;

    internal class Parameters
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; } = string.Empty;

        [Option(
            'd',
            "DeviceId",
            Required = true,
            HelpText = "The registration Id when using individual enrollment, or the desired device Id when using group enrollment.")]
        public string Id { get; set; } = string.Empty;

        [Option(
            'k',
            "Key",
            Required = true,
            HelpText = "The key of the individual enrollment or the derived primary key of the group enrollment.")]
        public string Key { get; set; } = string.Empty;
    }
}
