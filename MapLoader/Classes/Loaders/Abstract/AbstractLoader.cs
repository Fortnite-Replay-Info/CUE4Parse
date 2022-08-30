using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FileParsing.Classes.Loaders.Abstract
{
  abstract public class AbstractLoader
  {
    protected DefaultFileProvider Provider;

    public AbstractLoader(DefaultFileProvider provider)
    {
      Provider = provider;
    }

    public List<string> FindAssets(string path, string? notPath = null, string? extension = null)
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

    protected FPropertyTag? GetProperty(string propertyName, UObject element)
    {
      return element.Properties.Find(x => x.Name.Text == propertyName);
    }

    protected bool HasProperty(string propertyName, UObject element)
    {
      return element.Properties.Exists(x => x.Name.Text == propertyName);
    }

    protected object? ReadProperty(string propertyName, UObject element, Type type)
    {
      var property = element.Properties.Find(x => x.Name.Text == propertyName);

      if (property == null || property.Tag == null)
      {
        return null;
      }

      return property.Tag.GetValue(type);
    }

    protected HashSet<Assembly> GetAllReferencedAssemblies(Assembly assembly, Dictionary<string, Assembly> allAssemblies)
    {
      HashSet<Assembly> allAssemblyNames = new HashSet<Assembly>();

      foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
      {
        Assembly? referencedAssembly = null;

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

    protected object? ReadStruct(string propertyName, UScriptStruct element)
    {
      foreach (var elementt in ((FStructFallback)element.StructType).Properties)
      {
        if (elementt.Name.Text == propertyName && elementt.Tag != null)
        {
          return elementt.Tag.GenericValue;
        }
      }

      return null;
    }
    protected void EnsurePath(string path)
    {
        var parts = path.Split("/");

        for (int i = 0; i < parts.Length; i++)
        {
            var currentPath = string.Join("/", parts, 0, i + 1);

            if (!System.IO.Directory.Exists(currentPath))
            {
                System.IO.Directory.CreateDirectory(currentPath);
            }
        }
    }
  }
}
