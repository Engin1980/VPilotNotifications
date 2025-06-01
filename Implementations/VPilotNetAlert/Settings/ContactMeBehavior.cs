using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetAlert.Settings
{
  public class ContactMeBehavior
  {
    public bool Enabled { get; set; } = false;
    public string FrequencyRegex { get; set; } = "^Contact me.+(1\\d{2}[\\\\.,]\\d+)$";
    public int RepeatSoundInterval { get; set; } = 30; // seconds
  }
}
