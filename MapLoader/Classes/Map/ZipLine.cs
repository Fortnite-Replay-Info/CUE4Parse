using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using MapLoader.Attributes;
using System;

namespace MapLoader.Classes.Map
{
  [MapId("BP_Athena_Environmental_ZipLine_Spline_C")]
  class ZipLine : AbstractMapObject
  {
    public FVector Start;
    public FVector End;

    public ZipLine(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {
      var PoleASocketLocation = (FVector?)ReadProperty("PoleASocketLocation", data, typeof(FVector));
      var PoleBSocketLocation = (FVector?)ReadProperty("PoleBSocketLocation", data, typeof(FVector));

      Start = GetOffsetPosition((FVector)PoleASocketLocation);
      End = GetOffsetPosition((FVector)PoleBSocketLocation);
    }
  }
}
