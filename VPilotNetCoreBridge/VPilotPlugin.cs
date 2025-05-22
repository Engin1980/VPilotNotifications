using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreBridge
{
  public class VPilotPlugin
  {
    private readonly Logger logger = new Logger();
    private IBroker broker;
    private ServerProxy serverProxy;

    public void Initialize(IBroker broker)
    {
      logger.Clear();
      logger.Log(Logger.LogLevel.Info, "Initializing VPilotPlugin...");

      Config config = LoadConfig();
      if (config == null)
      {
        logger.Log(Logger.LogLevel.Error, "Failed to load configuration. Plugin will not start.");
        return;
      }

      logger.Log(Logger.LogLevel.Info, "Starting client...");
      StartClient(config);

      this.broker = broker;
      this.serverProxy = new ServerProxy(config.ClientExe, broker);
    }

    private void StartClient(Config config)
    {
      ProcessStartInfo psi = new ProcessStartInfo()
      {
        FileName = config.ClientExe,
        Arguments = $"{config.PipeId}"
      };
      
      if (config.ShowClientConsole == false)
      {
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
      }

      Process p = new Process()
      {
        StartInfo = psi
      };
      p.Start();
    }

    private Config LoadConfig()
    {
      string configFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.config.json";

      if (System.IO.File.Exists(configFileName) == false)
      {
        logger.Log(Logger.LogLevel.Error, $"Config file {configFileName} not found.");
        return null;
      }

      Config ret;
      try
      {
        string s = System.IO.File.ReadAllText(configFileName);
        ret = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(s);
      }
      catch (Exception ex)
      {
        logger.Log(Logger.LogLevel.Error, $"Failed to read config file {configFileName}: {ex.Message}");
        return null;
      }

      if (System.IO.File.Exists(ret.ClientExe) == false)
      {
        logger.Log(Logger.LogLevel.Error, $"Client executable {ret.ClientExe} ({System.IO.Path.GetFullPath(ret.ClientExe)}) not found.");
        return null;
      }
      if (ret.PipeId.Length == 0)
      {
        logger.Log(Logger.LogLevel.Error, $"Pipe ID {ret.PipeId} is empty.");
        return null;
      }

      return ret;
    }
  }
}
