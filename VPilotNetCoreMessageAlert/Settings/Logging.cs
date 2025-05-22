using ELogging;

namespace VPilotNetCoreMessageAlert.Settings
{
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
}
