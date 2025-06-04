using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications.Settings
{
  using System.ComponentModel.DataAnnotations;

  public class Config
  {
    [Required]
    public LoggingConfig Logging { get; set; } = null!;

    [Required]
    public TasksConfig Tasks { get; set; } = null!;

    [Required]
    public VatsimConfig Vatsim { get; set; } = null!;

    [Required]
    public GlobalConfig Global { get; set; } = null!;
  }

  public class LoggingConfig
  {
    [Required]
    public string FileName { get; set; } = null!;
  }

  public class TasksConfig
  {
    [Required]
    public ContactMeConfig ContactMe { get; set; } = null!;

    [Required]
    public NoFlightPlanConfig NoFlightPlan { get; set; } = null!;

    [Required]
    public ImportantRadioMessageConfig ImportantRadioMessage { get; set; } = null!;

    [Required]
    public DisconnectedConfig Disconnected { get; set; } = null!;
  }

  public class ContactMeConfig
  {
    public bool Enabled { get; set; }

    [Required]
    [RegularExpression(@"^Contact me.+(1\d{2}[\\.,]\d+)$")]
    public string FrequencyRegex { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int RepeatSoundInterval { get; set; }

    [Required]
    public AudioFileConfig AudioFile { get; set; } = null!;
  }

  public class NoFlightPlanConfig
  {
    public bool Enabled { get; set; }
    [Range(1, int.MaxValue)]
    public bool DetectionOnConnection { get; set; } = true;
    public bool DetectionOnParkingBrake { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int DetectionOnHeight { get; set; } = 1000;
    [Range(1, int.MaxValue)]
    public int DetectionOnHeightInterval { get; set; } = 10;
    [Required]
    public AudioFileConfig AudioFile { get; set; } = null!;
  }

  public class ImportantRadioMessageConfig
  {
    public bool Enabled { get; set; }

    [Required]
    public AudioFileConfig AudioFile { get; set; } = null!;
  }

  public class DisconnectedConfig
  {
    public bool Enabled { get; set; }

    [Range(1, int.MaxValue)]
    public int RepeatInterval { get; set; }

    [Required]
    public AudioFileConfig AudioFile { get; set; } = null!;
  }

  public class AudioFileConfig
  {
    [Required]
    public string Name { get; set; } = null!;

    [Range(0.0, 1.0)]
    public double Volume { get; set; }
  }

  public class VatsimConfig
  {
    [Required]
    [MinLength(1)]
    public List<string> Sources { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int NoFlightPlanUpdateInterval { get; set; }

    [Range(0, int.MaxValue)]
    public int RefreshFlightPlanUpdateInterval { get; set; }
  }

  public class GlobalConfig
  {
    public bool SendPrivateMessageWhenConnectedForTheFirstTime { get; set; }

    public bool SendPrivateMessageWhenFlightPlanDetected { get; set; }

    [Range(1, int.MaxValue)]
    public int ConnectTimeout { get; set; }
  }

}
