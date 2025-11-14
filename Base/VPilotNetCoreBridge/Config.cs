using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNetCoreBridge
{
  public class Config
  {
    public string ClientExe { get; set; }
    public bool ShowClientConsole { get; set; }
    public int ConnectTimeout { get; set; } = 5000; // Default to 5 seconds
    public bool ProcessAircraftRelatedEvents { get; set; }
  }
}
