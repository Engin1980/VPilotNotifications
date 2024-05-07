using ELogging;
using ESystem;
using ESystem.Asserting;
using NAudio.Wave;
using Newtonsoft.Json;
using RossCarlson.Vatsim.Vpilot.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using VPilotMessageAlert;
using VPilotMessageAlert.Settings;

namespace VPilotMessageAlert
{
  public class VPilotPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin
  {
    public string Name => "VPilotMessageAlert";
    private BrokerProxy brokerProxy;
    private ELogging.Logger logger = null;
    private static readonly VPilotMessageAlert.Settings.Root settings = null;
    private VatsimDataProvider vatsimDataProvider = null;
    private string connectedCallsign;
    private const string DEFAULT_LOG_FILE_NAME = "_log.txt";
    private static readonly bool isPluginMode = System.IO.Directory.Exists("Plugins");

    static VPilotPlugin()
    {
      RegisterLogInitially();

      try
      {
        settings = Settings.Root.Load();
      }
      catch (Exception ex)
      {
        Logger.Log(typeof(VPilotPlugin), LogLevel.WARNING, $"Failed to load settings from file 'settings.json'. Reason: {ex.GetFullMessage()}");
        settings = null;
        return;
      }

      RegisterLogBySettings();

    }

    private static void RegisterLogInitially()
    {
      string logFileName = isPluginMode ? System.IO.Path.Combine("Plugins", DEFAULT_LOG_FILE_NAME) : DEFAULT_LOG_FILE_NAME;
      RegisterLog(LogLevel.TRACE, logFileName, true);
    }

    private static void RegisterLogBySettings()
    {
      RegisterLog(settings.Logging.Level, settings.Logging.FileName, settings.Logging.FileName != DEFAULT_LOG_FILE_NAME);
    }


    private static void RegisterLog(LogLevel level, string fileName, bool deleteFileFirst)
    {
      Logger.RegisterSenderName(typeof(VPilotPlugin), nameof(VPilotPlugin), false);
      if (deleteFileFirst && System.IO.File.Exists(fileName))
        try
        {
          System.IO.File.Delete(fileName);
        }
        catch (Exception)
        {
          // intentionally blank
        }

      Logger.UnregisterLogAction(owner: typeof(VPilotPlugin));

      Logger.RegisterLogAction(
        li =>
        {
          string s = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {li.Level,-10} {li.SenderName,-20} {li.Message}\n";
          lock (typeof(VPilotPlugin))
          {
            System.IO.File.AppendAllText(fileName, s);
          }
        },
        new List<LogRule>()
        {
          new LogRule(".+", level)
        },
        owner: typeof(VPilotPlugin));
    }

    private static VPilotMessageAlert.Settings.Root GetDefaultSettings() => new Settings.Root(new Settings.Logging("_log.txt", ELogging.LogLevel.DEBUG));

    public void Initialize(IBroker broker)
    {
      this.brokerProxy = new BrokerProxy(broker);
      PostInitialize();
    }

    public void Initialize(MockBroker broker)
    {
      this.brokerProxy = new BrokerProxy(broker);
      PostInitialize();
    }

    public void PostInitialize()
    {
      if (settings == null)
      {
        Logger.Log(nameof(VPilotPlugin), LogLevel.CRITICAL, "Settings were not loaded. Plugin is disabled.");
        return;
      }

      this.logger = Logger.Create(this);

      this.brokerProxy.NetworkConnected += Broker_NetworkConnected;
      this.brokerProxy.NetworkDisconnected += Broker_NetworkDisconnected;
      this.brokerProxy.RadioMessageReceived += Broker_RadioMessageReceived;
      this.brokerProxy.SelcalAlertReceived += Broker_SelcalAlertReceived;

      this.vatsimDataProvider = new VatsimDataProvider(settings.Vatsim);
    }

    private void Broker_SelcalAlertReceived(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.SelcalAlertReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "SelcalAlertReceived");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.SelcalAlert);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void Broker_RadioMessageReceived(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.RadioMessageReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "RadioMessageReceived");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.RadioMessage);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null && IsMessageToMonitoredDataMatch(e.Message))
        TryPlaySound(rule.File);
    }

    private bool IsMessageToMonitoredDataMatch(string message)
    {
      bool ret = false;
      if (message.Contains(this.connectedCallsign))
        ret = true;
      else
      {
        var fd = this.vatsimDataProvider.MonitoredData;
        if (fd != null)
        {
          if (message.Contains(fd.Departure))
            ret = true;
          else if (message.Contains(fd.Arrival))
            ret = true;
        }
      }
      this.logger.Log(LogLevel.DEBUG, $"Message '{message}' checked with result {ret}");
      return ret;
    }

    private void Broker_NetworkDisconnected(object sender, EventArgs e)
    {
      logger.Log(LogLevel.INFO, "NetworkDisconnected");
      var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.Disconnected);
      logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
      if (rule != null) TryPlaySound(rule.File);
    }

    private void Broker_NetworkConnected(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.NetworkConnectedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "NetworkConnected");
      this.connectedCallsign = e.Callsign;

      this.vatsimDataProvider.SetMonitoredVatsimId(e.Cid);
      this.vatsimDataProvider.StartDownloading();

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
      WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);
      WaveOutEvent player = new WaveOutEvent();
      player.Init(volumeStream);
      player.Play();
    }
  }
}
