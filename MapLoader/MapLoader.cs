using MapParsing.Classes;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.Core.i18N;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using CUE4Parse_Fortnite.Enums;

namespace MapParsing
{
  public class MapLoader
  {
    private string[] ClassesToSave;
    private DefaultFileProvider Provider;
    private List<MapExport> MapObjects = new List<MapExport>();
    private List<Weapon> Weapons = new List<Weapon>();
    private Dictionary<string, Ammo> AmmoTypes = new Dictionary<string, Ammo>();

    public MapLoader(string[] classesToSave, DefaultFileProvider provider)
    {
      ClassesToSave = classesToSave;
      Provider = provider;
    }

    public IEnumerable<UObject> LoadMap(string filename, FVector mapPos, FRotator mapRot)
    {
      if (!Provider.TryLoadPackage(filename, out var package))
      {
        Console.WriteLine("Unable to load package: " + filename);

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

          var exportClass = new MapExport
          {
            Position = worldPos,
            Object = export,
            StaticId = export.ToString(),
          };

          MapObjects.Add(exportClass);
        }
      }

      return allExports;
    }

    public List<MapExport> LoadMapRecursive(string startPath, FVector mapPos, FRotator mapRot)
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

      return MapObjects;
    }

    public List<string> FindAssets(string path, string notPath = null)
    {
      var result = new List<string>();
      var lowerPath = path.ToLower();

      foreach (var filePath in this.Provider.Files.Keys)
      {
        if (filePath.Contains(lowerPath) && (notPath == null || !filePath.Contains(notPath.ToLower()) && !filePath.EndsWith(".ubulk")))
        {
          result.Add(filePath);
        }
      }

      return result;
    }

    public UObject? LoadObject(string path)
    {
      if (!Provider.TryLoadPackage(path, out var package))
      {
        Console.WriteLine("Unable to load package:" + path);

        return null;
      }

      var allExports = package.GetExports();

      foreach (var export in allExports)
      {
        if (export.Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject) || export.Flags.HasFlag(EObjectFlags.RF_Standalone))
        {
          return export;
        }
      }

      return null;
    }

    private void ParseAmmo()
    {
      var ammoPaths = FindAssets("fortnitegame/content/Athena/Items/Ammo/");

      foreach (var path in ammoPaths)
      {
        var ammoExport = LoadObject(path);

        var name = (FText?)ReadProperty("DisplayName", ammoExport, typeof(FText));
        var description = (FText?)ReadProperty("Description", ammoExport, typeof(FText));
        var largePreviewImage = (FSoftObjectPath?)ReadProperty("LargePreviewImage", ammoExport, typeof(FSoftObjectPath));
        var bSupportsQuickbarFocus = (bool?)ReadProperty("bSupportsQuickbarFocus", ammoExport, typeof(bool));
        var rarity = (EFortRarity?)ReadProperty("Rarity", ammoExport, typeof(EFortRarity));

        var ammo = new Ammo
        {
          Name = name == null ? null : name.Text,
          Description = description == null ? null : description.Text,
          Id = ammoExport.Name,
          PathName = ammoExport.GetPathName(),
          LargePreviewImage = largePreviewImage == null ? null : ((FSoftObjectPath)largePreviewImage).AssetPathName.PlainText,
          bSupportsQuickbarFocus = bSupportsQuickbarFocus == null ? false : (bool)bSupportsQuickbarFocus,
          Rarity = rarity == null ? EFortRarity.Common : (EFortRarity)rarity,
        };

        AmmoTypes.Add(ammo.PathName, ammo);
      }
    }

    public void ParseWeapons()
    {
      ParseAmmo();

      var weaponPaths = FindAssets("fortnitegame/content/athena/items/weapons/", "fortnitegame/content/athena/items/weapons/wid_harvest");

      foreach (var path in weaponPaths)
      {
        var weaponExport = LoadObject(path);

        if (weaponExport == null)
        {
          Console.WriteLine("Failed to load weapon: " + path);

          continue;
        }

        var ammoData = (FSoftObjectPath?)ReadProperty("AmmoData", weaponExport, typeof(FSoftObjectPath));
        var name = (FText?)ReadProperty("DisplayName", weaponExport, typeof(FText));
        var description = (FText?)ReadProperty("Description", weaponExport, typeof(FText));
        var largePreviewImage = (FSoftObjectPath?)ReadProperty("LargePreviewImage", weaponExport, typeof(FSoftObjectPath));
        var rarity = (EFortRarity?)ReadProperty("Rarity", weaponExport, typeof(EFortRarity));
        var actorPathName = (FSoftObjectPath?)ReadProperty("WeaponActorClass", weaponExport, typeof(FSoftObjectPath));

        var weapon = new Weapon
        {
          Name = name == null ? null : name.Text,
          Description = description == null ? null : description.Text,
          Id = weaponExport.Name,
          PathName = weaponExport.GetPathName(),
          LargePreviewImage = largePreviewImage == null ? null : ((FSoftObjectPath)largePreviewImage).AssetPathName.PlainText,
          Rarity = rarity == null ? EFortRarity.Common : (EFortRarity)rarity,
          ActorPathName = actorPathName == null ? null : ((FSoftObjectPath)actorPathName).AssetPathName.PlainText,
        };

        if (ammoData != null)
        {
          if (AmmoTypes.TryGetValue(((FSoftObjectPath)ammoData).AssetPathName.PlainText, out var ammoType))
          {
            weapon.Ammo = ammoType.Id;
          }
        }

        Weapons.Add(weapon);
      }
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

    private FName? ReadStruct(string propertyName, UObject element)
    {
      var property = GetProperty(propertyName, element);

      var structt = (FStructFallback)property.Tag.GetValue(typeof(FStructFallback));

      return (FName)structt.Properties[0].Tag.GetValue(typeof(FName));
    }
  }
}
