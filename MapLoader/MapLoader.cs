using CUE4Parse_Fortnite.Enums;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using MapLoader.Attributes;
using MapLoader.Classes.Map;
using MapParsing.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace MapParsing
{
    public class MapLoader
    {
        private DefaultFileProvider Provider;
        private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
        private List<Weapon> Weapons = new List<Weapon>();
        private Dictionary<string, Ammo> AmmoTypes = new Dictionary<string, Ammo>();
        private Dictionary<string, Type> MapObjectClasses = new Dictionary<string, Type>();
        private Dictionary<string, List<String>> MapOverlays = new Dictionary<string, List<String>>();
        private Dictionary<string, string> PluginMaps = new Dictionary<string, string>();

        public MapLoader(DefaultFileProvider provider)
        {
            Provider = provider;

            Dictionary<string, Assembly> allAssemblies = new Dictionary<string, Assembly>();

            foreach (var ok in AppDomain.CurrentDomain.GetAssemblies())
            {
                allAssemblies.Add(ok.FullName, ok);
            }

            HashSet<Assembly> referencedAssemblies = GetAllReferencedAssemblies(GetType().Assembly, allAssemblies);

            referencedAssemblies.Add(GetType().Assembly);

            List<Type> allTypes = new List<Type>();

            foreach (var ok in referencedAssemblies)
            {
                allTypes.AddRange(ok.GetTypes());
            }

            List<Type> mapObjects = new List<Type>();

            foreach (var ok in allTypes)
            {
                if (ok.GetCustomAttribute<MapIdAttribute>(false) != null)
                {
                    mapObjects.Add(ok);
                }
            }

            foreach (Type type in mapObjects)
            {
                MapIdAttribute[] mapIdAttributes = (MapIdAttribute[])type.GetCustomAttributes<MapIdAttribute>(false);

                foreach (MapIdAttribute mapIdAttribute in mapIdAttributes)
                {
                    if (mapIdAttribute.Id == null)
                    {
                        continue;
                    }

                    MapObjectClasses.Add(mapIdAttribute.Id, type);
                }
            }
        }

        public IEnumerable<UObject> LoadMap(string filename, FVector mapPos, FRotator mapRot)
        {
            Console.WriteLine("Loading map: " + filename);
            if (!Provider.TryLoadPackage(filename, out var package))
            {
                Console.WriteLine("Unable to load package: " + filename);

                return new List<UObject>();
            }

            var allExports = package.GetExports();
            var test = Provider.LoadObjectExports(filename);

            foreach (var export in test)
            {
                if (MapObjectClasses.TryGetValue(export.Class.Name, out Type type))
                {
                  try
                  {
                    MapObjects.Add((AbstractMapObject)Activator.CreateInstance(type, export, mapPos, mapRot));
                  }
                  catch (TargetInvocationException ex)
                  {
                    Console.WriteLine("Error while creating instance of " + type.Name + ": " + ex.InnerException.Message);
                  }
                }
                // var name = export.Class.Name.ToLower();
                // if (name.Contains("street"))
                // {
                //     continue;
                // }
//  || name.Contains("tree") || name.Contains("palm")
                // if (name.Contains("memory"))
                // {
                //     try
                //     {
                //         MapObjects.Add((AbstractMapObject)new Tree(export, mapPos, mapRot));
                //     }
                //     catch (Exception ex)
                //     {
                //         Console.WriteLine("Error while creating instance of Tree: " + ex.Message);
                //     }
                // }
            }

            Console.WriteLine("Loaded " + MapObjects.Count + " objects");

            if (MapOverlays.TryGetValue(package.Name, out List<String> overlays))
            {
                Console.WriteLine("Found " + overlays.Count + " overlays");

                foreach (var overlay in overlays)
                {
                    if (PluginMaps.TryGetValue(overlay, out string pluginMap))
                    {
                        allExports = allExports.Concat(LoadMap(pluginMap, mapPos, mapRot));
                    }
                    else
                    {
                        Console.WriteLine("Unable to find plugin map: " + overlay);
                    }
                }
            }

            return allExports;
        }

        public void PrepareMapOverlays()
        {
            var overlays = FindAssets("leveloverlay", ".umap");
            var maps = FindAssets("plugins/", null, "umap");

            foreach (var overlay in overlays)
            {
                var test = Provider.LoadPackage(overlay).GetExports();

                foreach (var export in test)
                {
                    if (export.ExportType != "FortLevelOverlayConfig")
                    {
                        continue;
                    }

                    foreach (var prop in export.Properties)
                    {
                        var genericValue = (UScriptArray)prop.Tag.GenericValue;

                        foreach (var prop2 in genericValue.Properties)
                        {
                            var overlayWorld = (FSoftObjectPath)ReadStruct("OverlayWorld", (UScriptStruct)prop2.GenericValue);
                            var sourceWorld = (String)ReadStruct("SourceWorldString", (UScriptStruct)prop2.GenericValue);

                            var sourceWorldTrimmed = sourceWorld.Split('.').First();

                            if (!MapOverlays.TryGetValue(sourceWorldTrimmed, out List<String> mapOverlay))
                            {
                                mapOverlay = new List<String>();

                                MapOverlays.Add(sourceWorldTrimmed, mapOverlay);
                            }

                            mapOverlay.Add(overlayWorld.AssetPathName.Text.Split('.').First());
                        }
                    }
                }
            }

            foreach (var map in maps)
            {
                var test = Provider.LoadPackage(map);

                PluginMaps.TryAdd(test.Name, map);
            }
        }

        public List<AbstractMapObject> LoadMapRecursive(string startPath, FVector mapPos, FRotator mapRot)
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

        public List<string> FindAssets(string path, string notPath = null, string extension = null)
        {
            var result = new List<string>();
            var lowerPath = path.ToLower();

            foreach (var filePath in this.Provider.Files.Keys)
            {
                if (filePath.Contains(lowerPath) && (notPath == null || !filePath.Contains(notPath.ToLower()) && !filePath.EndsWith(".ubulk")) && (extension == null || filePath.EndsWith(extension)))
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

        private object? ReadStruct(string propertyName, UScriptStruct element)
        {
            foreach (var elementt in ((FStructFallback)element.StructType).Properties)
            {
                if (elementt.Name.Text == propertyName)
                {
                    return elementt.Tag.GenericValue;
                }
            }

            return null;
        }

        private HashSet<Assembly> GetAllReferencedAssemblies(Assembly assembly, Dictionary<string, Assembly> allAssemblies)
        {
            HashSet<Assembly> allAssemblyNames = new HashSet<Assembly>();

            foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
            {
                Assembly referencedAssembly = null;

                if (!allAssemblies.TryGetValue(assemblyName.FullName, out referencedAssembly))
                {
                    continue;
                }

                allAssemblyNames.Add(allAssemblies[assemblyName.FullName]);

                foreach (Assembly newAssembly in GetAllReferencedAssemblies(referencedAssembly, allAssemblies))
                {
                    allAssemblyNames.Add(newAssembly);
                }
            }

            return allAssemblyNames;
        }
    }
}
