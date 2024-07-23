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
using System.Timers;
using VPilotMessageAlert;
using VPilotMessageAlert.Settings;

namespace VPilotMessageAlert
{
  public class VPilotPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin
  {
    private const string CONNECTED_INFO_PRIVATE_MESSAGE = "Plugin loaded and active";

    public string Name => "VPilotMessageAlert";
    private BrokerProxy brokerProxy;
    private ELogging.Logger logger = null;
    private static readonly VPilotMessageAlert.Settings.Root settings = null;
    private VatsimDataProvider vatsimDataProvider = null;
    private string connectedCallsign;
    private const string DEFAULT_LOG_FILE_NAME = "_log.txt";
    private static readonly bool isPluginMode = System.IO.Directory.Exists("Plugins");
    private System.Timers.Timer disconnectedRepeatTimer;
    private Action disconnectedPlaySoundAction;
    private Action connectedPlaySoundAction;
    private Action radioMessagePlaySoundAction;
    private Action selcalAlertSoundAction;
    private Action systemAlertSoundAction;

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

      SetUpDisconnectedTimer(settings.Behavior);
      SetUpActions();
      this.vatsimDataProvider = new VatsimDataProvider(settings.Vatsim);
      this.vatsimDataProvider.FlightPlanUpdateProcessed += VatsimDataProvider_FlightPlanUpdateProcessed;

      Logger.Log(nameof(VPilotPlugin), LogLevel.INFO, "Plugin seems to be loaded and running.");
    }

    private void VatsimDataProvider_FlightPlanUpdateProcessed(VatsimDataProvider.MonitoredDataRecord flightPlanInfo)
    {
      if (settings.Behavior.SendPrivateMessageWhenFlightPlanDetected && this.connectedCallsign != null)
      {
        this.brokerProxy.SendPrivateMessage($"Flight plan update: {flightPlanInfo.Departure} -> {flightPlanInfo.Arrival}.");
        systemAlertSoundAction?.Invoke();
      }
    }

    private void SetUpActions()
    {
      {
        logger.Log(LogLevel.INFO, "Setting up SystemAlert-Action");
        var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.SystemAlert);
        logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
        if (rule != null)
          this.systemAlertSoundAction = () => TryPlaySound(rule.File);
      }
      {
        logger.Log(LogLevel.INFO, "Setting up SelCalAlert-Action");
        var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.SelcalAlert);
        logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
        if (rule != null)
          this.selcalAlertSoundAction = () => TryPlaySound(rule.File);
      }
      {
        logger.Log(LogLevel.INFO, "Setting up Message-Action");
        var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.RadioMessage);
        logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
        if (rule != null)
          this.radioMessagePlaySoundAction = () => TryPlaySound(rule.File);
      }
      {
        logger.Log(LogLevel.INFO, "Setting up NetworkDisconnected-Action");
        var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.Disconnected);
        logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
        if (rule != null)
          this.disconnectedPlaySoundAction = () => TryPlaySound(rule.File);
      }
      {
        logger.Log(LogLevel.INFO, "Setting up NetworkConnected-Action");
        var rule = settings.Events.FirstOrDefault(q => q.Action == Settings.EventAction.Connected);
        logger.Log(LogLevel.DEBUG, rule == null ? "No rule found" : "Found rule with file " + rule.File.Name);
        if (rule != null)
          this.connectedPlaySoundAction = () => TryPlaySound(rule.File);
      }
    }

    private void SetUpDisconnectedTimer(Behavior behavior)
    {
      if (behavior.RepeatAlertIntervalWhenDisconnected <= 0)
      {
        this.disconnectedRepeatTimer = null;
      }
      else
      {
        this.disconnectedRepeatTimer = new System.Timers.Timer(behavior.RepeatAlertIntervalWhenDisconnected * 1000)
        {
          AutoReset = true,
          Enabled = false
        };
        this.disconnectedRepeatTimer.Elapsed += DisconnectedRepeatTimer_Elapsed;
      }
    }

    private void DisconnectedRepeatTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      this.disconnectedPlaySoundAction();
    }

    private void Broker_SelcalAlertReceived(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.SelcalAlertReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "SelcalAlertReceived");
      selcalAlertSoundAction?.Invoke();
    }

    private void Broker_RadioMessageReceived(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.RadioMessageReceivedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "RadioMessageReceived");
      if (radioMessagePlaySoundAction != null && IsMessageToMonitoredDataMatch(e.Message))
        radioMessagePlaySoundAction();
    }

    private bool IsMessageToMonitoredDataMatch(string message)
    {
      message = message.ToUpper();
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
      this.connectedCallsign = null;

      if (disconnectedPlaySoundAction != null)
      {
        disconnectedPlaySoundAction();
        if (disconnectedRepeatTimer != null)
          disconnectedRepeatTimer.Enabled = true;
      }
    }

    private void Broker_NetworkConnected(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.NetworkConnectedEventArgs e)
    {
      logger.Log(LogLevel.INFO, "NetworkConnected");
      this.disconnectedRepeatTimer.Enabled = false;

      this.connectedCallsign = e.Callsign;

      this.vatsimDataProvider.SetMonitoredVatsimId(e.Cid);
      this.vatsimDataProvider.StartDownloading();

      SendPrivateMessageOnFirstConnectionIfRequired();
      connectedPlaySoundAction?.Invoke();
    }

    private void SendPrivateMessageOnFirstConnectionIfRequired()
    {
      if (VPilotPlugin.settings.Behavior.SendPrivateMessageWhenConnectedForTheFirstTime)
      {
        this.brokerProxy.SendPrivateMessage(CONNECTED_INFO_PRIVATE_MESSAGE);
        systemAlertSoundAction?.Invoke();
        VPilotPlugin.settings.Behavior.SendPrivateMessageWhenConnectedForTheFirstTime = false;
      };
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
