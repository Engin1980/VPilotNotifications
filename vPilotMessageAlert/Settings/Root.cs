using ELogging;
using ESystem;
using ESystem.Asserting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace VPilotMessageAlert.Settings
{
  public class Root
  {
    public Root()
    {
    }

    public Root(Logging logging)
    {
      Logging = logging;
    }

    public List<Event> Events { get; set; } = new List<Event>();
    public Logging Logging { get; set; } = new Logging();
    public Vatsim Vatsim { get; set; } = new Vatsim();
    public Behavior Behavior { get; set; } = new Behavior();

    internal static Root Load()
    {
      // No usage of logging here! Log not ready yet!
      bool isPluginMode = System.IO.File.Exists("Plugins\\settings.json");
      string configFileName = isPluginMode ? "Plugins\\settings.json" : "settings.json";
      string s = System.IO.File.ReadAllText(configFileName);
      Root ret = JsonConvert.DeserializeObject<VPilotMessageAlert.Settings.Root>(s);
      if (isPluginMode) ret.AdjustPaths();
      ret.Validate(Logger.Create(typeof(VPilotPlugin)));
      return ret;
    }

    private void Validate(Logger logger)
    {
      bool isFailed = false;

      void Fail(string message)
      {
        isFailed = true;
        logger.Log(LogLevel.CRITICAL, "Settings issue: " + message);
      }

      if (this.Events == null) Fail("List of events is undefined.");
      foreach (Event e in this.Events)
      {
        if (e.File == null) Fail($"File of event {e.Action} is undefined.");
        if (System.IO.File.Exists(e.File.Name) == false) Fail($"File '{e.File.Name}' for event '{e.Action}' not found.");
        if (!(0 <= e.File.Volume && e.File.Volume <= 1)) Fail($"Volume for event '{e.Action}' must be between 0..1");
      }

      if (this.Logging == null) Fail("Logging is undefined.");
      if (string.IsNullOrEmpty(this.Logging.FileName)) Fail("Logging file is undefined or empty.");

      if (this.Vatsim == null) Fail("Vatsim is undefined.");
      if (this.Vatsim.Sources == null) Fail("Vatsim sources are undefined.");
      if (this.Vatsim.Sources.Count == 0) Fail("Vatsim sources are empty.");

      if (this.Vatsim.NoFlightPlanUpdateInterval <= 0) Fail("Vatsim no-flightplan-update-interval must be integer greater than zero.");
      if (this.Vatsim.RefreshFlightPlanUpdateInterval <= 0) Fail("Vatsim refresh-flightplan-update-interval must be integer greater than zero.");

      if (isFailed)
        throw new ApplicationException("Settings check failed. Unable to use settings.");
    }

    private void AdjustPaths()
    {
      if (this.Logging.FileName != null) this.Logging.FileName = System.IO.Path.Combine("Plugins", this.Logging.FileName);
      foreach (var evt in this.Events.Where(q => q.File != null && q.File.Name != null))
      {
        evt.File.Name = System.IO.Path.Combine("Plugins", evt.File.Name);
      }
    }
  }
}
