using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.Engine;
using FileParsing.Attributes;
using FileParsing.Classes.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FileParsing.Classes.Loaders.Abstract;

namespace FileParsing.Classes.Loaders
{
  class StructureLoader : AbstractMapLoader
  {
    private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
    private Dictionary<string, string> MappedObjects = new Dictionary<string, string>();
    public StructureLoader(DefaultFileProvider provider) : base(provider) { }

    override public void HandleMapObjects(IEnumerable<UObject> mapObjects, FVector mapPos, FRotator mapRot)
    {
      foreach (var export in mapObjects)
      {
        if (!(export.Class is UBlueprintGeneratedClass))
        {
          continue;
        }

        UBlueprintGeneratedClass blueprint = (UBlueprintGeneratedClass)export.Class;
        var path = Provider.FixPath(blueprint.ClassDefaultObject.Owner.Name);

        if (!MappedObjects.TryGetValue(path, out var type))
        {
          var objectt = LoadObject(path);

          if (objectt.Class.SuperStruct != null)
          {
            type = objectt.Class.SuperStruct.Name;
          }
          else
          {
            type = null;
          }

          MappedObjects.Add(path, type);
        }

        if (type == null)
        {
          continue;
        }

        if (type == "BuildingWall" || type == "Parent_BuildingWall_C")
        {
          try
          {
            MapObjects.Add((AbstractMapObject)new Wall(export, mapPos, mapRot));
          }
          catch (Exception ex)
          {
            Console.WriteLine("Error while creating instance of Wall: " + ex.Message);
          }
        }
        else if (type == "BuildingFloor" || type == "Parent_BuildingFloor_C")
        {
          try
          {
            MapObjects.Add((AbstractMapObject)new Floor(export, mapPos, mapRot));
          }
          catch (Exception ex)
          {
            Console.WriteLine("Error while creating instance of Floor: " + ex.Message);
          }
        }
        else if (type == "BuildingStairs" || type == "Parent_BuildingStairs_C")
        {
          try
          {
            MapObjects.Add((AbstractMapObject)new Ramp(export, mapPos, mapRot));
          }
          catch (Exception ex)
          {
            Console.WriteLine("Error while creating instance of Ramp: " + ex.Message);
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
