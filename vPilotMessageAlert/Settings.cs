using ELogging;
using ESystem.Asserting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotMessageAlert.Settings
{
  public enum EventAction
  {
    Unused,
    Connected,
    Disconnected,
    RadioMessage,
    SelcalAlert
  }

  public class Event
  {
    public Event()
    {
    }

    public EventAction Action { get; set; } = EventAction.Unused;
    public File File { get; set; } = new();
  }

  public class File
  {
    public File()
    {
    }

    public string Name { get; set; } = "";
    public double Volume { get; set; } = 1;
  }

  public class Logging
  {
    public Logging()
    {
    }

    public Logging(string fileName, LogLevel level)
    {
      FileName = fileName;
      Level = level;
    }

    public string FileName { get; set; } = "_log.txt";
    public LogLevel Level { get; set; } = LogLevel.TRACE;
  }
  public class Root
  {
    public Root()
    {
    }

    public Root(Logging logging)
    {
      Logging = logging;
    }

    public List<Event> Events { get; set; } = new();
    public Logging Logging { get; set; } = new();
    public Vatsim Vatsim { get; set; } = new();
  }

  public class Vatsim
  {
    public List<string> Sources { get; set; } = new();
    public int NoFlightPlanUpdateInterval { get; set; } = int.MaxValue;
    public int RefreshNoFlightPlanUpdateInterval { get; set; } = int.MaxValue;

  }
}
