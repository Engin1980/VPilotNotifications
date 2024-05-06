using ELogging;
using ESystem;
using ESystem.Asserting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using VPilotMessageAlert.Settings;

namespace VPilotMessageAlert
{
  public class VatsimDataProvider
  {
    public record MonitoredDataRecord(string Callsign, string Departure, string Arrival, DateTime LastUpdated);
    public record FlightPlan(string Departure, string Arrival);
    public record Pilot(int CID, string Callsign, double Latitude, double Longitude);
    public record Prefile(int CID, string Callsign, FlightPlan Flight_plan, DateTime Last_updated);
    public record Model(List<Pilot> Pilots, List<Prefile> Prefiles);

    private readonly Vatsim settings;
    private readonly System.Timers.Timer updateTimer;
    private readonly Logger logger;
    private string? monitoredVatsimId = null;
    private MonitoredDataRecord? monitoredData = null;

    public VatsimDataProvider(Vatsim settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsTrue(settings.NoFlightPlanUpdateInterval > 0);
      EAssert.Argument.IsTrue(settings.RefreshNoFlightPlanUpdateInterval > 0);

      this.logger = Logger.Create(nameof(VatsimDataProvider));

      this.settings = settings;
      this.updateTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = settings.NoFlightPlanUpdateInterval * 60 * 1000,
        Enabled = false
      };
      this.updateTimer.Elapsed += UpdateTimer_Elapsed;

      this.logger.Log(LogLevel.INFO, "Created, with timer interval " + this.updateTimer.Interval);
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      InvokeReloadData();
    }

    public MonitoredDataRecord? MonitoredData => this.monitoredData;

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
      if (prefile != null && (this.monitoredData == null || this.monitoredData.LastUpdated != prefile.Last_updated))
      {
        this.monitoredData = new(prefile.Callsign, prefile.Flight_plan.Departure, prefile.Flight_plan.Arrival, prefile.Last_updated);
        this.updateTimer.Interval = this.settings.RefreshNoFlightPlanUpdateInterval * 60 * 1000;
        this.logger.Log(LogLevel.INFO, $"Monitored plan updated for {this.monitoredData.Callsign} as {this.monitoredData.Departure} - {this.monitoredData.Arrival}.");
      } else
        this.logger.Log(LogLevel.DEBUG, "No prefile found or update not needed");
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
      var url = this.settings.Sources.GetRandom();
      string ret = await DownloadContentAsStringAsync(url);
      return ret;
    }


    private static async Task<string> DownloadContentAsStringAsync(string url)
    {
      using HttpClient httpClient = new();
      string content = await httpClient.GetStringAsync(url);
      return content;
    }
  }
}
