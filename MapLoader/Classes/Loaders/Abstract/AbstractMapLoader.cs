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

namespace FileParsing.Classes.Loaders.Abstract
{
    abstract public class AbstractMapLoader : AbstractLoader
    {
        private Dictionary<string, List<String>> MapOverlays = new Dictionary<string, List<String>>();
        private Dictionary<string, string> PluginMaps = new Dictionary<string, string>();

        public AbstractMapLoader(DefaultFileProvider provider) : base(provider)
        {
            PrepareMapOverlays();
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

            HandleMapObjects(test, mapPos, mapRot);

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

        public void LoadMapRecursive(string startPath, FVector mapPos, FRotator mapRot)
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
        }

        abstract public void HandleMapObjects(IEnumerable<UObject> mapObjects, FVector mapPos, FRotator mapRot);
    }
}
