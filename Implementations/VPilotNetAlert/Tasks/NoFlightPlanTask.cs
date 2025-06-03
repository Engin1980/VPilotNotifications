using ESimConnect;
using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VPilotNetAlert.Settings;

namespace VPilotNetAlert.Tasks
{
  internal class NoFlightPlanTask : AbstractTask
  {
    private readonly NoFlightPlanConfig config;
    private readonly System.Timers.Timer checkTimer;
    private bool parkingBrakeCheckEnabled = true;
    private readonly TypeId parkingBrakeTypeId;
    private bool heightCheckEnabled = true;
    private readonly TypeId heightTypeId;

    public NoFlightPlanTask(TaskInitData initData, NoFlightPlanConfig config) : base(initData)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));

      Logger.Log(LogLevel.INFO, "NoFlightPlanTask initializing.");

      this.config = config;

      this.checkTimer = new(this.config.DetectionInterval * 1000)
      {
        Enabled = false,
        AutoReset = true
      };
      this.checkTimer.Elapsed += CheckTimer_Elapsed;

      this.Broker.NetworkConnected += (s, e) => this.checkTimer.Enabled = true;
      this.Broker.NetworkDisconnected += (s, e) => this.checkTimer.Enabled = false;

      Logger.Log(LogLevel.DEBUG, "Registering TypeIds for NoFlightPlanTask.");
      heightTypeId = this.ESimWrapper.ValueCache.Register("PLANE ALT ABOVE GROUND");
      parkingBrakeTypeId = this.ESimWrapper.ValueCache.Register("BRAKE PARKING POSITION");
      Logger.Log(LogLevel.DEBUG, $"Registered TypeIds: {parkingBrakeTypeId}, {heightTypeId}");

      Logger.Log(LogLevel.INFO, "NoFlightPlanTask initialized.");
    }

    private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, "NoFlightPlanTask check timer elapsed. Checking conditions.");

      double parkingBrakeValue= this.ESimWrapper.ValueCache.GetValue(parkingBrakeTypeId);
      double heightValue = this.ESimWrapper.ValueCache.GetValue(heightTypeId);
      Logger.Log(LogLevel.DEBUG, $"Current values - Parking Brake: {parkingBrakeValue}, Height: {heightValue}");
      Logger.Log(LogLevel.DEBUG, $"Parking Brake Check Enabled: {parkingBrakeCheckEnabled}, Height Check Enabled: {heightCheckEnabled}");

      if (parkingBrakeCheckEnabled)
      {
        if (parkingBrakeValue < 0.5) // Parking brake is off
        {
          this.parkingBrakeCheckEnabled = false;
          if (this.VatsimFlightPlanProvider.CurrentFlightPlan == null)
          {
            this.Logger.Log(LogLevel.INFO, "No flight plan detected while parking brake is off. Playing alert sound.");
            base.SendSystemPrivateMessage("No flight plan detected while parking brake is off. Please file a flight plan before takeoff.");
            Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
          }
        }
      }
      else if (heightCheckEnabled)
      {
        if (heightValue > this.config.DetectionOnHeight) // Height above ground is greater than configured threshold
        {
          this.heightCheckEnabled = false;

          if (this.VatsimFlightPlanProvider.CurrentFlightPlan == null)
          {
            this.Logger.Log(LogLevel.INFO, "No flight plan detected while above configured height. Playing alert sound.");
            base.SendSystemPrivateMessage("No flight plan detected.. Please file a flight plan.");
            Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
          }
        }
      }

      if (!parkingBrakeCheckEnabled && parkingBrakeValue < 0.5)
        parkingBrakeCheckEnabled = true;
      if (!heightCheckEnabled && heightValue < this.config.DetectionOnHeight)
        heightCheckEnabled = true;
    }
  }
}
