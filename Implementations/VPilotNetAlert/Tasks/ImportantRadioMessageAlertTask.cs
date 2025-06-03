using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPilotNetAlert.Settings;
using VPilotNetCoreModule;

namespace VPilotNetAlert.Tasks
{
  internal class ImportantRadioMessageAlertTask : AbstractTask
  {
    private string connectedCallsign = string.Empty;
    private Vatsim.FlightPlan? flightPlan;
    private readonly ImportantRadioMessageConfig config;

    public ImportantRadioMessageAlertTask(TaskInitData data, ImportantRadioMessageConfig config) : base(data)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));

      Logger.Log(LogLevel.DEBUG, $"ImportantRadioMessageAlertTask initializing.");

      this.config = config;
      this.Broker.NetworkConnected += (s, e) => this.connectedCallsign = e.Callsign.ToUpperInvariant();
      this.Broker.NetworkDisconnected += (s, e) => this.connectedCallsign = string.Empty;
      this.Broker.RadioMessageReceived += Broker_RadioMessageReceived;
      this.VatsimFlightPlanProvider.FlightPlanUpdated += e => this.flightPlan = e.Current;

      Logger.Log(LogLevel.DEBUG, $"ImportantRadioMessageAlertTask initialized.");
    }

    private void Broker_RadioMessageReceived(object? sender, RadioMessageReceivedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, $"Radio message received: {e.Message}");

      bool isImportant = IsMessageToMonitoredDataMatch(e.Message);
      Logger.Log(LogLevel.TRACE, $"Message '{e.Message}' is important: {isImportant}");
      if (isImportant)
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
    }

    private bool IsMessageToMonitoredDataMatch(string message)
    {
      message = message.ToUpper();
      bool ret = false;
      if (message.Contains(this.connectedCallsign))
        ret = true;
      else
      {
        var fd = this.flightPlan;
        if (fd != null)
        {
          if (message.Contains(fd.Departure))
            ret = true;
          else if (message.Contains(fd.Arrival))
            ret = true;
        }
      }
      this.Logger.Log(LogLevel.TRACE, $"Message '{message}' checked with result {ret}");
      return ret;
    }
  }
}
