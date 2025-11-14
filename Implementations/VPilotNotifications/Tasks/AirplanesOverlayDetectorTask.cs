using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Settings;
using Eng.VPilotNotifications.Vatsim;
using ESimConnect;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Tasks
{
  internal class AirplanesOverlayDetectorTask : AbstractTask
  {
    public record AirplaneOverlay(
      int Cid,
      string Callsign,
      double Latitude,
      double Longitude,
      double Altitude,
      double DistanceInMeters);
    private const int INITIAL_DELAY_IN_MS = 5000;
    private const int MAX_ALTITUDE_DIFFERENCE = 100;
    private readonly AirplanesOverlayDetectorConfig config;
    private readonly TypeId planeLatitudeTypeId;
    private readonly TypeId planeLongitudeTypeId;
    private readonly TypeId planeAltitudeTypeId;
    private readonly TypeId simOnGroundTypeId;
    private readonly System.Timers.Timer? repeatTimer = null;

    internal AirplanesOverlayDetectorTask(TaskInitData data, AirplanesOverlayDetectorConfig config) : base(data)
    {
      Logger.Log(LogLevel.INFO, "AirplanesOverlayDetectorTask initializing.");
      this.config = config ?? throw new ArgumentNullException(nameof(config), "AirplanesOverlayDetectorConfig cannot be null.");

      if (this.config.ThresholdDistanceInMeters <= 0)
      {
        Logger.Log(LogLevel.ERROR, "Threshold distance must be greater than zero. Default 25 will be used instead.");
        this.config.ThresholdDistanceInMeters = 25;
      }
      if (this.config.RepeatInterval > 0 && this.config.RepeatInterval <= 60)
      {
        Logger.Log(LogLevel.ERROR, $"Repeat interval must be greater than 59 seconds or zero/negative. Repetitions are disabled.");
        this.config.RepeatInterval = 0;
      }

      this.Broker.NetworkConnected += (s, e) => Task.Run(DoOverlayDetectionAsync);
      if (this.config.RepeatInterval > 0)
      {
        this.repeatTimer = new System.Timers.Timer(this.config.RepeatInterval * 1000)
        {
          AutoReset = true,
          Enabled = false
        };
        this.repeatTimer.Elapsed += (s, e) => Task.Run(DoOverlayDetectionAsync);

        this.Broker.NetworkConnected += (s, e) => this.repeatTimer?.Start();
        this.Broker.NetworkDisconnected += (s, e) => this.repeatTimer?.Stop();
      }

      Logger.Log(LogLevel.DEBUG, "Registering TypeIds for AirplanesOverlayDetectorTask.");
      this.planeLatitudeTypeId = this.ESimWrapper.ValueCache.Register("PLANE LATITUDE", unit:ESimConnect.Definitions.SimUnits.Angle.DEGREE);
      this.planeLongitudeTypeId = this.ESimWrapper.ValueCache.Register("PLANE LONGITUDE", unit: ESimConnect.Definitions.SimUnits.Angle.DEGREE);
      this.planeAltitudeTypeId = this.ESimWrapper.ValueCache.Register("PLANE ALTITUDE", unit: ESimConnect.Definitions.SimUnits.Length.FOOT);
      this.simOnGroundTypeId = this.ESimWrapper.ValueCache.Register("SIM ON GROUND");


      Logger.Log(LogLevel.DEBUG, "Checking the audio file for existence.");
      if (!System.IO.File.Exists(config.AudioFile.Name))
      {
        Logger.Log(LogLevel.ERROR, $"Audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
        base.SendSystemPrivateMessage($"NoFlightPlan audio file '{config.AudioFile.Name}' does not exist. Please check the configuration.");
      }

      Logger.Log(LogLevel.INFO, "AirplanesOverlayDetectorTask initialized.");
    }

    private async Task DoOverlayDetectionAsync()
    {

      double isOnGroundFlag = this.ESimWrapper.ValueCache.GetValue(simOnGroundTypeId);
      if (isOnGroundFlag != 1)
      {
        Logger.Log(LogLevel.INFO, "Airplanes Overlay Detection after delay skipped, plane not on ground.");
        return;
      }

      int attempt = 0;
      const int MAX_ATTEMPTS = 5;
      Logger.Log(LogLevel.INFO, "Starting Airplanes Overlay Detection after initial delay.");

      List<VatsimAirplanePositionsProvider.AirplanePositionInfo>? airplanePositions = null;
      List<AirplaneOverlay> airplaneOverlays;

      while (airplanePositions == null)
      {
        attempt++;
        if (attempt > MAX_ATTEMPTS)
        {
          Logger.Log(LogLevel.ERROR, "Failed to retrieve airplane positions after multiple attempts.");
          return;
        }
        Thread.Sleep(INITIAL_DELAY_IN_MS);

        airplanePositions = await this.VatsimAirplanePositionsProvider.GetAirplanePositionsAsync();
      }

      var planeLatitude = this.ESimWrapper.ValueCache.GetValue(planeLatitudeTypeId);
      var planeLongitude = this.ESimWrapper.ValueCache.GetValue(planeLongitudeTypeId);
      var planeAltitude = this.ESimWrapper.ValueCache.GetValue(planeAltitudeTypeId);

      airplaneOverlays = airplanePositions
        .Select(q => new AirplaneOverlay(q.Cid, q.Callsign, q.latitude, q.longitude, q.altitude,
          CalculateGpsDistance(planeLatitude, planeLongitude, q.latitude, q.longitude)))
        .Where(q => q.DistanceInMeters <= this.config.ThresholdDistanceInMeters)
        .Where(q => Math.Abs(q.Altitude - planeAltitude) < MAX_ALTITUDE_DIFFERENCE)
        .ToList();

      if (airplaneOverlays.Count > 0)
      {
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);

        foreach (var overlay in airplaneOverlays)
        {
          Logger.Log(LogLevel.INFO, $"Detected airplane overlay: CID={overlay.Cid}, Callsign={overlay.Callsign}, " +
            $"Lat={overlay.Latitude}, Lon={overlay.Longitude}, Alt={overlay.Altitude}, Dist={overlay.DistanceInMeters}m");
          base.SendSystemPrivateMessage($"Detected airplane overlay: CID={overlay.Cid}, Callsign={overlay.Callsign}, distance={overlay.DistanceInMeters:N0} m");
        }
      }
    }

    private static double CalculateGpsDistance(double lat1, double lon1, double lat2, double lon2)
    {
      const double R = 6371e3; // Earth radius in meters
      var phi1 = lat1 * Math.PI / 180;
      var phi2 = lat2 * Math.PI / 180;
      var deltaPhi = (lat2 - lat1) * Math.PI / 180;
      var deltaLambda = (lon2 - lon1) * Math.PI / 180;
      var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
              Math.Cos(phi1) * Math.Cos(phi2) *
              Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
      var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      return R * c; // Distance in meters
    }
  }
}
