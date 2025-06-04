using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RossCarlson.Vatsim.Vpilot.Plugins;
//using VPilotNetCoreBridge.Mock;

namespace VPilotNetCoreBridge
{
  public class VPilotPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin, IDisposable
  {
    public enum Result
    {
      Success,
      Error
    }

    private readonly Logger logger = new Logger();
    private IBroker broker;
    private ServerProxy serverProxy;

    public string Name => "VPilotNetCoreBridge";

    public void Initialize(IBroker broker)
    {
      logger.Clear();
      {
        string ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        string fileVer = fvi.FileVersion;
        logger.Log(Logger.LogLevel.Info, $"Initializing VPilotPlugin (ver {ver}/{fileVer})...");
      }

      if (LoadConfig(out Config config) == Result.Error)
      {
        logger.Log(Logger.LogLevel.Error, "Failed to load configuration. Plugin will not start.");
        return;
      }

      logger.Log(Logger.LogLevel.Info, "Starting client...");
      if (StartClient(config) == Result.Error)
      {
        logger.Log(Logger.LogLevel.Error, "Failed to load client. Plugin will not start.");
        return;
      }

      this.broker = broker;

      logger.Log(Logger.LogLevel.Info, "Building Server Proxy.");
      this.serverProxy = new ServerProxy(
        config.PipeId, 
        config.ProcessAircraftRelatedEvents, 
        config.ConnectTimeout,
        broker);

      logger.Log(Logger.LogLevel.Info, "Initialization completed.");

      this.broker.NetworkConnected += DoDebug;
    }

    private void DoDebug(object sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.NetworkConnectedEventArgs e)
    {
      Thread.Sleep(1000); // Give the client some time to connect
      //serverProxy.DebugSendContactMe();
      serverProxy.DebugSendRadioMessage("LKMT");

    }

    private Result StartClient(Config config)
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
      try
      {
        p.Start();
      }
      catch (Exception ex)
      {
        logger.Log(Logger.LogLevel.Error, $"Failed to start client '{psi.FileName}': {ex.Message}");
        return Result.Error;
      }

      return Result.Success;
    }

    private Result LoadConfig(out Config config)
    {
      string configFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.config.json";
      configFileName = System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
        configFileName);

      if (System.IO.File.Exists(configFileName) == false)
      {
        logger.Log(Logger.LogLevel.Error, $"Config file {configFileName} not found.");
        config = null;
        return Result.Error;
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
        config = null;
        return Result.Error;
      }

      if (System.IO.File.Exists(ret.ClientExe) == false)
      {
        logger.Log(Logger.LogLevel.Error, $"Client executable {ret.ClientExe} ({System.IO.Path.GetFullPath(ret.ClientExe)}) not found.");
        config = null;
        return Result.Error;
      }
      if (ret.PipeId.Length == 0)
      {
        logger.Log(Logger.LogLevel.Error, $"Pipe ID {ret.PipeId} is empty.");
        config = null;
        return Result.Error;
      }

      config = ret;
      return Result.Success;
    }

    public void Dispose()
    {
      logger.Log(Logger.LogLevel.Info, "Disposing VPilotPlugin...");
    }
  }
}
