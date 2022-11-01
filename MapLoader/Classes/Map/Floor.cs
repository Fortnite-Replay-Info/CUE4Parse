using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using FileParsing.Attributes;

namespace FileParsing.Classes.Map
{
  class Floor : AbstractMapObject
  {
    public FRotator rotation;
    public string BuildingType = "Floor";

    public Floor(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {
      var exportRot = (FRotator?)ReadProperty("RelativeRotation", RootComponent, typeof(FRotator));

      if (exportRot is null)
      {
        return;
      }

      rotation = parentRot + (FRotator)exportRot;
    }
  }
}
