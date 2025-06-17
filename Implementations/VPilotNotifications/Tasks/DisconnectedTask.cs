using Eng.VPilotNotifications.Settings;
using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Eng.VPilotNotifications.Tasks
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
        NotifyDisconnected();
        this.checkTimer.Start();
      };
      data.Broker.NetworkConnected += (s, e) =>
      {
        Logger.Log(LogLevel.INFO, "Network connected. Stopping check timer.");
        isNetworkConnected = true;
        this.checkTimer.Stop();
      };

      Logger.Log(LogLevel.DEBUG, "Checking the audio file for existence.");
      if (!System.IO.File.Exists(config.AudioFile.Name))
      {
        Logger.Log(LogLevel.ERROR, $"Audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
        base.SendSystemPrivateMessage($"Disconnected audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
      }

      Logger.Log(LogLevel.INFO, "DisconnectedTask initialized.");
    }

    private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, "CheckTimer elapsed. Checking network connection status.");
      if (isNetworkConnected)
        Logger.Log(LogLevel.DEBUG, "Network is connected. No action needed.");
      else
        NotifyDisconnected();
    }

    private void NotifyDisconnected()
    {
      Logger.Log(LogLevel.INFO, "Network is disconnected. Playing warning sound.");
      //TOREM unable to do when disconnected: base.SendSystemPrivateMessage("Network disconnected. Please check your connection.");
      Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
    }
  }
}
