using ESystem.Asserting;
using ESystem.Logging;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.VPilotNotifications
{
  internal static class Audio
  {
    private static readonly Logger logger = Logger.Create("VPilotNotifications.Audio");

    internal static async Task PlayAudioFileAsync(string file, double volume)
    {
      await Task.Run(() => PlayAudioFile(file, volume));
    }

    internal static void PlayAudioFile(string file, double volume)
    {
      string absFile = System.IO.Path.GetFullPath(file);
      if (System.IO.File.Exists(absFile) == false)
      {
        logger.Log(LogLevel.ERROR, $"Audio file {file} (abs: {absFile} does not exist. Skipped.");
        return;
      }
      if (volume < 0.0 || volume > 1.0)
      {
        logger.Log(LogLevel.WARNING, $"Volume {volume} is out of range 0-1. Will be trimmed.");
        volume = Math.Clamp(volume, 0.0, 1.0);
      }

      WaveStream mainOutputStream;

      if (file.ToLower().EndsWith(".mp3"))
        mainOutputStream = new Mp3FileReader(absFile);
      else if (file.ToLower().EndsWith(".wav"))
        mainOutputStream = new WaveFileReader(absFile);
      else
      {
        logger.Log(LogLevel.ERROR, $"Unable to play {file}. Only MP3/WAV is supported. Skipped.");
        return;
      }

      WaveChannel32 volumeStream = new(mainOutputStream)
      {
        Volume = (float)volume
      };
      WaveOutEvent player = new();
      player.Init(volumeStream);
      player.Play();
    }
  }
}
