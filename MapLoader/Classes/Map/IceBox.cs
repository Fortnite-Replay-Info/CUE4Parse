using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using MapLoader.Attributes;

namespace MapLoader.Classes.Map
{
  [MapId("Tiered_Chest_Apollo_IceBox_C")]
  class IceBox : AbstractMapObject
  {
    public IceBox(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
