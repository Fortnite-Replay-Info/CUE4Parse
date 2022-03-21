using CUE4Parse_Fortnite.Enums;

class Weapon
{
  public string Name;
  public string Description;
  public string Id;
  public EFortRarity Rarity;
  public string PathName;
  public string LargePreviewImage;
  public string Ammo;
  public string ActorPathName;

  public string ToString() {
    return Rarity.ToString() + " " + Name;
  }
}
