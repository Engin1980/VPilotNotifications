using ESimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPilotNetAlert.Vatsim;
using VPilotNetCoreModule;

namespace VPilotNetAlert.Tasks
{
  internal record TaskInitData(ClientProxyBroker Broker, VatsimFlightPlanProvider VatsimFlightPlanProvider, ESimWrapper ESimWrapper);
}
