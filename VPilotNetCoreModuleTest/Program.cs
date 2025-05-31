// See https://aka.ms/new-console-template for more information
using ESystem.Logging;
using System.Linq.Expressions;
using System.Reflection;
using VPilotNetCoreModule;


string pipePrefix = args.Length < 1 ? "PipeNameNotProvided???" : args[0];
Console.WriteLine($"VPilotNetCoreModule - Test -- startup");
Console.WriteLine($"Pipe prefix: {pipePrefix}");
Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
Console.WriteLine("Initializing log");
SetUpLogging();

var logger = Logger.Create("VPilotNetCoreModuleTest.Program");

logger.Log(LogLevel.INFO, "Starting proxy broker .NET 6 with prefix " + pipePrefix);

ClientProxyBroker broker = new(pipePrefix);
logger.Log(LogLevel.INFO, "Starting proxy broker .NET 6 - completed.");

logger.Log(LogLevel.INFO, "Attaching event handlers.");
AttachEventDebugHandlers(broker);
logger.Log(LogLevel.INFO, "Attaching event handlers - completed.");


Thread.Sleep(3000);
logger.Log(LogLevel.INFO, "Requesting testing disconnect method to vPilot.");
broker.RequestDisconnect();

//Thread.Sleep(3000);
//broker.SendPrivateMessage("EZY1234", "Hello from .NET 6!");

Thread.Sleep(50000);
//broker.RequestDisconnect();


static void SetUpLogging()
{
  static void action(LogItem logItem)
  {
    string logFileName = Path.Combine(
      Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) ?? "",
      "vPilotNetCore.log");
    string msg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logItem.Level}] {logItem.Message}\n";
    try
    {
      File.AppendAllLines("vPilotNetCore.log", new[] { msg });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to write log: {ex.Message}");
    }
  }
  List<LogRule> logRules = new()
  {
        new(".*", LogLevel.DEBUG)
      };
  Logger.RegisterLogAction(action, logRules);
}

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