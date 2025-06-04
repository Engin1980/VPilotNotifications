using ESystem;
using ESystem.Asserting;
using ESystem.Json;
using ESystem.Logging;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using VPilotNetAlert.Settings;
using VPilotNetAlert.Tasks;
using VPilotNetAlert.Vatsim;
using static ESimConnect.Definitions.SimUnits;

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
    private static readonly object LOG_FILE_LOCK = new object();
    private static string? logFileName = null;
    private static readonly Object EXIT_LOCK = new object();

    static void Main(string[] args)
    {
      Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

      InitLogging();

      LoadConfig();
      if (config == null)
      {
        logger.Log(LogLevel.ERROR, "Configuration is null after loading. Exiting.");
        return;
      }
      logger.Log(LogLevel.DEBUG, $"Configuration loaded. Logging initialized.");
      logger.Log(LogLevel.DEBUG, $"Log file name: {logFileName}");

      string? pipeId = args.Length > 0 ? args[0] : "?";
      if (string.IsNullOrEmpty(pipeId))
      {
        logger.Log(LogLevel.ERROR, "No pipe ID provided. Please provide a pipe ID as the first argument. Aborted.");
        return;
      }
      logger.Log(LogLevel.INFO, $"Starting VPilotNetAlert with pipe ID '{pipeId}'");

      logger.Log(LogLevel.DEBUG, $"Current assembly base directory: '{AppDomain.CurrentDomain.BaseDirectory}'");
      logger.Log(LogLevel.DEBUG, $"Current working directory: '{System.IO.Directory.GetCurrentDirectory()}'");

      // init sim & broker
      logger.Log(LogLevel.INFO, "Initializing ESimWrapper...");
      eSimWrapper = new ESimWrapper();
      logger.Log(LogLevel.INFO, "Initializing ESimWrapper... - completed");
      logger.Log(LogLevel.INFO, "Initializing VPilotNetCoreModule.ClientProxyBroker...");
      broker = new VPilotNetCoreModule.ClientProxyBroker(pipeId, config.Global.ConnectTimeout);
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
        logger.Log(LogLevel.INFO, "Initializing tasks... - completed");
      }
      logger.Log(LogLevel.INFO, "Opening connection to FS...");
      eSimWrapper.Open.OpenInBackground(initTasks);
      logger.Log(LogLevel.INFO, "Opening connection to FS... - completed");


      // main run loop
      logger.Log(LogLevel.INFO, "Entering main app loop wait...");
      broker.SessionEnded += (s, e) =>
      {
        lock (EXIT_LOCK)
        {
          Monitor.Pulse(EXIT_LOCK);
        }
      };
      lock (EXIT_LOCK)
      {
        Monitor.Wait(EXIT_LOCK);
      }
      logger.Log(LogLevel.DEBUG, "Closing broker");
      broker.CloseBroker();
      logger.Log(LogLevel.DEBUG, "Closing ESimConnect");
      eSimWrapper.ESimConnect.Close();
      logger.Log(LogLevel.INFO, "Main app loop ended. Exiting application.");
    }

    private static void InitLogging()
    {
      List<LogRule> rules;
      try
      {
        EJDict ejObj = EJObject.Load(ConfigAbsoluteFilePath);
        EJDict ejLogging = ejObj["Logging"].AsDict() ?? throw new ArgumentException();
        var ejRules = ejLogging["Rules"] ?? throw new ArgumentException();
        rules = LogUtils.LoadLogRulesFromJson(ejRules, "Pattern", "Level");
        if (rules.Count == 0) throw new ArgumentException();
      }
      catch (Exception)
      {
        rules = new List<LogRule>
        {
          new(".*", LogLevel.TRACE)
        };
      }

      void saveToFile(LogItem li)
      {
        if (Program.logFileName == null) return;

        string level = $"[{li.Level}]";
        lock (LOG_FILE_LOCK)
        {
          File.AppendAllText(Program.logFileName, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level,-8} {li.Sender,-35}: {li.Message}{Environment.NewLine}");
        }
      }

      void printToConsole(LogItem li)
      {
        string level = $"[{li.Level}]";
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {level,-8} {li.Sender,-35}: {li.Message}");
      }

      Logger.RegisterLogAction(saveToFile, rules);
      Logger.RegisterLogAction(printToConsole, rules);

      logger.Log(LogLevel.INFO, $"Logging system initialized with {rules.Count} rules.");
    }

    private static readonly List<AbstractTask> tasks = new();

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

      if (config.Tasks.NoFlightPlan.Enabled)
      {
        logger.Log(LogLevel.INFO, "Starting NoFlightPlan task...");
        at = new NoFlightPlanTask(taskInitData, config.Tasks.NoFlightPlan);
        tasks.Add(at);
      }
    }

    private static string ConfigAbsoluteFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

    private static void LoadConfig()
    {
      Config? cfg;

      if (!File.Exists(ConfigAbsoluteFilePath))
      {
        logger.Log(LogLevel.ERROR, $"Config file '{CONFIG_FILE_NAME}' not found at '{ConfigAbsoluteFilePath}'. Please ensure it exists.");
        return;
      }

      try
      {
        var json = File.ReadAllText(ConfigAbsoluteFilePath);
        cfg = JsonConvert.DeserializeObject<Config>(json);
      }
      catch (Exception ex)
      {
        logger.Log(LogLevel.ERROR, $"Failed to deserialize config from '{ConfigAbsoluteFilePath}': {ex.Message}");
        return;
      }

      if (cfg == null)
      {
        logger.Log(LogLevel.ERROR, $"Failed to deserialize config from '{ConfigAbsoluteFilePath}'. Please check the file format.");
        return;
      }

      // Optional: Validate using DataAnnotations
      var context = new ValidationContext(cfg);
      var results = new List<ValidationResult>();
      bool isValid = Validator.TryValidateObject(cfg, context, results, validateAllProperties: true);

      if (!isValid)
      {
        var messages = string.Join(Environment.NewLine, results.Select(r => $"- {r.ErrorMessage}"));
        logger.Log(LogLevel.ERROR, $"Config validation failed:{Environment.NewLine}{messages}");
        return;
      }


      Program.logFileName = Path.Combine(Directory.GetCurrentDirectory(), cfg.Logging.FileName);
      Directory.CreateDirectory(Path.GetDirectoryName(logFileName)!);

      Program.config = cfg;
    }
  }
}
