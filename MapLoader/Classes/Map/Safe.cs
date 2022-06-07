using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using MapLoader.Attributes;

namespace MapLoader.Classes.Map
{
  [MapId("Tiered_Safe_Athena_C")]
  class Safe : AbstractMapObject
  {
    public Safe(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
