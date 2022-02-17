using MapParsing.Classes;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;
using System;

namespace MapParsing
{
  public class MapLoader
  {
    private string[] ClassesToSave;
    private DefaultFileProvider Provider;
    private List<Export> Exports = new List<Export>();

    public MapLoader(string[] classesToSave, DefaultFileProvider provider)
    {
      ClassesToSave = classesToSave;
      Provider = provider;
    }

    public IEnumerable<UObject> LoadMap(string filename, FVector mapPos, FRotator mapRot)
    {
      if (!Provider.TryLoadPackage(filename, out var package))
      {
        Console.WriteLine("Unable to load package:" + filename);

        return new List<UObject>();
      }

      var allExports = package.GetExports();

      foreach (var export in allExports)
      {
        if (Array.IndexOf(ClassesToSave, export.Class.Name) != -1)
        {
          var rootComponenent = (UObject)ReadProperty("RootComponent", export, typeof(UObject));
          var exportPos = (FVector)ReadProperty("RelativeLocation", rootComponenent, typeof(FVector));

          exportPos = new FVector
          {
            X = (float)(exportPos.X * Math.Cos(mapRot.Yaw * (Math.PI / 180)) - exportPos.Y * Math.Sin(mapRot.Yaw * (Math.PI / 180))),
            Y = (float)(exportPos.X * Math.Sin(mapRot.Yaw * (Math.PI / 180)) + exportPos.Y * Math.Cos(mapRot.Yaw * (Math.PI / 180))),
            Z = exportPos.Z,
          };

          var worldPos = mapPos + exportPos;

          var exportClass = new Export
          {
            Position = worldPos,
            Object = export,
            StaticId = export.ToString(),
          };

          Exports.Add(exportClass);
        }
      }

      return allExports;
    }

    public List<Export> LoadMapRecursive(string startPath, FVector mapPos, FRotator mapRot)
    {
      var exports = LoadMap(startPath, mapPos, mapRot);

      foreach (var export in exports)
      {
        var pos = new FVector(mapPos.X, mapPos.Y, mapPos.Z);
        var rot = new FRotator(mapRot.Pitch, mapRot.Yaw, mapRot.Roll);
        var hasAdditionalWorlds = HasProperty("AdditionalWorlds", export);
        var hasWorldAsset = HasProperty("WorldAsset", export);
        var hasRootComponenent = HasProperty("RootComponent", export);

        if (hasRootComponenent)
        {
          var rootComponenent = (UObject)ReadProperty("RootComponent", export, typeof(UObject));

          if (rootComponenent != null)
          {
            if (HasProperty("RelativeLocation", rootComponenent))
            {
              var exportPos = (FVector)ReadProperty("RelativeLocation", rootComponenent, typeof(FVector));

              exportPos = new FVector
              {
                X = (float)(exportPos.X * Math.Cos(mapRot.Yaw * (Math.PI / 180)) - exportPos.Y * Math.Sin(mapRot.Yaw * (Math.PI / 180))),
                Y = (float)(exportPos.X * Math.Sin(mapRot.Yaw * (Math.PI / 180)) + exportPos.Y * Math.Cos(mapRot.Yaw * (Math.PI / 180))),
                Z = exportPos.Z,
              };

              pos += exportPos;
            }

            if (HasProperty("RelativeRotation", rootComponenent))
            {
              rot += (FRotator)ReadProperty("RelativeRotation", rootComponenent, typeof(FRotator));
            }
          }
        }

        if (hasWorldAsset)
        {
          var prop = GetProperty("WorldAsset", export);

          if (prop.Tag.GenericValue.GetType() == typeof(FSoftObjectPath))
          {
            var genericValue = (FSoftObjectPath)prop.Tag.GenericValue;

            LoadMapRecursive(Provider.FixPath(genericValue.AssetPathName.PlainText), pos, rot);
          }
        }

        if (hasAdditionalWorlds)
        {
          var prop = GetProperty("AdditionalWorlds", export);

          if (prop.Tag.GenericValue.GetType() == typeof(UScriptArray))
          {
            var genericValue = (UScriptArray)prop.Tag.GenericValue;

            foreach (var map in genericValue.Properties)
            {
              if (map.GenericValue.GetType() == typeof(FSoftObjectPath))
              {
                var mapVal = (FSoftObjectPath)map.GenericValue;

                LoadMapRecursive(Provider.FixPath(mapVal.AssetPathName.PlainText), pos, rot);
              }
            }
          }
        }
      }

      return Exports;
    }

    private FPropertyTag GetProperty(string propertyName, UObject element)
    {
      return element.Properties.Find(x => x.Name.Text == propertyName);
    }

    private bool HasProperty(string propertyName, UObject element)
    {
      return element.Properties.Exists(x => x.Name.Text == propertyName);
    }

    private object? ReadProperty(string propertyName, UObject element, Type type)
    {
      var property = element.Properties.Find(x => x.Name.Text == propertyName);

      if (property == null)
      {
        return null;
      }

      return property.Tag.GetValue(type);
    }
  }
}
