using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;

namespace MapLoader.Classes.Map
{
  public abstract class AbstractMapObject
  {
    public FVector Position;
    public string StaticId;
    public string Type;
    private FVector ParentPos;
    private FRotator ParentRot;

    public AbstractMapObject(UObject data, FVector parentPos, FRotator parentRot)
    {
      ParentPos = parentPos;
      ParentRot = parentRot;

      var rootComponenent = (UObject)ReadProperty("RootComponent", data, typeof(UObject));
      var exportPos = (FVector)ReadProperty("RelativeLocation", rootComponenent, typeof(FVector));

      Position = GetOffsetPosition(exportPos);
      StaticId = data.ToString();
      Type = data.Class.Name;
    }

    public FVector GetOffsetPosition(FVector pos)
    {
      var offsetPos = new FVector
      {
        X = (float)(pos.X * Math.Cos(ParentRot.Yaw * (Math.PI / 180)) - pos.Y * Math.Sin(ParentRot.Yaw * (Math.PI / 180))),
        Y = (float)(pos.X * Math.Sin(ParentRot.Yaw * (Math.PI / 180)) + pos.Y * Math.Cos(ParentRot.Yaw * (Math.PI / 180))),
        Z = pos.Z,
      };

      return offsetPos + ParentPos;
    }

    protected FPropertyTag GetProperty(string propertyName, UObject element)
    {
      return element.Properties.Find(x => x.Name.Text == propertyName);
    }

    static protected bool HasProperty(string propertyName, UObject element)
    {
      return element.Properties.Exists(x => x.Name.Text == propertyName);
    }

    protected object? ReadProperty(string propertyName, UObject element, Type type)
    {
      var property = element.Properties.Find(x => x.Name.Text == propertyName);

      if (property == null)
      {
        return null;
      }

      return property.Tag.GetValue(type);
    }

    protected FName? ReadStruct(string propertyName, UObject element)
    {
      var property = GetProperty(propertyName, element);

      var structt = (FStructFallback)property.Tag.GetValue(typeof(FStructFallback));

      return (FName)structt.Properties[0].Tag.GetValue(typeof(FName));
    }
  }
}
