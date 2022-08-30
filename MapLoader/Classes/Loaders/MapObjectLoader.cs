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
    class MapObjectLoader : AbstractMapLoader
    {
        private Dictionary<string, Type> MapObjectClasses = new Dictionary<string, Type>();
        private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
        public MapObjectLoader(DefaultFileProvider provider) : base(provider)
        {
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

        override public void HandleMapObjects(IEnumerable<UObject> mapObjects, FVector mapPos, FRotator mapRot)
        {
            foreach (var export in mapObjects)
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
            }
        }

        public List<AbstractMapObject> GetResult()
        {
            return MapObjects;
        }
    }
}
