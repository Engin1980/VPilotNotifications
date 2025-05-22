using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPilotNetCoreBridge;

namespace VPilotNetCoreBridgeTest
{
  internal class Program
  {
    static void Main(string[] args)
    {
      MockBroker broker = new MockBroker();

      VPilotPlugin plugin = new VPilotPlugin();
      plugin.Initialize(broker);

      //System.Threading.Thread.Sleep(1000);
      //broker.InvokeSessionEnded();

      //System.Threading.Thread.Sleep(1000);
      //broker.InvokeAircraftAdded();

      //System.Threading.Thread.Sleep(1000);
      //broker.InvokeAtisReceived(new AtisReceivedEventArgs()
      //{
      //  From = "TestAtis",
      //  Lines = new string[] { "TestAtisLine1", "TestAtisLine2", "TestAtisLine3" }.ToList(),
      //});

      //System.Threading.Thread.Sleep(1000);
      //broker.InvokeBroadcastMessageReceived(new BroadcastMessageReceivedEventArgs()
      //{
      //  From = "TestBroadcast",
      //  Message = "TestBroadcastMessage",
      //});

      //System.Threading.Thread.Sleep(1000);
      //broker.InvokeControllerAdded(new ControllerAddedEventArgs()
      //{
      //  Callsign = "TestController",
      //  Frequency = 123450,
      //  Latitude = 50.05,
      //  Longitude = 17.71
      //});

      System.Threading.Thread.Sleep(50000);
    }
  }
}
