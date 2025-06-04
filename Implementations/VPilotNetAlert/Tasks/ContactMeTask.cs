using ESimConnect;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using VPilotNetAlert.Settings;
using VPilotNetCoreModule;

namespace VPilotNetAlert.Tasks
{
  internal class ContactMeTask : AbstractTask
  {
    private class ContactMeData
    {
      public double Frequency { get; }
      public DateTime CreatedAt { get; }

      public ContactMeData(double frequency)
      {
        Frequency = frequency;
        CreatedAt = DateTime.Now;
      }
    }

    private ContactMeData? data = null;
    private readonly System.Timers.Timer checkTimer;
    private readonly ContactMeConfig config;
    private readonly TypeId[] comFrequencyTypeId;
    private readonly TypeId[] comReceivingTypeId;
    private readonly CultureInfo enUS = System.Globalization.CultureInfo.GetCultureInfo("en-US");

    public ContactMeTask(TaskInitData data, ContactMeConfig config) : base(data)
    {
      Logger.Log(LogLevel.INFO, "ContactMeTask initalizing.");
      this.config = config ?? throw new ArgumentNullException(nameof(config), "ContactMeConfig cannot be null.");

      Logger.Log(LogLevel.DEBUG, "Registering event handlers for network and radio messages.");
      data.Broker.PrivateMessageReceived += Broker_PrivateMessageReceived;
      Logger.Log(LogLevel.DEBUG, "Event handlers registered.");

      this.checkTimer = new System.Timers.Timer(config.RepeatSoundInterval * 1000)
      {
        AutoReset = true,
        Enabled = false,
      };
      this.checkTimer.Elapsed += CheckTimer_Elapsed;

      Logger.Log(LogLevel.DEBUG, "Registering COM frequency and receiving TypeIds.");
      comFrequencyTypeId = new TypeId[3];
      comReceivingTypeId = new TypeId[3];
      for (int i = 0; i < 3; i++)
      {
        comFrequencyTypeId[i] = data.ESimWrapper.ValueCache.Register($"COM ACTIVE FREQUENCY:{i + 1}");
        comReceivingTypeId[i] = data.ESimWrapper.ValueCache.Register($"COM RECEIVE:{i + 1}");
      }
      Logger.Log(LogLevel.DEBUG, "COM frequency and receiving TypeIds registered.");

      Logger.Log(LogLevel.INFO, "ContactMeTask initialized.");
    }

    private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      if (base.IsConnected == false)
      {
        checkTimer.Stop();
        this.data = null;
        return;
      }

      Logger.Log(LogLevel.DEBUG, "CheckTimer elapsed. Checking radio tuning and ContactMe data.");
      CheckRadioTuning();
      if (this.data == null)
      {
        this.checkTimer.Stop();
        Logger.Log(LogLevel.DEBUG, "No ContactMe data available. Timer elapsed without action.");
        return;
      }
      else
      {
        Logger.Log(LogLevel.DEBUG, $"ContactMe data available. Frequency: {this.data.Frequency}, CreatedAt: {this.data.CreatedAt}");
        base.SendSystemPrivateMessage($"Contact me on frequency {this.data.Frequency} MHz. Original request created at {this.data.CreatedAt:HH:mm:ss}.");
        Audio.PlayAudioFile(this.config.AudioFile.Name, this.config.AudioFile.Volume);
      }
    }

    private void CheckRadioTuning()
    {
      if (this.data == null) return;

      for (int i = 0; i < 3; i++)
      {
        double freq = this.ESimWrapper.ValueCache.GetValue(comFrequencyTypeId[i]);
        freq = freq / 1000000; // convert from BCD format to MHz (e.g., 127850000 to 127.85 MHz)
        double receiving = this.ESimWrapper.ValueCache.GetValue(comReceivingTypeId[i]);
        Logger.Log(LogLevel.TRACE, $"Checking COM{i + 1}: Frequency = {freq} MHz, Receiving = {receiving}");
        if (freq == this.data.Frequency && receiving > 0.5)
        {
          Logger.Log(LogLevel.DEBUG, $"Radio tuned to frequency {freq} on COM{i + 1}. ContactMe data is valid.");
          this.data = null;
          return; // Radio is tuned to the correct frequency
        }
      }
    }

    private void Broker_PrivateMessageReceived(object? sender, PrivateMessageReceivedEventArgs e)
    {
      Logger.Log(LogLevel.DEBUG, $"Radio message received: {e.From} :: {e.Message}");
      if (!IsContactMeMessage(e, out double frequency))
      {
        Logger.Log(LogLevel.DEBUG, "Not a ContactMe message. Ignoring.");
        return;
      }

      Logger.Log(LogLevel.INFO, $"Detected 'Contact me' message from {e.From} with frequency {frequency} MHz.");
      this.data = new ContactMeData(frequency);
      this.checkTimer.Start();
    }

    private bool IsContactMeMessage(PrivateMessageReceivedEventArgs e, out double frequency)
    {
      string msg = e.Message;

      Regex regex = new(config.FrequencyRegex);
      MatchCollection matches = regex.Matches(msg);
      if (matches.Count > 0)
      {
        string frequencyGroup = matches[0].Groups[1].Value;
        frequencyGroup = frequencyGroup.Replace(",", ".");
        if (double.TryParse(frequencyGroup, NumberStyles.Float, enUS, out frequency))
          return true;
        else
        {
          this.Logger.Log(LogLevel.ERROR, $"Detected XX 'Contact me' message, but unable to parse frequency from '{frequencyGroup}' in message '{msg}'.");
        }
      }
      frequency = double.NaN;
      return false;
    }
  }
}
