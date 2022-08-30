using CUE4Parse_Fortnite.Enums;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using System;
using FileParsing.Classes.Loaders.Abstract;
using System.Collections.Generic;

namespace FileParsing.Classes.Loaders
{
    public class WeaponLoader : AbstractLoader
    {
        private Dictionary<string, Ammo> AmmoTypes = new Dictionary<string, Ammo>();
        private List<Weapon> Weapons = new List<Weapon>();

        public WeaponLoader(DefaultFileProvider provider) : base(provider)
        { }

        public List<string> FindWeaponPaths()
        {
            var result = new List<string>();

            foreach (var filePath in Provider.Files.Keys)
            {
                if (filePath.Contains("items") && filePath.Contains("/wid_") && !filePath.Contains("wid_harvest") && !filePath.Contains("savetheworld"))
                {
                    result.Add(filePath);
                }
            }

            return result;
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

            var weaponPaths = FindWeaponPaths();

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

        public Weapon[] GetParsedWeapons()
        {
            return Weapons.ToArray();
        }
    }
}
