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
    private const string _aesKey = "0x6DAA4CCE14CB94598DA8B0C07F1386867DC73FA644B92900ADCEF89F26D159DC";

    private const string _objectPath = "FortniteGame/Content/Athena/Artemis/maps/artemis_poi_foundations.umap";

    public static void Main(string[] args)
    {
      var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_LATEST));
      provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
      provider.SubmitKey(new FGuid(), new FAesKey(_aesKey)); // decrypt basic info (1 guid - 1 key)

      provider.LoadMappings(); // needed to read Fortnite assets
      provider.LoadLocalization(ELanguage.English); // explicit enough

      var mapLoader = new MapLoader(new string[] {
        "Tiered_Chest_Athena_C", // chests
        "Tiered_Ammo_Athena_C", // ammo box
        "Tiered_Chest_Athena_FactionChest_IO_NoLocks_C", // henchman chests
        "Tiered_Chest_Apollo_IceBox_C", // ice
        "BGA_Athena_SCMachine_Redux_C", // reboot van
        "Tiered_Athena_FloorLoot_01_C", // floor loot spawns
      }, provider);

      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();

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
