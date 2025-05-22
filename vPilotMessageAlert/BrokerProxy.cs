using ESystem.Asserting;
using RossCarlson.Vatsim.Vpilot.Plugins;
using RossCarlson.Vatsim.Vpilot.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotMessageAlert
{
  internal class BrokerProxy
  {
    #region Public Events

    public event Action<object, NetworkConnectedEventArgs> NetworkConnected;

    public event Action<object, EventArgs> NetworkDisconnected;

    public event Action<object, RadioMessageReceivedEventArgs> RadioMessageReceived;

    public event Action<object, SelcalAlertReceivedEventArgs> SelcalAlertReceived;

    public event Action<object, PrivateMessageReceivedEventArgs> PrivateMessageReceived;

    #endregion Public Events

    #region Private Fields

    private readonly MockBroker mock;
    private readonly IBroker vpilot;
    private bool isConnected = false;

    #endregion Private Fields

    #region Public Constructors

    public BrokerProxy(IBroker broker)
    {
      EAssert.Argument.IsNotNull(broker, nameof(broker));
      this.vpilot = broker;
      this.mock = null;

      this.vpilot.NetworkConnected += Broker_NetworkConnected;
      this.vpilot.NetworkDisconnected += Broker_NetworkDisconnected;
      this.vpilot.RadioMessageReceived += Broker_RadioMessageReceived;
      this.vpilot.SelcalAlertReceived += Broker_SelcalAlertReceived;
      this.vpilot.PrivateMessageReceived += Broker_PrivateMessageReceived;
    }



    public BrokerProxy(MockBroker broker)
    {
      EAssert.Argument.IsNotNull(broker, nameof(broker));
      this.vpilot = null;
      this.mock = broker;

      this.mock.NetworkConnected += Broker_NetworkConnected;
      this.mock.NetworkDisconnected += Broker_NetworkDisconnected;
      this.mock.RadioMessageReceived += Broker_RadioMessageReceived;
      this.mock.SelcalAlertReceived += Broker_SelcalAlertReceived;
    }

    #endregion Public Constructors

    #region Public Methods

    public void SendPrivateMessage(string message)
    {
      if (this.mock != null)
        Console.WriteLine(message);
      else if (this.vpilot != null)
      {
        if (isConnected)
          this.vpilot.SendPrivateMessage("VPilotMsgAlert", message);
      }
    }
    #endregion

    #region Private Methods

    private void Broker_NetworkConnected(object sender, NetworkConnectedEventArgs e)
    {
      this.isConnected = true;
      this.NetworkConnected?.Invoke(this, e);
    }

    private void Broker_NetworkDisconnected(object sender, EventArgs e)
    {
      this.isConnected = false;
      this.NetworkDisconnected?.Invoke(this, e);
    }

    private void Broker_RadioMessageReceived(object sender, RadioMessageReceivedEventArgs e)
    {
      this.RadioMessageReceived?.Invoke(this, e);
    }

    private void Broker_SelcalAlertReceived(object sender, SelcalAlertReceivedEventArgs e)
    {
      this.SelcalAlertReceived?.Invoke(this, e);
    }

    private void Broker_PrivateMessageReceived(object sender, PrivateMessageReceivedEventArgs e)
    {
      this.PrivateMessageReceived?.Invoke(this, e);
    }

    #endregion Private Methods
  }
}
