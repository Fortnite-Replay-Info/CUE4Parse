using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FileParsing.Attributes;
using FileParsing.Classes.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FileParsing.Classes.Loaders.Abstract;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using Newtonsoft.Json;
using System.Text;

namespace FileParsing.Classes.Loaders
{
  class HeightMapLoader : AbstractLoader
  {
    private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
    public HeightMapLoader(DefaultFileProvider provider) : base(provider)
    {
    }

    public void GetHeightmaps()
    {
      var files = FindAssets("maps/landscape/artemis_terrain", null, "umap");

      var ok = new Dictionary<string, UTexture2D>();

      for (int i = 0; i < files.Count; i++)
      {
        var path = files.ElementAt(i);

        Console.WriteLine("Loading package: " + i + "/" + files.Count + " " + path);

        if (!Provider.TryLoadPackage(path, out var package))
        {
          Console.WriteLine("Unable to load package:" + path);

          return;
        }

        Console.WriteLine("Loading exports: " + path);

        var oks = package.GetExports();

        Console.WriteLine("Found " + oks.Count() + " exports in " + path);

        foreach (var export in oks)
        {
          switch (export)
          {
            case UTexture2D texture:
              {
                ok.Add("result/heightmaps/" + package.Name + "/" + texture.Name + ".png", texture);

                break;
              }
          }
        }

        Console.WriteLine("Done with " + path);
      }

      Console.WriteLine("Found " + ok.Count + " heightmaps");

      foreach (var texture in ok)
      {
        var path = texture.Key;
        var tex = texture.Value;

        var okk = tex.Decode();

        var skiaImage = okk.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

        EnsurePath(path.Substring(0, path.LastIndexOf('/')));

        File.WriteAllBytes(path, skiaImage.ToArray());
      }

      Console.WriteLine("Done with heightmaps");
    }

    public List<AbstractMapObject> GetResult()
    {
      return MapObjects;
    }
  }
}
