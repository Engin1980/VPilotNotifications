// See https://aka.ms/new-console-template for more information
using NAudio.Wave;

Console.WriteLine("NAudio, Hello, World!");

string file = @"..\..\..\..\..\Implementations\VPilotNetAlert\Sounds\disconnected.mp3";
string absFile = System.IO.Path.GetFullPath(file);

WaveStream mainOutputStream = new Mp3FileReader(file);
WaveChannel32 volumeStream = new(mainOutputStream);
WaveOutEvent player = new();
player.Init(volumeStream);
player.Play();

//PlayIII(absFile);

//await Task.Run(() => PlayIII(absFile));

Console.WriteLine("Sent");
Thread.Sleep(1000);
Console.WriteLine("Done");

static void PlayI(string file)
{
  WaveStream mainOutputStream = new Mp3FileReader(file);
  WaveChannel32 volumeStream = new(mainOutputStream);
  using (WaveOutEvent player = new())
  {
    player.Init(volumeStream);
    player.Play();
  }
}

static void PlayII(string file)
{
  WaveStream mainOutputStream = new Mp3FileReader(file);
  WaveChannel32 volumeStream = new(mainOutputStream);
  using WaveOutEvent player = new();
  player.Init(volumeStream);
  player.Play();
}

static void PlayIII(string file)
{
  WaveStream mainOutputStream = new Mp3FileReader(file);
  WaveChannel32 volumeStream = new(mainOutputStream);
  WaveOutEvent player = new();
  player.Init(volumeStream);
  player.Play();
}