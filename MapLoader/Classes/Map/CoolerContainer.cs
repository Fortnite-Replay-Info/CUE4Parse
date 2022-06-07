using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using MapLoader.Attributes;

namespace MapLoader.Classes.Map
{
  [MapId("B_Cooler_Container_Spawner_C")]
  class CoolerContainer : AbstractMapObject
  {
    public CoolerContainer(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
