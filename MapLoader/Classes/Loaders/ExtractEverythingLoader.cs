using CUE4Parse.FileProvider;
using FileParsing.Classes.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Objects.UObject;
using FileParsing.Classes.Loaders.Abstract;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using Newtonsoft.Json;
using System.Text;

namespace FileParsing.Classes.Loaders
{
  public class ExractEverythingLoader : AbstractLoader
  {
    private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
    private List<Weapon> Weapons = new List<Weapon>();
    private Dictionary<string, Type> MapObjectClasses = new Dictionary<string, Type>();
    private Dictionary<string, List<String>> MapOverlays = new Dictionary<string, List<String>>();
    private Dictionary<string, string> PluginMaps = new Dictionary<string, string>();

    public ExractEverythingLoader(DefaultFileProvider provider) : base(provider)
    { }

    public void LoadFiles()
    {
      for (var index = 0; index < Provider.Files.Count; index++)
      {
        var path = Provider.Files.Keys.ElementAt(index);

        Console.WriteLine($"Loading {path}");

        if (index % 100 == 0)
        {
          Console.WriteLine($"{index}/{Provider.Files.Count} ({index / Provider.Files.Count * 100})");
        }

        try
        {
          var file = Provider.LoadObject(path.Substring(0, path.LastIndexOf(".")));

          switch (file)
          {
            case UTexture2D texture:
              {
                var ok = texture.Decode();

                var skiaImage = ok.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

                EnsurePath("result/exports/" + path.Substring(0, path.LastIndexOf("/")));

                File.WriteAllBytes("result/exports/" + path + ".png", skiaImage.ToArray());
                break;
              }

            default:
              continue;
              {
                EnsurePath("result/exports/" + path.Substring(0, path.LastIndexOf("/")));

                File.WriteAllBytes("result/exports/" + path + ".json", Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(file, Formatting.Indented)));

                break;
              }
          }
        }
        catch (Exception e)
        {
          // Console.WriteLine("failed to load " + path + "\n" + e.Message);
        }
      }
    }
  }
}
