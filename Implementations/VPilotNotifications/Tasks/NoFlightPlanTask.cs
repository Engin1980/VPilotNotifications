using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Settings;
using ESimConnect;
using ESimConnect.Extenders;
using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Eng.VPilotNotifications.Tasks
{
  internal class NoFlightPlanTask : AbstractTask
  {
    private const int WAIT_TIME_TO_DOWNLOAD_FLIGHT_PLAN = 5000;
    private readonly NoFlightPlanConfig config;
    private readonly System.Timers.Timer heightCheckTimer;
    private readonly TypeId parkingBrakeTypeId;
    private bool heightCheckEnabled = true;
    private readonly TypeId heightTypeId;

    public NoFlightPlanTask(TaskInitData initData, NoFlightPlanConfig config) : base(initData)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));

      Logger.Log(LogLevel.INFO, "NoFlightPlanTask initializing.");

      this.config = config;

      this.heightCheckTimer = new(this.config.DetectionOnHeightInterval * 1000)
      {
        Enabled = false,
        AutoReset = true
      };
      this.heightCheckTimer.Elapsed += CheckTimer_Elapsed;

      if (this.config.DetectionOnConnection)
        this.Broker.NetworkConnected += Broker_FlightPlanDetectionOnConnection;
      this.Broker.NetworkConnected += (s, e) => this.heightCheckTimer.Enabled = true;
      this.Broker.NetworkDisconnected += (s, e) => this.heightCheckTimer.Enabled = false;

      Logger.Log(LogLevel.DEBUG, "Registering TypeIds for NoFlightPlanTask.");
      heightTypeId = this.ESimWrapper.ValueCache.Register("PLANE ALT ABOVE GROUND");
      parkingBrakeTypeId = this.ESimWrapper.ValueCache.Register("BRAKE PARKING POSITION");
      this.ESimWrapper.ValueCache.ValueChanged += ValueCache_ValueChanged;
      Logger.Log(LogLevel.DEBUG, $"Registered TypeIds: {parkingBrakeTypeId}, {heightTypeId}");

      Logger.Log(LogLevel.INFO, $"NoFlightPlanTask initialized.");
    }

    private void Broker_FlightPlanDetectionOnConnection(object? sender, NetworkConnectedEventArgs e)
    {
      void testFlightPlanOnConnection()
      {
        Thread.Sleep(WAIT_TIME_TO_DOWNLOAD_FLIGHT_PLAN);
        var fp = this.VatsimFlightPlanProvider.CurrentFlightPlan;
        if (fp == null)
        {
          Logger.Log(LogLevel.INFO, "No flight plan detected on connection. Playing alert sound.");
          base.SendSystemPrivateMessage("No flight plan detected. Please file a flight plan before takeoff.");
          Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
        }
      }

      Task.Run(testFlightPlanOnConnection);
    }

    private void ValueCache_ValueChanged(ValueCacheExtender.ValueChangeEventArgs e)
    {
      if (this.heightCheckTimer.Enabled == false)
      {
        // timer not enabled, therefore not connected, skip processing
        return;
      }
      if (e.TypeId != parkingBrakeTypeId || e.Value > .5)
        return;

      Logger.Log(LogLevel.DEBUG, $"ParkingBrake release detected. Current value: {e.Value}");
      if (this.VatsimFlightPlanProvider.CurrentFlightPlan == null)
      {
        this.Logger.Log(LogLevel.INFO, "No flight plan detected while parking brake is off. Playing alert sound.");
        base.SendSystemPrivateMessage("No flight plan detected while parking brake is off. Please file a flight plan before takeoff.");
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
      }
    }

    private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, "NoFlightPlanTask height check timer elapsed. Checking conditions.");

      double heightValue = this.ESimWrapper.ValueCache.GetValue(heightTypeId);
      Logger.Log(LogLevel.DEBUG, $"Current Height: {heightValue}, Height Check Enabled: {heightCheckEnabled}");

      if (heightCheckEnabled)
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
      else
      {
        if (heightValue < this.config.DetectionOnHeight)
          heightCheckEnabled = true;
      }
    }
  }
}
