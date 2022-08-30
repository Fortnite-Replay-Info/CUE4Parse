using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using FileParsing.Attributes;

namespace FileParsing.Classes.Map
{
  [MapId("FoodBox_Produce_Athena_C")]
  class FoodBox : AbstractMapObject
  {
    public FoodBox(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
