using ESystem.Logging;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using VPilotNetAlert.Settings;
using VPilotNetAlert.Tasks;
using VPilotNetAlert.Vatsim;

namespace VPilotNetAlert
{
  internal class Program
  {
    public const string SENDER = "VPilotNetAlert";
    private const string CONFIG_FILE_NAME = "settings.json";
    public static bool IsSupposedToClose { get; set; } = false;
    private static VPilotNetCoreModule.ClientProxyBroker broker = null!;
    private static ESimWrapper eSimWrapper = null!;
    private static readonly Logger logger = Logger.Create("VPilotNetAlert.Program");
    private static VatsimFlightPlanProvider vatsimDataProvider = null!;
    private static Config config = null!;

    static void Main(string[] args)
    {
      string pipeId = args.Length > 0 ? args[0] : string.Empty;
      logger.Log(LogLevel.INFO, $"Starting VPilotNetAlert with pipe ID '{pipeId}'");

      // load config
      LoadConfig();
      if (config == null)
      {
        logger.Log(LogLevel.ERROR, "Configuration is null after loading. Exiting.");
        return;
      }

      // init sim & broker
      eSimWrapper = new ESimWrapper();
      broker = new VPilotNetCoreModule.ClientProxyBroker(pipeId);

      // init Vatsim Flight Plan Provider
      logger.Log(LogLevel.INFO, "Initializing VATSIM flight plan provider...");
      vatsimDataProvider = new VatsimFlightPlanProvider(broker, config.Vatsim);

      // init tasks
      void initTasks()
      {
        logger.Log(LogLevel.INFO, "Initializing tasks...");
        StartTasks();
      }
      if (eSimWrapper.Open.IsOpened)
        initTasks();
      else
        eSimWrapper.Open.Opened += initTasks;


      // main run loop
      broker.SessionEnded += (s, e) => IsSupposedToClose = true;
      while (!IsSupposedToClose)
      {
        Thread.Sleep(5000);
      }
    }

    private static readonly List<AbstractTask> tasks = new List<AbstractTask>();

    private static void StartTasks()
    {
      AbstractTask at;
      TaskInitData taskInitData = new(broker, vatsimDataProvider, eSimWrapper);

      if (config.Tasks.ContactMe.Enabled)
      {
        logger.Log(LogLevel.INFO, "Starting ContactMe task...");
        at = new ContactMeTask(taskInitData, config.Tasks.ContactMe);
        tasks.Add(at);
      }

      if (config.Tasks.ImportantRadioMessage.Enabled)
      {
        logger.Log(LogLevel.INFO, "Starting ImportantRadioMessage task...");
        at = new ImportantRadioMessageAlertTask(taskInitData, config.Tasks.ImportantRadioMessage);
        tasks.Add(at);
      }

      if (config.Tasks.Disconnected.Enabled)
      {
        logger.Log(LogLevel.INFO, "Starting Disconnected task...");
        at = new DisconnectedTask(taskInitData, config.Tasks.Disconnected);
        tasks.Add(at);
      }
    }

    private static void LoadConfig()
    {
      string configAbsoluteFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

      if (!File.Exists(configAbsoluteFilePath))
      {
        logger.Log(LogLevel.ERROR, $"Config file '{CONFIG_FILE_NAME}' not found at '{configAbsoluteFilePath}'. Please ensure it exists.");
        return;
      }

      var json = File.ReadAllText(configAbsoluteFilePath);

      var config = JsonConvert.DeserializeObject<Config>(json);
      if (config == null)
      {
        logger.Log(LogLevel.ERROR, $"Failed to deserialize config from '{configAbsoluteFilePath}'. Please check the file format.");
        return;
      }

      // Optional: Validate using DataAnnotations
      var context = new ValidationContext(config);
      var results = new List<ValidationResult>();
      bool isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

      if (!isValid)
      {
        var messages = string.Join(Environment.NewLine, results.Select(r => $"- {r.ErrorMessage}"));
        logger.Log(LogLevel.ERROR, $"Config validation failed:{Environment.NewLine}{messages}");
        return;
      }

      Program.config = config;
    }
  }
}
