using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Settings;
using Eng.VPilotNotifications.Vatsim;
using ESystem.Asserting;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Vatsim
{
  internal class VatsimAirplanePositionsProvider
  {
    public record AirplanePositionInfo(int Cid, string Callsign, double latitude, double longitude, double altitude);
    private readonly VatsimConfig settings;
    private readonly Logger logger;

    public VatsimAirplanePositionsProvider(VatsimConfig settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      logger = Logger.Create(nameof(VatsimAirplanePositionsProvider));
      this.settings = settings;
    }

    public async Task<List<AirplanePositionInfo>?> GetAirplanePositionsAsync()
    {
      logger.Log(LogLevel.INFO, "GetAirplanePositionsAsync called.");
      Model? model;

      try
      {
        model = await VatsimModelDownloader.DownloadAsync(settings.Sources);
        EAssert.IsNotNull(model, "VATSIM data model is null after download. This should not happen.");
      }
      catch (Exception ex)
      {
        logger.Log(LogLevel.ERROR, "Failed to download VATSIM data model.");
        logger.LogException(ex);
        return new List<AirplanePositionInfo>();
      }

      List<AirplanePositionInfo> ret = model.Pilots?
        .Select(p => new AirplanePositionInfo(p.CID, p.Callsign, p.Latitude, p.Longitude, p.Altitude))
        .ToList() ?? new List<AirplanePositionInfo>();
      return ret;
    }
  }
}
