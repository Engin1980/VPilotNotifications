using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreBridge
{
  public interface IBroker
  {
    event EventHandler SessionEnded;
    event EventHandler<NetworkConnectedEventArgs> NetworkConnected;
    event EventHandler NetworkDisconnected;
    event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;
    event EventHandler<RadioMessageReceivedEventArgs> RadioMessageReceived;
    event EventHandler<BroadcastMessageReceivedEventArgs> BroadcastMessageReceived;
    event EventHandler<MetarReceivedEventArgs> MetarReceived;
    event EventHandler<AtisReceivedEventArgs> AtisReceived;
    event EventHandler<ControllerAddedEventArgs> ControllerAdded;
    event EventHandler<ControllerDeletedEventArgs> ControllerDeleted;
    event EventHandler<ControllerFrequencyChangedEventArgs> ControllerFrequencyChanged;
    event EventHandler<ControllerLocationChangedEventArgs> ControllerLocationChanged;
    event EventHandler<SelcalAlertReceivedEventArgs> SelcalAlertReceived;
    event EventHandler<AircraftAddedEventArgs> AircraftAdded;
    event EventHandler<AircraftUpdatedEventArgs> AircraftUpdated;
    event EventHandler<AircraftDeletedEventArgs> AircraftDeleted;

    void RequestConnect(string callsign, string typeCode, string selcalCode);
    void RequestDisconnect();
    void RequestMetar(string station);
    void RequestAtis(string callsign);
    void SendPrivateMessage(string to, string message);
    void SendRadioMessage(string message);
    void PostDebugMessage(string message);
    void SetModeC(bool modeC);
    void SquawkIdent();
    void SetPtt(bool pressed);
  }
}
