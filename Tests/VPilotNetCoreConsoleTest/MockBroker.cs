using Eng.VPilotNetCoreBridge;
using Eng.VPilotNetCoreBridge.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.Tests.VPilotNetCoreBridgeTest
{
  internal class MockBroker : IBroker
  {
    public event EventHandler SessionEnded;
    public event EventHandler<AircraftAddedEventArgs> AircraftAdded;
    public event EventHandler<NetworkConnectedEventArgs> NetworkConnected;
    public event EventHandler NetworkDisconnected;
    public event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;
    public event EventHandler<RadioMessageReceivedEventArgs> RadioMessageReceived;
    public event EventHandler<BroadcastMessageReceivedEventArgs> BroadcastMessageReceived;
    public event EventHandler<MetarReceivedEventArgs> MetarReceived;
    public event EventHandler<AtisReceivedEventArgs> AtisReceived;
    public event EventHandler<ControllerAddedEventArgs> ControllerAdded;
    public event EventHandler<ControllerDeletedEventArgs> ControllerDeleted;
    public event EventHandler<ControllerFrequencyChangedEventArgs> ControllerFrequencyChanged;
    public event EventHandler<ControllerLocationChangedEventArgs> ControllerLocationChanged;
    public event EventHandler<SelcalAlertReceivedEventArgs> SelcalAlertReceived;
    public event EventHandler<AircraftUpdatedEventArgs> AircraftUpdated;
    public event EventHandler<AircraftDeletedEventArgs> AircraftDeleted;

    internal void OnSessionEnded()
    {
      Console.WriteLine("MockBroker - Invoking SessionEnded event");
      this.SessionEnded?.Invoke(this, new EventArgs());
    }
    internal void InvokeNetworkConnected(NetworkConnectedEventArgs args)
    {
      Console.WriteLine("MockBroker - Invoking NetworkConnected event");
      this.NetworkConnected?.Invoke(this, args);
    }

    internal void InvokeNetworkDisconnected()
    {
      Console.WriteLine("MockBroker - Invoking NetworkDisconnected event");
      this.NetworkDisconnected?.Invoke(this, new EventArgs());
    }

    internal void InvokePrivateMessageReceived(PrivateMessageReceivedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokePrivateMessageReceived");
      this.PrivateMessageReceived?.Invoke(this, args);
    }

    internal void InvokeRadioMessageReceived(RadioMessageReceivedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeRadioMessageReceived");
      this.RadioMessageReceived?.Invoke(this, args);
    }

    internal void InvokeBroadcastMessageReceived(BroadcastMessageReceivedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeBroadcastMessageReceived");
      this.BroadcastMessageReceived?.Invoke(this, args);
    }

    internal void InvokeMetarReceived(MetarReceivedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeMetarReceived");
      this.MetarReceived?.Invoke(this, args);
    }

    internal void InvokeAtisReceived(AtisReceivedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeAtisReceived");
      this.AtisReceived?.Invoke(this, args);
    }

    internal void InvokeControllerAdded(ControllerAddedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeControllerAdded");
      this.ControllerAdded?.Invoke(this, args);
    }

    internal void InvokeControllerDeleted(ControllerDeletedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeControllerDeleted");
      this.ControllerDeleted?.Invoke(this, args);
    }

    internal void InvokeControllerFrequencyChanged(ControllerFrequencyChangedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeControllerFrequencyChanged");
      this.ControllerFrequencyChanged?.Invoke(this, args);
    }

    internal void InvokeControllerLocationChanged(ControllerLocationChangedEventArgs args)
    {
      Console.WriteLine("MockBroker - InvokeControllerLocationChanged");
      this.ControllerLocationChanged?.Invoke(this, args);
    }

    public void RequestDisconnect()
    {
      Console.WriteLine("MockBroker - RequestDisconnect called");
    }

    public void SendPrivateMessage(string to, string message)
    {
      Console.WriteLine("MockBroker - sendPrivateMessage called to " + to + " with text: " + message);
    }

    internal void InvokeSessionEnded()
    {
      Console.WriteLine("MockBroker - Invoking SessionEnded event");
      this.SessionEnded?.Invoke(this, new EventArgs());
    }

    internal void InvokeAircraftAdded()
    {
      Console.WriteLine("MockBroker - Invoking AircraftAdded event");
      this.AircraftAdded?.Invoke(this, new AircraftAddedEventArgs()
      {
        Altitude = 38000,
        Bank = 0,
        Callsign = "N12345",
        Heading = 180,
        Latitude = 37.7749,
        Longitude = -122.4194,
        Pitch = 0,
        PressureAltitude = 38000,
        Speed = 450,
        TypeCode = "B737"
      });
    }

    public void RequestConnect(string callsign, string typeCode, string selcalCode)
    {
      Console.WriteLine($"ReqestConnect({callsign}, {typeCode}, {selcalCode})");
    }

    public void RequestMetar(string station)
    {
      Console.WriteLine($"RequestMetar({station})");
    }

    public void RequestAtis(string callsign)
    {
      Console.WriteLine($"RequestAtis({callsign})");
    }

    public void SendRadioMessage(string message)
    {
      Console.WriteLine($"SendRadMsg({message})");
    }

    public void PostDebugMessage(string message)
    {
      Console.WriteLine($"PostDebugMessage({message})");
    }

    public void SetModeC(bool modeC)
    {
      Console.WriteLine($"SetModeC({modeC})");
    }

    public void SquawkIdent()
    {
      Console.WriteLine($"SquawkIdent()");
    }

    public void SetPtt(bool pressed)
    {
      Console.WriteLine($"SetPtt({pressed})");
    }
  }
}
