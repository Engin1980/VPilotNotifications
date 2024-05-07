using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPilotMessageAlert;

namespace VPilotMessageAlertConsoleTestFW
{
  internal class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Hello, World!");

      MockBroker broker = new MockBroker();

      VPilotPlugin plugin = new VPilotPlugin();
      plugin.Initialize(broker);

      Console.WriteLine("Broker initialized");
      Thread.Sleep(500);
      Console.WriteLine("Invoking connected");
      broker.InvokeConnected("964586");
      Thread.Sleep(1000);
      Console.WriteLine("Invoking message");
      broker.InvokeMessage("EZY5495, hello");
      Thread.Sleep(1000);
      Console.WriteLine("Invoking disconnected");
      broker.InvokeDisconnected();
      Thread.Sleep(5000);
      Console.WriteLine("Done");

    }
  }
}
