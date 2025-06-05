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
using Eng.VPilotNotifications.Settings;
using Eng.VPilotNetCoreModule;

namespace Eng.VPilotNotifications.Vatsim
{
  public class VatsimFlightPlanProvider
  {
    private record ConnectionInfo(int Cid, string Callsign);

    private readonly VatsimConfig settings;
    private readonly System.Timers.Timer updateTimer;
    private readonly Logger logger;
    private static volatile bool isUpdateInProgress = false;
    private ConnectionInfo? connectionInfo = null;

    public record FlightPlanUpdatedEventArgs(FlightPlan? Previous, FlightPlan? Current);
    public delegate void FlightPlanUpdatedHandler(FlightPlanUpdatedEventArgs e);
    public FlightPlan? CurrentFlightPlan { get; private set; }

    public VatsimFlightPlanProvider(ClientProxyBroker broker, VatsimConfig settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsTrue(settings.NoFlightPlanUpdateInterval > 0, nameof(settings), "NoFlightPlanUpdateInterval must be positive integer.");
      EAssert.Argument.IsTrue(settings.RefreshFlightPlanUpdateInterval > 0, nameof(settings), "RefreshFlightPlanUpdateInterval must be positive integer.");
      logger = Logger.Create(nameof(VatsimFlightPlanProvider));
      this.settings = settings;

      updateTimer = new System.Timers.Timer()
      {
        AutoReset = false,
        Interval = settings.NoFlightPlanUpdateInterval * 60 * 1000,
        Enabled = false
      };
      updateTimer.Elapsed += (s, e) => InvokeUpdateFlightPlan();

      broker.NetworkConnected += (s, e) =>
      {
        this.connectionInfo = new ConnectionInfo(int.Parse(e.Cid), e.Callsign);
        this.InvokeUpdateFlightPlan();
      };
      broker.NetworkDisconnected += (s, e) =>
      {
        this.connectionInfo = null;
        this.InvokeUpdateFlightPlan();
      };

      logger.Log(LogLevel.INFO, $"Created with timer interval {updateTimer.Interval}");
    }

    public void InvokeUpdateFlightPlan()
    {
      Task.Run(() => UpdateFlightPlan());
    }

    private async Task UpdateFlightPlan()
    {
      logger.Log(LogLevel.INFO, "UpdateFlightPlan called.");

      ConnectionInfo? ci = this.connectionInfo;
      if (ci == null)
        EraseFlightPlan();
      else
        await ReloadFlightPlanAsync(ci);

      this.updateTimer.Interval = this.CurrentFlightPlan == null
        ? this.settings.NoFlightPlanUpdateInterval * 60 * 1000
        : this.settings.RefreshFlightPlanUpdateInterval * 60 * 1000;
      this.updateTimer.Start();
    }

    private void EraseFlightPlan()
    {
      logger.Log(LogLevel.INFO, "VATSIM ID is not set - user not connected.");
      this.CurrentFlightPlan = null;
    }

    private async Task ReloadFlightPlanAsync(ConnectionInfo ci)
    {
      logger.Log(LogLevel.DEBUG, $"VATSIM ID is set to {ci.Cid}, Callsign to {ci.Callsign}. Starting flight plan update process.");
      if (!TryGetUpdateLock())
      {
        logger.Log(LogLevel.ERROR, "Update flag did not expire. Too short updating interval or something went wrong during update? Aborting.");
        return;
      }

      logger.Log(LogLevel.TRACE, "Entering update flag -- success");
      logger.Log(LogLevel.INFO, "Reloading VATSIM data");

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
        ReleaseUpdateLock();
        return;
      }

      FlightPlan? newFlightPlan = SelectFlightPlanByVatsimId(model, ci);
      if (newFlightPlan != null)
      {
        logger.Log(LogLevel.INFO, $"Flight plan for VATSIM ID {ci.Cid} found: {ci.Callsign} :: {newFlightPlan.Departure} -> {newFlightPlan.Arrival}. Setting it as current flight plan.");
        this.CurrentFlightPlan = newFlightPlan;
      }
      else
      {
        logger.Log(LogLevel.INFO, $"No flight plan found for VATSIM ID {ci.Cid} & callsign {ci.Callsign}.");
      }

      ReleaseUpdateLock();
    }

    private FlightPlan? SelectFlightPlanByVatsimId(Model model, ConnectionInfo ci)
    {
      logger.Log(LogLevel.TRACE, "Picking up flight plan from model.");
      FlightPlan? flightPlan = null;
      if (model.Pilots != null && model.Pilots.Count > 0)
      {
        logger.Log(LogLevel.TRACE, "Searching for flight plan in active pilots.");
        var pilot = model.Pilots.FirstOrDefault(q => q.CID == ci.Cid && q.Callsign == ci.Callsign);
        if (pilot != null)
        {
          logger.Log(LogLevel.TRACE, "Flight plan found in active pilots.");
          flightPlan = pilot.Flight_plan;
        }
      }

      if (flightPlan == null && model.Prefiles != null && model.Prefiles.Count > 0)
      {
        logger.Log(LogLevel.TRACE, "Searching for flight plan in prefiled plans.");
        var prefile = model.Prefiles.FirstOrDefault(q => q.CID == ci.Cid && q.Callsign == ci.Callsign);
        if (prefile != null)
        {
          logger.Log(LogLevel.TRACE, "Flight plan found in prefiled plans.");
          flightPlan = prefile.Flight_plan;
          flightPlan.RevisionId = -1; // Set revision ID to -1 for prefiled plans
        }
      }

      if (flightPlan == null)
        logger.Log(LogLevel.TRACE, "No flight plan found in model.");
      else
        logger.Log(LogLevel.TRACE, $"Flight plan found: {flightPlan.Departure} -> {flightPlan.Arrival}, Revision ID: {flightPlan.RevisionId}");

      return flightPlan;
    }

    private void ReleaseUpdateLock()
    {
      lock (updateTimer)
      {
        isUpdateInProgress = false;
      }
    }

    private bool TryGetUpdateLock()
    {
      bool canContinue;
      lock (updateTimer)
      {
        DateTime endTime = DateTime.Now.AddSeconds(5);
        while (isUpdateInProgress && DateTime.Now < endTime)
        {
          // Wait for the update to finish or timeout
          Thread.Sleep(100);
        }
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
  }
}
