using RossCarlson.Vatsim.Vpilot.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotMessageAlert
{
  public class MockBroker
  {
    public event Action<object, NetworkConnectedEventArgs> NetworkConnected;
    public event Action<object, EventArgs> NetworkDisconnected;
    public event Action<object, RadioMessageReceivedEventArgs> RadioMessageReceived;
    public event Action<object, SelcalAlertReceivedEventArgs> SelcalAlertReceived;

    public void InvokeConnected(string cid)
    {
      this.NetworkConnected?.Invoke(this, new NetworkConnectedEventArgs(cid, "EZY5495", "A20N", "JP-MN", false));
    }

    public void InvokeDisconnected()
    {
      this.NetworkDisconnected?.Invoke(this, new EventArgs());
    }

    public void InvokeMessage(string message)
    {
      this.RadioMessageReceived?.Invoke(this, new RadioMessageReceivedEventArgs(new int[] { 22800 }, "LKPR_TWR", message));
    }

  }
}
