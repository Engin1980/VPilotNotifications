using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VPilotNetAlert.Settings;
using VPilotNetCoreModule;

namespace VPilotNetAlert.Tasks
{
  internal class DisconnectedTask : AbstractTask
  {
    private bool isNetworkConnected = false;
    private readonly System.Timers.Timer checkTimer;
    private readonly DisconnectedConfig config;
    public DisconnectedTask(TaskInitData data, DisconnectedConfig config) : base(data)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));

      Logger.Log(LogLevel.INFO, "DisconnectedTask initializing.");

      this.config = config;
      this.checkTimer = new System.Timers.Timer(config.RepeatInterval * 1000)
      {
        AutoReset = true,
        Enabled = false,
      };
      this.checkTimer.Elapsed += CheckTimer_Elapsed;

      data.Broker.NetworkDisconnected += (s, e) =>
      {
        Logger.Log(LogLevel.INFO, "Network disconnected. Starting check timer.");
        isNetworkConnected = false;
        this.checkTimer.Start();
      };
      data.Broker.NetworkConnected += (s, e) =>
      {
        Logger.Log(LogLevel.INFO, "Network connected. Stopping check timer.");
        isNetworkConnected = true;
        this.checkTimer.Stop();
      };

      Logger.Log(LogLevel.INFO, "DisconnectedTask initialized.");
    }

    private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, "CheckTimer elapsed. Checking network connection status.");
      if (isNetworkConnected)
      {
        Logger.Log(LogLevel.DEBUG, "Network is connected. No action needed.");
        return;
      }
      else
      {
        Logger.Log(LogLevel.INFO, "Network is disconnected. Playing warning sound.");
        base.SendSystemPrivateMessage("Network disconnected. Please check your connection.");
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
      }
    }
  }
}
