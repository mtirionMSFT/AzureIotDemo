namespace AzureIoTDemo
{
    using CommandLine;
    using AzureIoTDemo.Model;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Provisioning.Client;
    using Microsoft.Azure.Devices.Provisioning.Client.Transport;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using System.Text;

    internal class Program
    {
        static void Main(string[] args)
        {
            #region Handle parameters from command line
            Parameters parameters = new Parameters();
            Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(p => parameters = p)
                .WithNotParsed(e => Environment.Exit(1));
            #endregion

            // read local config
            string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            AppSettings settings = TryReadLocalConfiguration(configPath);

            if (string.IsNullOrEmpty(settings.IotHubEndpoint))
            {
                // we don't have an IotHub registered, so connect to DPS to provision
                if (ConnectToDps(parameters, settings))
                {
                    #region log
                    Console.WriteLine("Saving in local settings.");
                    #endregion
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(settings));
                }
                else
                {
                    Environment.Exit(1);
                }
            }

            if (string.IsNullOrEmpty(settings.IotHubEndpoint))
            {
                // We still don't know IOT HUB settings, so stop processing
                Environment.Exit(1);
            }

            HubContext? context = null;
            try
            {
                context = ConnectToIotHub(settings, parameters);

                while (true)
                {
                    SendMessages(context);
                    #region log & input
                    Console.WriteLine("Press ENTER to send or type 'exit' to quit.");
                    string? input = Console.ReadLine();
                    if (input == "exit")
                    {
                        Console.WriteLine("Okay, we'll stop the loop.");
                        break;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region log error
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine("Removing local config. Please restart the app.");
                #endregion
                File.Delete(configPath);
                Environment.Exit(1);
            }
            finally
            {
                if (context != null && context.Device != null)
                {
                    #region log
                    Console.WriteLine("Closing connection");
                    #endregion
                    context.Device.CloseAsync().Wait();
                }
            }

            Console.WriteLine("Press ENTER ...");
            Console.ReadLine();
        }

        private static bool ConnectToDps(Parameters parameters, AppSettings settings)
        {
            #region log
            Console.WriteLine("Connecting to DPS to provision using a symmetric key.");
            Console.WriteLine($"Id: {parameters.Id}");
            Console.WriteLine($"Key: {parameters.Key}");
            #endregion
            var security = new SecurityProviderSymmetricKey(parameters.Id, parameters.Key, null);
            string GlobalDpsEndpoint = "global.azure-devices-provisioning.net";
            ProvisioningDeviceClient dpsClient = ProvisioningDeviceClient.Create(
                GlobalDpsEndpoint, parameters.IdScope, security, new ProvisioningTransportHandlerHttp());
            DeviceRegistrationResult result = dpsClient.RegisterAsync().Result;
            #region log
            Console.WriteLine($"DPS Registration returned {result.Status}");
            #endregion

            if (result.Status == ProvisioningRegistrationStatusType.Assigned)
            {
                // Store the returned information in the settings
                settings.IotHubEndpoint = result.AssignedHub;
                settings.DeviceId = result.DeviceId;
                #region log
                Console.WriteLine($"DeviceId: {settings.DeviceId}");
                Console.WriteLine($"IoT Hub: {settings.IotHubEndpoint}\n");
                #endregion
                return true;
            }
            else
            {
                #region log
                Console.WriteLine("Unexpected result.");
                Console.WriteLine(result.JsonPayload + "\n");
                #endregion
                return false;
            }
        }

        private static HubContext ConnectToIotHub(AppSettings settings, Parameters parameters)
        {
            HubContext context = new HubContext();

            string hubConnectionString =
                $"HostName={settings.IotHubEndpoint};DeviceId={settings.DeviceId};SharedAccessKey={parameters.Key}";
            #region log
            Console.WriteLine(hubConnectionString);
            #endregion

            context.Device = DeviceClient.CreateFromConnectionString(hubConnectionString);
            context.Device.OpenAsync().Wait();

            // Get the twin for settings
            Twin twin = context.Device.GetTwinAsync().GetAwaiter().GetResult();
            context.DesiredProperties = twin.Properties.Desired;
            // Also register a handler for changes in settings
            context.Device.SetDesiredPropertyUpdateCallbackAsync(OnTwinChanged, context);

            #region log
            Console.WriteLine("Content of the device twin:");
            Console.WriteLine(twin.ToJson());
            #endregion
            context.MessageId = 1;
            if (twin.Properties.Reported.Contains("messageId"))
            {
                context.MessageId = (int)twin.Properties.Reported["messageId"];
            }

            #region log
            Console.WriteLine("");
            #endregion
            return context;
        }

        private static void SendMessages(HubContext context)
        {
            Random rand = new Random();

            // determine number of messages from desired properties in Device Twin 
            int max = (int)context.DesiredProperties["maxMessages"];
            #region log
            Console.WriteLine($"Sending {max} messages.");
            #endregion

            for (int i = 0; i < max; i++)
            {
                SendSingleMessage(context);
                Task.Delay(1000).Wait();
            }

            // update reported properties in Twin
            TwinCollection reported = new TwinCollection();
            reported["messageId"] = context.MessageId;
            context.Device.UpdateReportedPropertiesAsync(reported);
            #region log
            Console.WriteLine("Device twin updated.\n");
            #endregion
        }

        private static void SendSingleMessage(HubContext context)
        {
            var temperature = context.Rand.Next(20, 35);
            var humidity = context.Rand.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = context.MessageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };

            eventMessage.Properties.Add("temperatureAlert", (temperature > 30) ? "true" : "false");
            #region log
            Console.WriteLine($"Sending message { eventMessage.MessageId } for {messagePayload}");
            #endregion
            context.Device.SendEventAsync(eventMessage).Wait();
            context.MessageId++;
        }

        private static AppSettings TryReadLocalConfiguration(string configPath)
        {
            if (File.Exists(configPath))
            {
                #region log
                Console.WriteLine("Reading configuration");
                #endregion
                string json = File.ReadAllText(configPath);
                AppSettings settings = JsonConvert.DeserializeObject<AppSettings>(json);
                #region log
                Console.WriteLine("Configuration read.");
                Console.WriteLine($"DeviceId: {settings.DeviceId}");
                Console.WriteLine($"IoT Hub: {settings.IotHubEndpoint}\n");
                #endregion
                return settings;
            }

            return new AppSettings();
        }

        private static Task OnTwinChanged(TwinCollection desiredProperties, object userContext)
        {
            HubContext context = (HubContext)userContext;
            #region log
            Console.WriteLine($"\nReceived an update of desired settings.");
            Console.WriteLine($"maxMessage was {context.DesiredProperties["maxMessages"]} and will be {desiredProperties["maxMessages"]}\n");
            #endregion
            context.DesiredProperties = desiredProperties;
            return Task.CompletedTask;
        }
    }
}
