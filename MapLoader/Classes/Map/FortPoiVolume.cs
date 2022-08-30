using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FileParsing.Attributes;

namespace FileParsing.Classes.Map
{
  [MapId("FortPoiVolume")]
  class FortPoiVolume : AbstractMapObject
  {
    public FVector Start;
    public FVector End;

    public FortPoiVolume(UObject data, FVector parentPos, FRotator parentRot) : base (data, parentPos, parentRot)
    {
      var Brush = (UObject?)ReadProperty("Brush", data, typeof(UObject));
      // var LocationTags = GetGameplayTags("LocationTags", data);
    }
  }
}
