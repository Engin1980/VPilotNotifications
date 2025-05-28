using RossCarlson.Vatsim.Vpilot.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugintTestToDelete
{
  public class VPlugin : RossCarlson.Vatsim.Vpilot.Plugins.IPlugin
  {
    public string Name => "My Test Plugin";

    public void Initialize(IBroker broker)
    {
      // empty
    }
  }
}
