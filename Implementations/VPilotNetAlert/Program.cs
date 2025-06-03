using ESystem.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
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
      LoadConfig(out List<LogRule> rules, out string? errorMessage);

      InitLogging(rules);

      if (config == null)
      {
        logger.Log(LogLevel.ERROR, "Config load error: " + errorMessage);
        logger.Log(LogLevel.ERROR, "Configuration is null after loading. Exiting.");
        return;
      }
      logger.Log(LogLevel.DEBUG, $"Configuration loaded. Logging initialized.");

      string? pipeId = args.Length > 0 ? args[0] : null;
      if (string.IsNullOrEmpty(pipeId))
      {
        logger.Log(LogLevel.ERROR, "No pipe ID provided. Please provide a pipe ID as the first argument. Aborted.");
        return;
      }
      logger.Log(LogLevel.INFO, $"Starting VPilotNetAlert with pipe ID '{pipeId}'");

      // init sim & broker
      logger.Log(LogLevel.INFO, "Initializing ESimWrapper...");
      eSimWrapper = new ESimWrapper();
      logger.Log(LogLevel.INFO, "Initializing ESimWrapper... - completed");
      logger.Log(LogLevel.INFO, "Initializing VPilotNetCoreModule.ClientProxyBroker...");
      broker = new VPilotNetCoreModule.ClientProxyBroker(pipeId);
      logger.Log(LogLevel.INFO, "Initializing VPilotNetCoreModule.ClientProxyBroker... - completed");

      // init Vatsim Flight Plan Provider
      logger.Log(LogLevel.INFO, "Initializing VATSIM flight plan provider ...");
      vatsimDataProvider = new VatsimFlightPlanProvider(broker, config.Vatsim);
      logger.Log(LogLevel.INFO, "Initializing VATSIM flight plan provider... - completed");

      // init tasks
      logger.Log(LogLevel.INFO, "Prepating task initialization...");
      static void initTasks()
      {
        logger.Log(LogLevel.INFO, "Initializing tasks...");
        StartTasks();
      }
      logger.Log(LogLevel.INFO, "Opening connection to FS...");
      eSimWrapper.Open.OpenInBackground(initTasks);
      logger.Log(LogLevel.INFO, "Opening connection to FS... - completed");


      // main run loop
      logger.Log(LogLevel.INFO, "Entering main app loop...");
      broker.SessionEnded += (s, e) => IsSupposedToClose = true;
      while (!IsSupposedToClose)
      {
        Thread.Sleep(5000);
      }
    }

    private static void InitLogging(List<LogRule> rules)
    {
      void saveToFile(LogItem li)
      {
        string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"{DateTime.Now:yyyy-MM-dd}.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        string level = $"[{li.Level}]";
        File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level,-8} {li.Sender,-25}: {li.Message}{Environment.NewLine}");
      }

      void printToConsole(LogItem li)
      {
        string level = $"[{li.Level}]";
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {level,-8} {li.Sender,-25}: {li.Message}");
      }

      Logger.RegisterLogAction(saveToFile, rules);
      Logger.RegisterLogAction(printToConsole, rules);

      logger.Log(LogLevel.INFO, "Logging system initialized.");
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

    private static void LoadConfig(out List<LogRule> rules, out string? errorMessage)
    {
      rules = new();

      Config? cfg;
      string configAbsoluteFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

      if (!File.Exists(configAbsoluteFilePath))
      {
        errorMessage = $"Config file '{CONFIG_FILE_NAME}' not found at '{configAbsoluteFilePath}'. Please ensure it exists.";
        return;
      }

      try
      {
        var json = File.ReadAllText(configAbsoluteFilePath);
        cfg = JsonConvert.DeserializeObject<Config>(json);
      }
      catch (Exception ex)
      {
        errorMessage = $"Failed to deserialize config from '{configAbsoluteFilePath}': {ex.Message}";
        return;
      }

      if (cfg == null)
      {
        errorMessage = $"Failed to deserialize config from '{configAbsoluteFilePath}'. Please check the file format.";
        return;
      }

      // Optional: Validate using DataAnnotations
      var context = new ValidationContext(cfg);
      var results = new List<ValidationResult>();
      bool isValid = Validator.TryValidateObject(cfg, context, results, validateAllProperties: true);

      if (!isValid)
      {
        var messages = string.Join(Environment.NewLine, results.Select(r => $"- {r.ErrorMessage}"));
        errorMessage = $"Config validation failed:{Environment.NewLine}{messages}";
        return;
      }

      errorMessage = null;
      Program.config = cfg;
    }
  }
}
