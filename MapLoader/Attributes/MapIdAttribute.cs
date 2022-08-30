namespace FileParsing.Attributes
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class MapIdAttribute : Attribute
  {
    public string Id { get; private set; }

    public MapIdAttribute(string id)
    {
      Id = id;
    }
  }
}
