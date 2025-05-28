// See https://aka.ms/new-console-template for more information
using System.Linq.Expressions;
using System.Reflection;
using VPilotNetCoreModule;


string pipePrefix = args.Length < 1 ? "PipeNameNotProvided???" : args[0];
Console.WriteLine("Starting proxy broker .NET 6 with prefix " + pipePrefix);

ClientProxyBroker broker = new(pipePrefix);
Console.WriteLine("Starting proxy broker .NET 6 - completed.");

Console.WriteLine("Attaching event handlers.");
AttachEventDebugHandlers(broker);
Console.WriteLine("Attaching event handlers - completed.");

Thread.Sleep(3000);
broker.RequestDisconnect();

//Thread.Sleep(3000);
//broker.SendPrivateMessage("EZY1234", "Hello from .NET 6!");

Thread.Sleep(50000);
//broker.RequestDisconnect();


static void AttachEventDebugHandlers<T>(T obj)
{
  if (obj == null) throw new ArgumentNullException(nameof(obj));

  var type = typeof(T);
  var events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance);

  foreach (var evt in events)
  {
    var handlerType = evt.EventHandlerType;
    if (handlerType == null) continue;

    var invokeMethod = handlerType.GetMethod("Invoke");
    if (invokeMethod == null) continue;

    var parameters = invokeMethod.GetParameters();

    // Build lambda parameters
    var paramExprs = parameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

    // Build expression body
    var body = new List<Expression>();

    body.Add(Expression.Call(
        typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!,
        Expression.Constant($"Event '{evt.Name}' invoked with arguments:")
    ));

    foreach (var p in paramExprs)
    {
      Expression toStr = Expression.Call(
          Expression.Convert(p, typeof(object)),
          typeof(object).GetMethod("ToString")!
      );

      body.Add(Expression.Call(
          typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!,
          toStr
      ));
    }

    var lambda = Expression.Lambda(handlerType, Expression.Block(body), paramExprs);
    var del = lambda.Compile();

    evt.AddEventHandler(obj, del);
  }
}