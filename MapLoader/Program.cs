using System;
using System.IO;
using System.Diagnostics;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace MapParsing
{
  public static class Program
  {
    private const string _gameDirectory = "/home/xnocken/Games/Fortnite/FortniteGame/Content/Paks";
    private const string _aesKey = "0x53839BA2A77AE393588184ACBD18EDBC935CA60D554F9D29BC3F135E426C4A6F";

    public static void Main(string[] args)
    {
      var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_LATEST));
      provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
      // provider.SubmitKey(new FGuid(), new FAesKey(_aesKey)); // decrypt basic info (1 guid - 1 key)

      provider.LoadAesKeys();
      provider.LoadMappings(); // needed to read Fortnite assets
      provider.LoadLocalization(ELanguage.English); // explicit enough

      var mapLoader = new MapLoader(provider);

      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();


      mapLoader.PrepareMapOverlays();
      var exports = mapLoader.LoadMapRecursive("FortniteGame/Content/Athena/Artemis/maps/artemis_terrain", new FVector(0), new FRotator(0f));

      stopWatch.Stop();

      TimeSpan ts = stopWatch.Elapsed;

      // Format and display the TimeSpan value.
      string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
          ts.Hours, ts.Minutes, ts.Seconds,
          ts.Milliseconds / 10);
      Console.WriteLine("RunTime " + elapsedTime);

      var json = JsonConvert.SerializeObject(exports, Formatting.Indented);

      File.WriteAllText("result.json", json);
    }
  }
}
