using RossCarlson.Vatsim.Vpilot.Plugins;

namespace VPilotMessageAlert
{
  public class VPilotPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin
  {
    public string Name => "VPilotMessageAlert";
    private BrokerProxy? brokerProxy;

    public void Initialize(IBroker broker)
    {
      this.brokerProxy = new(broker);

      this.brokerProxy.NetworkConnected += Broker_NetworkConnected;
      this.brokerProxy.NetworkDisconnected += Broker_NetworkDisconnected;
      this.brokerProxy.RadioMessageReceived += Broker_RadioMessageReceived;
      this.brokerProxy.SelcalAlertReceived += Broker_SelcalAlertReceived;
    }

    private void Broker_SelcalAlertReceived(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.SelcalAlertReceivedEventArgs e)
    {
      throw new NotImplementedException();
    }

    private void Broker_RadioMessageReceived(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.RadioMessageReceivedEventArgs e)
    {
      throw new NotImplementedException();
    }

    private void Broker_NetworkDisconnected(object? sender, EventArgs e)
    {
      throw new NotImplementedException();
    }

    private void Broker_NetworkConnected(object? sender, RossCarlson.Vatsim.Vpilot.Plugins.Events.NetworkConnectedEventArgs e)
    {
      throw new NotImplementedException();
    }
  }
}
