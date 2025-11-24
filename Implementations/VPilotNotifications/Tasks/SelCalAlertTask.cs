using Eng.VPilotNetCoreModule;
using Eng.VPilotNotifications.Settings;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Tasks
{
  internal class SelCalAlertTask : AbstractTask
  {
    private readonly SelcalAlertConfig config;

    public SelCalAlertTask(TaskInitData data, SelcalAlertConfig config) : base(data)
    {
      this.config = config;
      data.Broker.SelcalAlertReceived += Broker_SelcalAlertReceived;
      Logger.Log(LogLevel.INFO, "Initialized.");
    }

    private void Broker_SelcalAlertReceived(object? sender, SelcalAlertReceivedEventArgs e)
    {
      Logger.Log(LogLevel.INFO, "SELCAL received");
      Logger.Log(LogLevel.INFO, $"SELCAL alert received: {e.From} on {e.Frequencies}");
      
      Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
    }
  }
}
