using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetAlert
{
  internal class ESimWrapper
  {
    public ESimConnect.ESimConnect ESimConnect { get; init; }
    public ESimConnect.Extenders.OpenInBackgroundExtender Open { get; init; }
    public ESimConnect.Extenders.SimTimeExtender SimTime { get; init; }
    public ESimConnect.Extenders.ValueCacheExtender ValueCache { get; init; }

    public ESimWrapper()
    {
      this.ESimConnect = new ESimConnect.ESimConnect();
      this.Open = new ESimConnect.Extenders.OpenInBackgroundExtender(this.ESimConnect);
      this.SimTime = new ESimConnect.Extenders.SimTimeExtender(this.ESimConnect, false);
      this.ValueCache = new ESimConnect.Extenders.ValueCacheExtender(this.ESimConnect);
    }
  }
}
