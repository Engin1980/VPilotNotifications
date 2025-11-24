using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Settings;
using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Tasks
{
  internal class ImportantRadioMessageAlertTask : AbstractTask
  {
    private string connectedCallsign = string.Empty;
    private readonly ImportantRadioMessageConfig config;

    public ImportantRadioMessageAlertTask(TaskInitData data, ImportantRadioMessageConfig config) : base(data)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));

      Logger.Log(LogLevel.DEBUG, $"Initializing.");

      this.config = config;
      this.Broker.NetworkConnected += (s, e) => this.connectedCallsign = e.Callsign.ToUpperInvariant();
      this.Broker.NetworkDisconnected += (s, e) => this.connectedCallsign = string.Empty;
      this.Broker.RadioMessageReceived += Broker_RadioMessageReceived;

      Logger.Log(LogLevel.DEBUG, "Checking the audio file for existence.");
      if (!System.IO.File.Exists(config.AudioFile.Name))
      {
        Logger.Log(LogLevel.ERROR, $"Audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
        base.SendSystemPrivateMessage($"Important radio message audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
      }

      Logger.Log(LogLevel.INFO, $"Initialized.");
    }

    private void Broker_RadioMessageReceived(object? sender, RadioMessageReceivedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, $"Radio message received: {e.Message}");

      bool isImportant = IsMessageToMonitoredDataMatch(e.Message);
      Logger.Log(LogLevel.DEBUG, $"Message '{e.Message}' is important: {isImportant}");
      if (isImportant)
      {
        Logger.Log(LogLevel.INFO, $"Message '{e.Message}' alerted");
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
      }

    }

    private bool IsMessageToMonitoredDataMatch(string message)
    {
      message = message.ToUpper();
      bool ret = false;
      if (message.Contains(this.connectedCallsign.ToUpper()))
        ret = true;
      else
      {
        var fd = this.VatsimFlightPlanProvider.CurrentFlightPlan;
        if (fd != null)
        {
          if (message.Contains(fd.Departure.ToUpper()))
            ret = true;
          else if (message.Contains(fd.Arrival.ToUpper()))
            ret = true;
        }
      }
      this.Logger.Log(LogLevel.DEBUG, $"Message '{message}' checked with result {ret}");
      return ret;
    }
  }
}
