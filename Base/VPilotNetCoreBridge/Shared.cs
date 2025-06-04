using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNetCoreBridge
{
  public class ProxyMessage
  {
    public const string METHOD_CALL = "MethodCall";
    public const string EVENT = "Event";
    public string Type { get; set; } = METHOD_CALL;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();

    public ProxyMessage(string type, string method, Dictionary<string, object> args)
    {
      Type = type;
      Method = method;
      Args = args;
    }

    public ProxyMessage(string type, string method)
    {
      Type = type;
      Method = method;
    }

    public ProxyMessage() { }
  }

  public class AircraftAddedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
    public string TypeCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double PressureAltitude { get; set; }
    public double Pitch { get; set; }
    public double Bank { get; set; }
    public double Heading { get; set; }
    public double Speed { get; set; }
  }

  public class NetworkConnectedEventArgs : EventArgs
  {
    public string Cid { get; set; }
    public string Callsign { get; set; }
    public string TypeCode { get; set; }
    public string SelcalCode { get; set; }
    public bool ObserverMode { get; set; }
  }
  public class PrivateMessageReceivedEventArgs : EventArgs
  {
    public string From { get; set; }
    public string Message { get; set; }
  }
  public class RadioMessageReceivedEventArgs : EventArgs
  {
    public int[] Frequencies { get; set; }
    public string From { get; set; }
    public string Message { get; set; }
  }
  public class BroadcastMessageReceivedEventArgs : EventArgs
  {
    public string From { get; set; }
    public string Message { get; set; }
  }
  public class MetarReceivedEventArgs : EventArgs
  {
    public string Metar { get; set; }
  }
  public class AtisReceivedEventArgs : EventArgs
  {
    public string From { get; set; }
    public List<string> Lines { get; set; }
  }
  public class ControllerAddedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
    public int Frequency { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
  }
  public class ControllerDeletedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
  }
  public class ControllerFrequencyChangedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
    public int NewFrequency { get; set; }
  }
  public class ControllerLocationChangedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
    public double NewLatitude { get; set; }
    public double NewLongitude { get; set; }
  }
  public class SelcalAlertReceivedEventArgs : EventArgs
  {
    public int[] Frequencies { get; set; }
    public string From { get; set; }
  }
  public class AircraftUpdatedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double PressureAltitude { get; set; }
    public double Pitch { get; set; }
    public double Bank { get; set; }
    public double Heading { get; set; }
    public double Speed { get; set; }
  }
  public class AircraftDeletedEventArgs : EventArgs
  {
    public string Callsign { get; set; }
  }


}
