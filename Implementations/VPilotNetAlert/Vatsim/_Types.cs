using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Vatsim
{
  public class FlightPlan
  {
    public string Departure { get; set; } = null!;
    public string Arrival { get; set; } = null!;
    public int? Revision_id { get; set; }
    public int? RevisionId
    {
      get => Revision_id;
      set => Revision_id = value;
    }

    public FlightPlan()
    {
    }
  }

  public class Pilot
  {
    public int CID { get; set; }
    public string Callsign { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public FlightPlan Flight_plan { get; set; } = null!;
    public DateTime Last_updated { get; set; }

    public Pilot()
    {
    }
  }

  public class Prefile
  {
    public int CID { get; set; }
    public string Callsign { get; set; } = null!;
    public FlightPlan Flight_plan { get; set; } = null!;
    public DateTime Last_updated { get; set; }

    public Prefile()
    {
    }
  }

  public class Model
  {
    public List<Pilot> Pilots { get; set; } = null!;
    public List<Prefile> Prefiles { get; set; } = null!;

    public Model()
    {
    }
  }
}
