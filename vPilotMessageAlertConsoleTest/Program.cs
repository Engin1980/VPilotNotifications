// See https://aka.ms/new-console-template for more information
using VPilotMessageAlert;

Console.WriteLine("Hello, World!");

MockBroker broker = new();

VPilotPlugin plugin = new VPilotPlugin();
plugin.Initialize(broker);

Console.WriteLine("Broker initialized");
Thread.Sleep(500);
Console.WriteLine("Invoking connected");
broker.InvokeConnected("1666537");
Thread.Sleep(1000);
Console.WriteLine("Invoking message");
broker.InvokeMessage("EZY5495, hello");
Thread.Sleep(1000);
Console.WriteLine("Invoking disconnected");
broker.InvokeDisconnected();
Thread.Sleep(1000);
Console.WriteLine("Done");
