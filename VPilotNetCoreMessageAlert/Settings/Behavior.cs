using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetCoreMessageAlert.Settings
{
  public class Behavior
  {
    public bool SendPrivateMessageWhenConnectedForTheFirstTime { get; set; } = true;
    public bool SendPrivateMessageWhenFlightPlanDetected { get; set; } = true;
    public int RepeatAlertIntervalWhenDisconnected { get; set; } = -1;
    public ContactMeBehavior ContactMeBehavior { get; set; } = new ContactMeBehavior();
  }
}
