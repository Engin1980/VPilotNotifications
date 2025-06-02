using ESystem.Asserting;
using ESystem.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPilotNetAlert.Vatsim
{
  public class VatsimModelDownloader
  {
    private static Logger logger = Logger.Create(nameof(VatsimModelDownloader));

    public static Logger Logger { get => logger; set => logger = value; }

    public static async Task<Model?> DownloadAsync(List<string> urls)
    {
      string data = await DownloadDataAsync(urls);
      Model? model = TryParseModel(data);
      return model;
    }

    public static Model? Download(List<string> urls)
    {
      Model? ret = DownloadAsync(urls).GetAwaiter().GetResult();
      return ret;
    }

    private static async Task<string> DownloadDataAsync(List<string> urls)
    {
      EAssert.Argument.IsNotNull(urls, nameof(urls));
      EAssert.Argument.IsTrue(urls.Count > 0, nameof(urls), "URLs array cannot be empty.");

      string ret;
      int index = new Random().Next(0, urls.Count);
      var url = urls[index];
      Logger.Log(LogLevel.DEBUG, $"VATSIM source selected: {url}");

      var uri = new Uri(url);
      Logger.Log(LogLevel.TRACE, "Opening web-client");
      using (HttpClient webClient = new())
      {
        Logger.Log(LogLevel.TRACE, "Downloading data... awaiting...");
        ret = await webClient.GetStringAsync(uri);
        Logger.Log(LogLevel.TRACE, $"Downloading data... completed, got {ret.Length} chars.");
      }
      return ret;
    }

    private static Model? TryParseModel(string data)
    {
      Logger.Log(LogLevel.TRACE, "Deserializing data");
      Model? model = JsonConvert.DeserializeObject<Model>(data);
      Logger.Log(LogLevel.TRACE, "Data deserialized.");
      if (model == null)
      {
        Logger.Log(LogLevel.ERROR, "Failed to extract vatsim data model from data string of length " + data.Length + ". Skipping.");
        return null;
      }

      return model;
    }
  }
}
