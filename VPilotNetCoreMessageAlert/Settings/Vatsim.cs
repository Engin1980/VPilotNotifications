using System.Collections.Generic;

namespace VPilotNetCoreMessageAlert.Settings
{
  public class Vatsim
  {
    public List<string> Sources { get; set; } = new List<string>();
    public int NoFlightPlanUpdateInterval { get; set; } = 100;
    public int RefreshFlightPlanUpdateInterval { get; set; } = 100;

  }
}
