using ESystem.Logging;
using ESystem;
using ESystem.Asserting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPilotNetCoreMessageAlert.Settings;

namespace VPilotNetCoreMessageAlert
{
  public class VatsimDataProvider
  {
    public class MonitoredDataRecord
    {
      public string Departure { get; }
      public string Arrival { get; }
      public int RevisionId { get; set; }

      public MonitoredDataRecord(string departure, string arrival, int revisionId)
      {
        Departure = departure;
        Arrival = arrival;
        RevisionId = revisionId;
      }
    }

    public class FlightPlan
    {
      public string Departure { get; set; }
      public string Arrival { get; set; }
      public int? Revision_id { get; set; }

      public FlightPlan()
      {
      }
    }

    public class Pilot
    {
      public int CID { get; set; }
      public string Callsign { get; set; }
      public double Latitude { get; set; }
      public double Longitude { get; set; }
      public FlightPlan Flight_plan { get; set; }
      public DateTime Last_updated { get; set; }

      public Pilot()
      {
      }
    }

    public class Prefile
    {
      public int CID { get; set; }
      public string Callsign { get; set; }
      public FlightPlan Flight_plan { get; set; }
      public DateTime Last_updated { get; set; }

      public Prefile()
      {
      }
    }

    public class Model
    {
      public List<Pilot> Pilots { get; set; }
      public List<Prefile> Prefiles { get; set; }

      public Model()
      {
      }
    }

    private readonly Vatsim settings;
    private readonly System.Timers.Timer updateTimer;
    private readonly Logger logger;
    private string monitoredVatsimId = null;
    private volatile bool isUpdateInProgress = false;
    private MonitoredDataRecord monitoredData = null;
    public event Action<MonitoredDataRecord> FlightPlanUpdateProcessed;

    public VatsimDataProvider(Vatsim settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsTrue(settings.NoFlightPlanUpdateInterval > 0, nameof(settings), "NoFlightPlanUpdateInterval must be positive integer.");
      EAssert.Argument.IsTrue(settings.RefreshFlightPlanUpdateInterval > 0, nameof(settings), "RefreshFlightPlanUpdateInterval must be positive integer.");

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

    private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      InvokeReloadData();
    }

    public MonitoredDataRecord MonitoredData => this.monitoredData;

    private void InvokeReloadData()
    {
      Task.Run(() => ReloadDataAsync());
    }

    private async Task ReloadDataAsync()
    {
      this.logger.Log(LogLevel.TRACE, "Trying to obtain update flag.");
      if (!TryGetUpdateLock())
      {
        this.logger.Log(LogLevel.ERROR, "Update flag did not expire. Too short updating interval or something went wrong during update?");
        return;
      }

      this.logger.Log(LogLevel.TRACE, "Entering update flag -- success");
      this.logger.Log(LogLevel.INFO, "Reloading VATSIM data");

      try
      {
        var data = await DownloadDataAsync();
        UpdateUserData(data, out bool changed);
        if (changed) FlightPlanUpdateProcessed?.Invoke(this.MonitoredData);
      }
      catch (Exception ex)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to download or update data.");
        this.logger.LogException(ex);
      }
      finally
      {
        this.logger.Log(LogLevel.TRACE, "Exiting update flag.");
        ReleaseUpdateLock();
      }

      this.logger.Log(LogLevel.INFO, "Reloading VATSIM data -- completed.");
    }

    private void ReleaseUpdateLock()
    {
      lock (this.updateTimer)
      {
        this.isUpdateInProgress = false;
      }
    }

    private bool TryGetUpdateLock()
    {
      bool canContinue;
      lock (this.updateTimer)
      {
        if (isUpdateInProgress)
          canContinue = false;
        else
        {
          canContinue = true;
          isUpdateInProgress = true;
        }
      }

      return canContinue;
    }

    private void UpdateUserData(string data, out bool flightPlanUpdated)
    {
      this.logger.Log(LogLevel.TRACE, "Deserializing data");
      flightPlanUpdated = false;
      Model model = JsonConvert.DeserializeObject<Model>(data);
      this.logger.Log(LogLevel.TRACE, "Data deserialized.");
      if (model == null)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to extract vatsim data model from data string of length " + data.Length + ". Skipping.");
        return;
      }

      int mcid = int.Parse(this.monitoredVatsimId);
      this.logger.Log(LogLevel.TRACE, $"Looking for flight-plan for {mcid} between active pilots");
      var pilot = model.Pilots.FirstOrDefault(q => q.CID == mcid);
      FlightPlan fp = null;
      if (pilot != null)
      {
        this.logger.Log(LogLevel.TRACE, "Active pilot Flight-plan found");
        fp = pilot.Flight_plan;
      }
      else
      {
        this.logger.Log(LogLevel.TRACE, $"Looking for flight-plan for {mcid} between prefiles");
        var prefile = model.Prefiles.FirstOrDefault(q => q.CID == mcid);
        if (prefile != null)
        {
          this.logger.Log(LogLevel.TRACE, "Prefiled Flight-plan found");
          fp = prefile.Flight_plan;
          fp.Revision_id = -1;
        }
      }
      if (fp != null)
      {
        if (this.monitoredData == null || this.monitoredData.RevisionId < 0 || this.monitoredData.RevisionId != fp.Revision_id)
        {
          this.logger.Log(LogLevel.TRACE, "Flight-plan will be updated");
          this.monitoredData = new MonitoredDataRecord(fp.Departure, fp.Arrival, fp.Revision_id.Value);
          this.updateTimer.Interval = this.settings.RefreshFlightPlanUpdateInterval * 60 * 1000;
          this.logger.Log(LogLevel.INFO, $"Monitored plan updated as {this.monitoredData.Departure} - {this.monitoredData.Arrival} (rev {this.monitoredData.RevisionId}).");
          flightPlanUpdated = true;
        }
        else
        {
          this.logger.Log(LogLevel.TRACE, "Flight-plan will not be updated");
        }
      }
      else
        this.logger.Log(LogLevel.DEBUG, "No prefile found or update not needed");
    }

    public void StartDownloading()
    {
      EAssert.IsNotNull(this.monitoredVatsimId);
      this.updateTimer.Enabled = true;
      Task.Run(InvokeReloadData);
    }

    internal void SetMonitoredVatsimId(string cid)
    {
      this.monitoredVatsimId = cid;
    }

    private async Task<string> DownloadDataAsync()
    {
      var url = this.settings.Sources.GetRandom();
      this.logger.Log(LogLevel.DEBUG, $"VATSIM source selected: {url}");
      string ret = await DownloadContentAsStringAsync(url);
      return ret;
    }


    private async Task<string> DownloadContentAsStringAsync(string url)
    {
      string content;
      var uri = new System.Uri(url);
      this.logger.Log(LogLevel.TRACE, "Opening web-client");
      using (WebClient webClient = new WebClient())
      {
        this.logger.Log(LogLevel.TRACE, "Downloading data... awaiting...");
        content = await webClient.DownloadStringTaskAsync(uri);
        this.logger.Log(LogLevel.TRACE, $"Downloading data... completed, got {content.Length} chars.");
      }
      return content;
    }
  }
}
