using ELogging;
using ESystem;
using ESystem.Asserting;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using RossCarlson.Vatsim.Vpilot.Plugins;
using System.Runtime.CompilerServices;
using VPilotMessageAlert;

namespace VPilotMessageAlert
{
  public class VPilotPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin
  {
    public string Name => "VPilotMessageAlert";
    private BrokerProxy? brokerProxy;
    private ELogging.Logger logger = null!;
    private static readonly VPilotMessageAlert.Settings.Root settings = null!;

    static VPilotPlugin()
    {
      var provider = new ConfigurationManager();
      provider.AddJsonFile("settings.json");

      try
      {
        settings = provider.Get<VPilotMessageAlert.Settings.Root>() ?? throw new ApplicationException("Configuration returned null.");
        RegisterLog();
      }
      catch (Exception ex)
      {
        VPilotPlugin.settings = GetDefaultSettings();
        RegisterLog();
        Logger.Log(typeof(VPilotPlugin), LogLevel.WARNING, $"Failed to load settings from file 'settings.json'. Reason: {ex.GetFullMessage()}");
      }
      EAssert.IsNotNull(VPilotPlugin.settings);
      if (settings.Events.Count == 0)
        Logger.Log(typeof(VPilotPlugin), LogLevel.WARNING, "No events are monitored. Update configuration file.");

      //TODO add file-exist validation
    }

    private static void RegisterLog()
    {
      Logger.RegisterLogAction(
        li =>
        {
          string s = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {li.Level,-20} {li.SenderName} {li.Message}\n";
          System.IO.File.AppendAllText(settings.Logging.FileName, s);
        },
        new List<LogRule>()
        {
          new(".+", settings.Logging.Level)
        });
    }

    private static VPilotMessageAlert.Settings.Root GetDefaultSettings() => new(new(), new("_log.txt", ELogging.LogLevel.DEBUG), new(new()));

    public void Initialize(IBroker broker)
    {
      this.logger = ELogging.Logger.Create(this);

      this.brokerProxy = new(broker);

      this.brokerProxy.NetworkConnected += Broker_NetworkConnected;
      this.brokerProxy.NetworkDisconnected += Broker_NetworkDisconnected;
      this.brokerProxy.RadioMessageReceived += Broker_RadioMessageReceived;
      this.brokerProxy.SelcalAlertReceived += Broker_SelcalAlertReceived;
    }

    public void Initialize(MockBroker broker)
    {
      this.logger = ELogging.Logger.Create(this);

      this.brokerProxy = new(broker);

      this.brokerProxy.NetworkConnected += Broker_NetworkConnected;
      this.brokerProxy.NetworkDisconnected += Broker_NetworkDisconnected;
      this.brokerProxy.RadioMessageReceived += Broker_RadioMessageReceived;
      this.brokerProxy.SelcalAlertReceived += Broker_SelcalAlertReceived;
    }

    private void Broker_SelcalAlertReceived(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.SelcalAlertReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "SelcalAlertReceived");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.SelcalAlert);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void Broker_RadioMessageReceived(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.RadioMessageReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "RadioMessageReceived");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.RadioMessage);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void Broker_NetworkDisconnected(object? sender, EventArgs e)
    {
      logger.Log(LogLevel.INFO, "NetworkDisconnected");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.Disconnected);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void Broker_NetworkConnected(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.NetworkConnectedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "NetworkConnected");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.Connected);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void TryPlaySound(Settings.File file)
    {
      EAssert.Argument.IsNotNull(file);
      EAssert.Argument.IsTrue(System.IO.File.Exists(file.Name));
      WaveStream mainOutputStream;

      if (file.Name.ToLower().EndsWith(".mp3")) mainOutputStream = new Mp3FileReader(file.Name);
      else if (file.Name.ToLower().EndsWith(".wav")) mainOutputStream = new WaveFileReader(file.Name);
      else
      {
        this.logger.Log(LogLevel.ERROR, $"Unable to play {file.Name}. Only MP3/WAV is supported.");
        return;
      }
      WaveChannel32 volumeStream = new(mainOutputStream);
      WaveOutEvent player = new();
      player.Init(volumeStream);
      player.Play();
    }
  }
}
