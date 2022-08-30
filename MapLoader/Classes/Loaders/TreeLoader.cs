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

namespace FileParsing.Classes.Loaders
{
    class TreeLoader : AbstractMapLoader
    {
        private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
        public TreeLoader(DefaultFileProvider provider) : base(provider) { }

        override public void HandleMapObjects(IEnumerable<UObject> mapObjects, FVector mapPos, FRotator mapRot)
        {
            foreach (var export in mapObjects)
            {
                var name = export.Class.Name.ToLower();

                if (name.Contains("street"))
                {
                    continue;
                }

                if (name.Contains("pine") || name.Contains("tree") || name.Contains("palm"))
                {
                    try
                    {
                        MapObjects.Add((AbstractMapObject)new Tree(export, mapPos, mapRot));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while creating instance of Tree: " + ex.Message);
                    }
                }
            }
        }

        public List<AbstractMapObject> GetResult()
        {
            return MapObjects;
        }
    }
}
