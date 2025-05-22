using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreBridge
{
  using System;
  using System.IO;

  public class Logger
  {
    public enum LogLevel
    {
      Error,
      Info,
      Warning,
      Debug
    }

    private readonly string logFilePath = ".\\_log.txt";

    public void Log(LogLevel level, string message)
    {
      try
      {
        File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{level}\t{message}{Environment.NewLine}");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Logging failed: {ex.Message}");
      }
    }
  }

}
