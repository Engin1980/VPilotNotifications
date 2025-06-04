using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Eng.VPilotNetCoreBridge
{
  public class Logger
  {
    public enum LogLevel
    {
      Error,
      Info,
      Warning,
      Debug
    }

    private readonly string logFilePath = ".\\Plugins\\_VPilotNetCoreBridge.log";
    private readonly string prefix = "";

    public Logger(string prefix = "")
    {
      this.prefix = prefix;
    }

    public void Log(LogLevel level, string message)
    {
      message = string.IsNullOrEmpty(prefix) ? message : $"{prefix}: {message}";
      try
      {
        File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{level}\t{message}{Environment.NewLine}");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Logging failed: {ex.Message}");
      }
    }

    public void Clear()
    {
      try
      {
        if (File.Exists(logFilePath))
        {
          File.Delete(logFilePath);
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Failed to clear log file: {ex.Message}");
      }
    }
  }

}
