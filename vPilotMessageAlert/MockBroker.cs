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
    public event Action<object?, NetworkConnectedEventArgs>? NetworkConnected;
    public event Action<object?, EventArgs>? NetworkDisconnected;
    public event Action<object?, RadioMessageReceivedEventArgs>? RadioMessageReceived;
    public event Action<object?, SelcalAlertReceivedEventArgs>? SelcalAlertReceived;
  }
}
