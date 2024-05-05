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

    public Event(EventAction action, File file)
    {
      Action = action;
      File = file;
    }

    public EventAction Action { get; set; }
    public File File { get; set; } = null!;
  }

  public class File
  {
    public File()
    {
    }

    public File(string name, double volume)
    {
      Name = name;
      Volume = volume;
    }

    public string Name { get; set; } = null!;
    public double Volume { get; set; }
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

    public string FileName { get; set; } = null!;
    public LogLevel Level { get; set; }
  }
  public class Monitors
  {
    public Monitors()
    {
    }

    public Monitors(List<string> vatsimIds)
    {
      VatsimIds = vatsimIds;
    }

    public List<string> VatsimIds { get; set; } = null!;

  }
  public class Root
  {
    public Root()
    {
    }

    public Root(List<Event> events, Logging logging, Monitors monitors)
    {
      Events = events;
      Logging = logging;
      Monitors = monitors;
    }

    public List<Event> Events { get; set; } = null!;
    public Logging Logging { get; set; } = null!;
    public Monitors Monitors { get; set; } = null!;
  }
}
