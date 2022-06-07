using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using MapLoader.Attributes;

namespace MapLoader.Classes.Map
{
  class Tree : AbstractMapObject
  {
    public Tree(UObject data, FVector parentPos, FRotator parentRot) : base(data, parentPos, parentRot)
    {}
  }
}
