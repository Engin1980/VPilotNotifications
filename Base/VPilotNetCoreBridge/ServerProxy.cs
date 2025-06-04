using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RossCarlson.Vatsim.Vpilot.Plugins;
using System.Diagnostics.SymbolStore;
//using VPilotNetCoreBridge.Mock;

namespace VPilotNetCoreBridge
{
  internal class ServerProxy : IDisposable
  {
    private readonly string pipePrefix;
    private readonly CancellationTokenSource methodListeningTaskCancelationTokenSource = new CancellationTokenSource();
    private readonly Task methodListeningTask;
    private readonly IBroker broker;
    private static readonly Logger logger = new Logger("ServerProxy");

    private readonly static Dictionary<string, Action<IBroker, Dictionary<string, object>>> incomingMethodHandlers;

    static ServerProxy()
    {
      logger.Log(Logger.LogLevel.Info, "Initializing static ServerProxy handlers...");

      incomingMethodHandlers = new Dictionary<string, Action<IBroker, Dictionary<string, object>>>();

      incomingMethodHandlers[nameof(IBroker.RequestDisconnect)] = (broker, args) =>
      {
        broker.RequestDisconnect();
      };

      incomingMethodHandlers[nameof(IBroker.SendPrivateMessage)] = (broker, args) =>
      {
        string to = (string)args["to"];
        string message = (string)args["message"];
        broker.SendPrivateMessage(to, message);
      };

      incomingMethodHandlers[nameof(IBroker.PostDebugMessage)] = (broker, args) =>
      {
        string message = (string)args["message"];
        broker.PostDebugMessage(message);
      };

      incomingMethodHandlers[nameof(IBroker.RequestAtis)] = (broker, args) =>
      {
        string callsign = (string)args["callsign"];
        broker.RequestAtis(callsign);
      };

      incomingMethodHandlers[nameof(IBroker.RequestConnect)] = (broker, args) =>
      {
        broker.RequestConnect(
          (string)args["callsign"],
          (string)args["typeCode"],
          (string)args["selcalCode"]
        );
      };

      incomingMethodHandlers[nameof(IBroker.RequestMetar)] = (broker, args) =>
      {
        string station = (string)args["station"];
        broker.RequestMetar(station);
      };

      incomingMethodHandlers[nameof(IBroker.SendRadioMessage)] = (broker, args) =>
      {
        string message = (string)args["message"];
        broker.SendRadioMessage(message);
      };

      incomingMethodHandlers[nameof(IBroker.SetModeC)] = (broker, args) =>
      {
        bool modeC = (bool)args["modeC"];
        broker.SetModeC(modeC);
      };

      incomingMethodHandlers[nameof(IBroker.SquawkIdent)] = (broker, args) =>
      {
        broker.SquawkIdent();
      };

      incomingMethodHandlers[nameof(IBroker.SetPtt)] = (broker, args) =>
      {
        bool pressed = (bool)args["pressed"];
        broker.SetPtt(pressed);
      };

      incomingMethodHandlers[nameof(IBroker.RequestDisconnect)] = (broker, args) =>
      {
        broker.RequestDisconnect();
      };

      incomingMethodHandlers[nameof(IBroker.RequestDisconnect)] = (broker, args) =>
      {
        broker.RequestDisconnect();
      };

      incomingMethodHandlers[nameof(IBroker.SendPrivateMessage)] = (broker, args) =>
      {
        string to = (string)args["to"];
        string message = (string)args["message"];
        broker.SendPrivateMessage(to, message);
      };

      // check everything is registered
      var methods = typeof(IBroker)
        .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
        .Select(q => q.Name)
        .Where(q => q.Contains("_") == false)
        .ToList();
      methods.Remove("ToString");
      methods.Remove("GetHashCode");
      methods.RemoveAll(q => incomingMethodHandlers.ContainsKey(q));

      if (methods.Count > 0)
        logger.Log(Logger.LogLevel.Error, $"Missing handlers for methods: {string.Join(", ", methods)}");

      logger.Log(Logger.LogLevel.Info, "Static ServerProxy handlers initialized.");
    }

    public ServerProxy(string pipePrefix, bool processAircraftRelatedEvents, IBroker broker)
    {
      logger.Log(Logger.LogLevel.Info, "Creating ServerProxy with pipe prefix: " + pipePrefix);
      this.pipePrefix = pipePrefix;
      this.broker = broker;
      logger.Log(Logger.LogLevel.Info, "Registering method listeners in nested async task.");
      this.methodListeningTask = Task.Run(() => ListenForMethods(methodListeningTaskCancelationTokenSource.Token));
      logger.Log(Logger.LogLevel.Info, "Registering event listeners.");
      RegisterEventListeners(processAircraftRelatedEvents);
      logger.Log(Logger.LogLevel.Info, "ServerProxy creation completed.");
    }

    private async Task ListenForMethods(CancellationToken token)
    {
      while (!token.IsCancellationRequested)
      {
        try
        {
          string pipeName = this.pipePrefix + "ProxyMethodPipe";
          using (var pipe = new NamedPipeServerStream(pipeName))
          {
            await pipe.WaitForConnectionAsync(token);

            string json;
            using (var reader = new StreamReader(pipe, Encoding.UTF8))
            {
              json = await reader.ReadToEndAsync();
            }

            var evt = JsonConvert.DeserializeObject<ProxyMessage>(json);
            EAssert.IsTrue(evt != null, "evt is null");
            EAssert.IsTrue(evt.Type == ProxyMessage.METHOD_CALL, "evt.Type is not METHOD_CALL");
            EAssert.IsTrue(incomingMethodHandlers.TryGetValue(evt.Method, out var handler), $"No handler for method {evt.Method}");
            handler.Invoke(broker, evt.Args);
          }
        }
        catch (OperationCanceledException ex)
        {
          // Expected on shutdown
          break;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Method listener error: {ex.Message}");
          await Task.Delay(1000, token); // prevent tight loop on error
        }
      }
    }

    private void RegisterEventListeners(bool processAircraftRelatedEvents)
    {
      if (processAircraftRelatedEvents)
      {
        this.broker.AircraftAdded += (b, e) => HandleInvokedEvent(nameof(IBroker.AircraftAdded), e);
        this.broker.AircraftDeleted += (b, e) => HandleInvokedEvent(nameof(IBroker.AircraftDeleted), e);
        this.broker.AircraftUpdated += (b, e) => HandleInvokedEvent(nameof(IBroker.AircraftUpdated), e);
      }
      this.broker.AtisReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.AtisReceived), e);
      this.broker.BroadcastMessageReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.BroadcastMessageReceived), e);
      this.broker.ControllerAdded += (b, e) => HandleInvokedEvent(nameof(IBroker.ControllerAdded), e);
      this.broker.ControllerDeleted += (b, e) => HandleInvokedEvent(nameof(IBroker.ControllerDeleted), e);
      this.broker.ControllerFrequencyChanged += (b, e) => HandleInvokedEvent(nameof(IBroker.ControllerFrequencyChanged), e);
      this.broker.ControllerLocationChanged += (b, e) => HandleInvokedEvent(nameof(IBroker.ControllerLocationChanged), e);
      this.broker.MetarReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.MetarReceived), e);
      this.broker.NetworkConnected += (b, e) => HandleInvokedEvent(nameof(IBroker.NetworkConnected), e);
      this.broker.NetworkDisconnected += (b, e) => HandleInvokedEvent(nameof(IBroker.NetworkDisconnected), e);
      this.broker.PrivateMessageReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.PrivateMessageReceived), e);
      this.broker.RadioMessageReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.RadioMessageReceived), e);
      this.broker.SelcalAlertReceived += (b, e) => HandleInvokedEvent(nameof(IBroker.SelcalAlertReceived), e);
      this.broker.SessionEnded += (b, e) => HandleInvokedEvent(nameof(IBroker.SessionEnded), e);
      this.broker.SessionEnded += (b, e) => this.methodListeningTaskCancelationTokenSource.Cancel();
    }

    private void HandleInvokedEvent(string eventName, object eventArgs)
    {
      logger.Log(Logger.LogLevel.Debug, $"Handling event: {eventName}");
      Dictionary<string, object> args = eventArgs.GetType()
        .GetProperties()
        .ToDictionary(prop => prop.Name, prop => prop.GetValue(eventArgs, null));
      var msg = new ProxyMessage(ProxyMessage.EVENT, eventName, args);
      var json = JsonConvert.SerializeObject(msg);

      string pipeName = this.pipePrefix + "ProxyEventPipe";
      logger.Log(Logger.LogLevel.Debug, $"Sending via pipe {pipeName}");
      using (var pipe = new NamedPipeClientStream(pipeName))
      {
        pipe.Connect();
        using (var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true })
        {
          writer.Write(json);
        }
      }
      logger.Log(Logger.LogLevel.Debug, $"Handling event: {eventName} - completed");
    }

    public void Dispose()
    {
      methodListeningTaskCancelationTokenSource.Cancel();
      try { methodListeningTask.Wait(); } catch { /* ignore */ }
      methodListeningTaskCancelationTokenSource.Dispose();
    }

    internal void DebugSendContactMe()
    {
      PrivateMessageReceivedEventArgs e = new PrivateMessageReceivedEventArgs()
      {
        From = "DEBUG",
        Message = "Contact me on at 123.450"
      };
      HandleInvokedEvent(nameof(IBroker.PrivateMessageReceived), e);
    }

    internal void DebugSendRadioMessage(string v)
    {
      int[] freqs = new int[1];
      freqs[0] = 12344500; // Example frequency in Hz
      RadioMessageReceivedEventArgs e = new RadioMessageReceivedEventArgs()
      {
        Frequencies = freqs,
        From = "DEBUG",
        Message = "hudla prdla " + v + " smudla dudla"
      };
      HandleInvokedEvent(nameof(IBroker.RadioMessageReceived), e);
    }
  }
}
