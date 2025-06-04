using ESystem.Asserting;
using ESystem.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO.IsolatedStorage;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreModule
{
  public class ClientProxyBroker : IProxyBroker
  {
    private readonly Logger logger = Logger.Create(nameof(ClientProxyBroker));
    private readonly string pipeID;
    private readonly CancellationTokenSource eventListeningTaskCancelationTokenSource = new();
    private readonly Task eventListeningTask;

    public event EventHandler? SessionEnded;
    public event EventHandler? NetworkDisconnected;
    public event EventHandler<AircraftAddedEventArgs>? AircraftAdded;
    public event EventHandler<NetworkConnectedEventArgs>? NetworkConnected;
    public event EventHandler<PrivateMessageReceivedEventArgs>? PrivateMessageReceived;
    public event EventHandler<RadioMessageReceivedEventArgs>? RadioMessageReceived;
    public event EventHandler<BroadcastMessageReceivedEventArgs>? BroadcastMessageReceived;
    public event EventHandler<MetarReceivedEventArgs>? MetarReceived;
    public event EventHandler<AtisReceivedEventArgs>? AtisReceived;
    public event EventHandler<ControllerAddedEventArgs>? ControllerAdded;
    public event EventHandler<ControllerDeletedEventArgs>? ControllerDeleted;
    public event EventHandler<ControllerFrequencyChangedEventArgs>? ControllerFrequencyChanged;
    public event EventHandler<ControllerLocationChangedEventArgs>? ControllerLocationChanged;
    public event EventHandler<SelcalAlertReceivedEventArgs>? SelcalAlertReceived;
    public event EventHandler<AircraftUpdatedEventArgs>? AircraftUpdated;
    public event EventHandler<AircraftDeletedEventArgs>? AircraftDeleted;

    private readonly Dictionary<string, Action<Dictionary<string, object>>> eventHandlers;

    public void SendPrivateMessage(string to, string message)
    {
      Dictionary<string, object> args = BuildArgsDictionary(nameof(to), to, nameof(message), message);
      SendViaPipe(nameof(SendPrivateMessage), args);
    }

    private void SendViaPipe(string methodName, Dictionary<string, object>? args)
    {
      logger.Log(LogLevel.INFO, $"Invoking '{methodName}({args})");
      args ??= new();
      var msg = new ProxyMessage(ProxyMessage.METHOD_CALL, methodName, args);
      string json = JsonConvert.SerializeObject(msg);
      string pipeName = this.pipeID + "ProxyMethodPipe";

      logger.Log(LogLevel.DEBUG, $"Sending message to pipe {pipeName}: {json}");
      using var pipe = new NamedPipeClientStream(pipeName);
      pipe.Connect();

      using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
      writer.Write(json);
      writer.Flush();
      writer.Close();
    }

    private Dictionary<string, object> BuildArgsDictionary(params object[] data)
    {
      Dictionary<string, object> ret = new();

      int i = 0;
      while (i < data.Length)
      {
        string key = (string)data[i];
        object value = data[i + 1];
        ret[key] = value;
        i += 2;
      }

      return ret;
    }

    public void RequestDisconnect()
    {
      SendViaPipe(nameof(RequestDisconnect), null);
    }

    public ClientProxyBroker(string pipePrefix)
    {
      this.pipeID = pipePrefix;
      this.eventListeningTask = Task.Run(() => DoEventListening(eventListeningTaskCancelationTokenSource.Token));
      this.eventHandlers = PrepareEventHandlers();
    }

    private Dictionary<string, Action<Dictionary<string, object>>> PrepareEventHandlers()
    {
      Dictionary<string, Action<Dictionary<string, object>>> ret = new();

      ret[nameof(IProxyBroker.AircraftAdded)] = (args) =>
      {
        var aircraftAddedArgs = BuildObject<AircraftAddedEventArgs>(args);
        this.AircraftAdded?.Invoke(this, aircraftAddedArgs);
      };

      ret[nameof(IProxyBroker.AircraftDeleted)] = (args) =>
      {
        var aircraftDeletedArgs = BuildObject<AircraftDeletedEventArgs>(args);
        this.AircraftDeleted?.Invoke(this, aircraftDeletedArgs);
      };

      ret[nameof(IProxyBroker.AircraftUpdated)] = (args) =>
      {
        var aircraftUpdatedArgs = BuildObject<AircraftUpdatedEventArgs>(args);
        this.AircraftUpdated?.Invoke(this, aircraftUpdatedArgs);
      };

      ret[nameof(IProxyBroker.SelcalAlertReceived)] = (args) =>
      {
        var selcalAlertReceivedArgs = BuildObject<SelcalAlertReceivedEventArgs>(args);
        this.SelcalAlertReceived?.Invoke(this, selcalAlertReceivedArgs);
      };

      ret[nameof(IProxyBroker.MetarReceived)] = (args) =>
      {
        var metarReceivedArgs = BuildObject<MetarReceivedEventArgs>(args);
        this.MetarReceived?.Invoke(this, metarReceivedArgs);
      };

      ret[nameof(IProxyBroker.BroadcastMessageReceived)] = (args) =>
      {
        var broadcastMessageReceivedArgs = BuildObject<BroadcastMessageReceivedEventArgs>(args);
        this.BroadcastMessageReceived?.Invoke(this, broadcastMessageReceivedArgs);
      };

      ret[nameof(IProxyBroker.RadioMessageReceived)] = (args) =>
      {
        var radioMessageReceivedArgs = BuildObject<RadioMessageReceivedEventArgs>(args);
        this.RadioMessageReceived?.Invoke(this, radioMessageReceivedArgs);
      };

      ret[nameof(IProxyBroker.NetworkConnected)] = (args) =>
      {
        var networkConnectedArgs = BuildObject<NetworkConnectedEventArgs>(args);
        this.NetworkConnected?.Invoke(this, networkConnectedArgs);
      };

      ret[nameof(IProxyBroker.NetworkDisconnected)] = (args) =>
      {
        this.NetworkDisconnected?.Invoke(this, EventArgs.Empty);
      };

      ret[nameof(IProxyBroker.AtisReceived)] = (args) =>
      {
        var atisReceivedArgs = BuildObject<AtisReceivedEventArgs>(args);
        this.AtisReceived?.Invoke(this, atisReceivedArgs);
      };

      ret[nameof(IProxyBroker.PrivateMessageReceived)] = (args) =>
      {
        var privateMessageReceivedArgs = BuildObject<PrivateMessageReceivedEventArgs>(args);
        this.PrivateMessageReceived?.Invoke(this, privateMessageReceivedArgs);
      };

      ret[nameof(IProxyBroker.RadioMessageReceived)] = (args) =>
      {
        var radioMessageReceivedArgs = BuildObject<RadioMessageReceivedEventArgs>(args);
        this.RadioMessageReceived?.Invoke(this, radioMessageReceivedArgs);
      };

      ret[nameof(IProxyBroker.SelcalAlertReceived)] = (args) =>
      {
        var selcalAlertReceivedArgs = BuildObject<SelcalAlertReceivedEventArgs>(args);
        this.SelcalAlertReceived?.Invoke(this, selcalAlertReceivedArgs);
      };

      ret[nameof(IProxyBroker.ControllerAdded)] = (args) =>
      {
        var controllerAddedArgs = BuildObject<ControllerAddedEventArgs>(args);
        this.ControllerAdded?.Invoke(this, controllerAddedArgs);
      };

      ret[nameof(IProxyBroker.ControllerDeleted)] = (args) =>
      {
        var controllerDeletedArgs = BuildObject<ControllerDeletedEventArgs>(args);
        this.ControllerDeleted?.Invoke(this, controllerDeletedArgs);
      };

      ret[nameof(IProxyBroker.ControllerFrequencyChanged)] = (args) =>
      {
        var controllerFrequencyChangedArgs = BuildObject<ControllerFrequencyChangedEventArgs>(args);
        this.ControllerFrequencyChanged?.Invoke(this, controllerFrequencyChangedArgs);
      };

      ret[nameof(IProxyBroker.ControllerLocationChanged)] = (args) =>
      {
        var controllerLocationChangedArgs = BuildObject<ControllerLocationChangedEventArgs>(args);
        this.ControllerLocationChanged?.Invoke(this, controllerLocationChangedArgs);
      };

      ret[nameof(IProxyBroker.SessionEnded)] = (args) =>
      {
        this.SessionEnded?.Invoke(this, EventArgs.Empty);
      };

      return ret;
    }

    private async Task DoEventListening(CancellationToken token)
    {
      while (!token.IsCancellationRequested)
      {
        try
        {
          string pipeName = this.pipeID + "ProxyEventPipe";
          logger.Log(LogLevel.DEBUG, "Opening Server-Pipe " + pipeName);
          using var pipe = new NamedPipeServerStream(pipeName);
          await pipe.WaitForConnectionAsync(token);

          logger.Log(LogLevel.DEBUG, "Somebody Connected to pipe " + pipeName);
          using var reader = new StreamReader(pipe, Encoding.UTF8);
          string json = await reader.ReadToEndAsync();

          var evt = JsonConvert.DeserializeObject<ProxyMessage>(json);
          EAssert.IsNotNull(evt);
          EAssert.IsTrue(eventHandlers.TryGetValue(evt.Method, out var handler), $"No handler for event {evt.Method}");
          EAssert.IsNotNull(handler);
          logger.Log(LogLevel.INFO, "Handling event " + evt.Method + " with args " + evt.Args.ToString());
          handler.Invoke(evt.Args);
        }
        catch (OperationCanceledException)
        {
          // Expected on shutdown
          break;
        }
        catch (Exception ex)
        {
          logger.Log(LogLevel.ERROR, $"Error in event listening task: {ex.Message}");
          await Task.Delay(1000, token); // prevent tight loop on error
        }
      }
    }

    private T BuildObject<T>(Dictionary<string, object> args) where T : new()
    {
      logger.LogMethodStart();
      T ret = new T();

      var properties = typeof(T).GetProperties();
      foreach (var property in properties)
      {
        var value = args[property.Name];
        if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
          var listType = typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]);
          value = ((JArray)value).ToObject(listType);
        }
        else if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(int))
        {
          value = ((JArray) value).ToObject<int[]>();
        }
        else if (value is Int64)
          value = Convert.ToInt32(value);
        try
        {
          property.SetValue(ret, value);
        }
        catch (Exception ex)
        {
          logger.LogException(ex);
          throw new ApplicationException($"Error setting property {typeof(T).Name}.{property.Name} as {value}", ex);
        }
      }

      logger.LogMethodEnd();
      return ret;
    }

    public void Dispose()
    {
      eventListeningTaskCancelationTokenSource.Cancel();
      try { eventListeningTask.Wait(); }
      catch
      { /* ignore */
      }
      eventListeningTaskCancelationTokenSource.Dispose();
    }

    public void RequestConnect(string callsign, string typeCode, string selcalCode)
    {
      Dictionary<string, object> args = BuildArgsDictionary(nameof(callsign), callsign, nameof(typeCode), typeCode, nameof(selcalCode), selcalCode);
      SendViaPipe(nameof(RequestConnect), args);
    }

    public void RequestMetar(string station)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(station), station);
      SendViaPipe(nameof(RequestMetar), args);
    }

    public void RequestAtis(string callsign)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(callsign), callsign);
      SendViaPipe(nameof(RequestAtis), args);
    }

    public void SendRadioMessage(string message)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(message), message);
      SendViaPipe(nameof(SendRadioMessage), args);
    }

    public void PostDebugMessage(string message)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(message), message);
      SendViaPipe(nameof(PostDebugMessage), args);
    }

    public void SetModeC(bool modeC)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(modeC), modeC);
      SendViaPipe(nameof(SetModeC), args);
    }

    public void SquawkIdent()
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = new Dictionary<string, object>();
      SendViaPipe(nameof(SquawkIdent), args);
    }

    public void SetPtt(bool pressed)
    {
      logger.LogMethodStart();
      Dictionary<string, object> args = BuildArgsDictionary(nameof(pressed), pressed);
      SendViaPipe(nameof(SetPtt), args);
    }
  }
}
