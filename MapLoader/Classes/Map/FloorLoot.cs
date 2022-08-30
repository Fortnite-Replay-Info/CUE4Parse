using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using FileParsing.Attributes;

namespace FileParsing.Classes.Map
{
  [MapId("Tiered_Athena_FloorLoot_01_C")]
  class FloorLoot : AbstractMapObject
  {
    public FloorLoot(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
