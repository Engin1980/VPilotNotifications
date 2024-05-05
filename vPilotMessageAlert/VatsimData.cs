using ELogging;
using ESystem.Asserting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VPilotMessageAlert.Settings;

namespace VPilotMessageAlert
{
  public class VatsimData
  {
    public record FlightPlan(string Departure, string Arrival);
    public record Pilot(int CID, string Callsign, double Latitude, double Longitude);
    public record Prefile(int CID, string Callsign, FlightPlan Flight_plan);
    public record Model(List<Pilot> Pilots, List<Prefile> Prefiles);

    private readonly Vatsim settings;
    private readonly System.Timers.Timer updateTimer;
    private readonly Logger logger;
    private string? monitoredVatsimId = null;
    private FlightPlan? monitoredFlightPlan = null;
    private string? monitoredCallsign = null;

    public VatsimData(Vatsim settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsTrue(settings.NoFlightPlanUpdateInterval > 0);
      EAssert.Argument.IsTrue(settings.RefreshNoFlightPlanUpdateInterval > 0);

      this.logger = Logger.Create(nameof(VatsimData));

      this.settings = settings;
      this.updateTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = settings.NoFlightPlanUpdateInterval * 60 * 1000,
        Enabled = false
      };
      this.updateTimer.Elapsed += UpdateTimer_Elapsed;
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      InvokeReloadData();
    }

    private void InvokeReloadData()
    {
      Task.Run(() => ReloadDataAsync());
    }

    private async Task ReloadDataAsync()
    {
      if (!Monitor.TryEnter(this.updateTimer))
      {
        // lock did not expire, something wrong?
        this.logger.Log(LogLevel.ERROR, "Update lock did not expire. Too short updating interval or something went wrong during update?");
        return;
      }

      this.logger.Log(LogLevel.INFO, "Reloading VATSIM data");

      try
      {
        var data = await DownloadDataAsync();
        UpdateUserData(data);
      }
      finally
      {
        Monitor.Exit(this.updateTimer);
      }

      this.logger.Log(LogLevel.INFO, "Reloading VATSIM data -- completed.");
    }

    private void UpdateUserData(string data)
    {
      Model? model = JsonConvert.DeserializeObject<Model>(data);
      if (model == null)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to extract vatsim data model from data string of length " + data.Length + ". Skipping.");
        return;
      }
      var prefile = model.Prefiles.FirstOrDefault(q => q.CID.ToString() == this.monitoredVatsimId);
      if (prefile != null)
      {
        this.monitoredCallsign = prefile.Callsign;
        this.monitoredFlightPlan = prefile.Flight_plan;
        this.updateTimer.Interval = this.settings.RefreshNoFlightPlanUpdateInterval * 60 * 1000;
        this.logger.Log(LogLevel.INFO, $"Monitored plan updated to ${this.monitoredFlightPlan.Departure} - ${this.monitoredFlightPlan.Arrival}.");
      }

    }

    public void StartDownloading()
    {
      EAssert.IsNotNull(this.monitoredVatsimId);
      this.updateTimer.Enabled = true;
      Task.Run(InvokeReloadData);
    }

    internal void SetMonitoredVatsimId(string? cid)
    {
      this.monitoredVatsimId = cid;
    }

    private async Task<string> DownloadDataAsync()
    {
      var url = GetRandom(this.settings.Sources); //TODO to ESystem
      string ret = await DownloadContentAsStringAsync(url);
      return ret;
    }

    private static string GetRandom(List<string> sources)
    {
      Random rnd = new();
      int index = rnd.Next(0, sources.Count);
      return sources[index];
    }

    private static async Task<string> DownloadContentAsStringAsync(string url)
    {
      using HttpClient httpClient = new();
      string content = await httpClient.GetStringAsync(url);
      return content;
    }
  }
}
