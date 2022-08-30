using CUE4Parse.FileProvider;
using FileParsing.Classes.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileParsing.Classes.Loaders.Abstract;

namespace FileParsing.Classes.Loaders
{
    public class InitLoader : AbstractLoader
    {
        private List<AbstractMapObject> MapObjects = new List<AbstractMapObject>();
        private List<Weapon> Weapons = new List<Weapon>();
        private Dictionary<string, Type> MapObjectClasses = new Dictionary<string, Type>();
        private Dictionary<string, List<String>> MapOverlays = new Dictionary<string, List<String>>();
        private Dictionary<string, string> PluginMaps = new Dictionary<string, string>();

        public InitLoader(DefaultFileProvider provider) : base(provider)
        { }

        public void LoadInis()
        {
            var iniPaths = FindAssets("", null, "ini");
            // iniPaths = iniPaths.Concat(FindAssets("engine/config", null, "ini")).ToList();

            foreach (var path in iniPaths)
            {
                Provider.TrySavePackage(path, out var ini);

                foreach (var kvp in ini)
                {
                    File.WriteAllBytes("result/inis/" + kvp.Key.Replace("/", "_"), kvp.Value);
                }
            }
        }
    }
}
