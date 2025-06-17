using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Vatsim;
using ESimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Tasks
{

  internal record TaskInitData(
    ClientProxyBroker Broker,
    VatsimFlightPlanProvider VatsimFlightPlanProvider,
    ESimWrapper ESimWrapper);
}
