﻿using System;
using System.IO;
using System.Diagnostics;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using FileParsing.Classes.Loaders;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FileParsing
{
  public static class Program
  {
    private const string _gameDirectory = "/home/xnocken/Games/Fortnite/FortniteGame/Content/Paks";
    private const string _aesKey = "0x53839BA2A77AE393588184ACBD18EDBC935CA60D554F9D29BC3F135E426C4A6F";

    public static void Main(string[] args)
    {
      var provider = new DefaultFileProvider(_gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_UE5_LATEST, ETexturePlatform.DesktopMobile, new FPackageFileVersion(522, 1006), null, null));
      provider.Initialize(); // will scan local files and read them to know what it has to deal with (PAK/UTOC/UCAS/UASSET/UMAP)
      // provider.SubmitKey(new FGuid(), new FAesKey(_aesKey)); // decrypt basic info (1 guid - 1 key)

      provider.LoadAesKeys();
      provider.LoadMappings(); // needed to read Fortnite assets
      provider.LoadLocalization(ELanguage.English); // explicit enough

      var mapLoader = new HeightMapLoader(provider);

      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();

      // var ok = mapLoader.LoadObject("fortnitegame/plugins/gamefeatures/specialeventgameplay/config/specialeventgameplaygame.ini");

      // mapLoader.LoadMapRecursive("fortnitegame/content/athena/artemis/maps/artemis_terrain", new FVector(0), new FRotator(0f));
      mapLoader.GetHeightmaps();

      // mapLoader.LoadFiles();

      var mapExports = mapLoader.GetResult();

      stopWatch.Stop();

      TimeSpan ts = stopWatch.Elapsed;

      // Format and display the TimeSpan value.
      string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
          ts.Hours, ts.Minutes, ts.Seconds,
          ts.Milliseconds / 10);
      Console.WriteLine("RunTime " + elapsedTime);

      var mapExportsJson = JsonConvert.SerializeObject(mapExports, Formatting.Indented);

      File.WriteAllText("result/result-structures.json", mapExportsJson);
    }
  }
}
