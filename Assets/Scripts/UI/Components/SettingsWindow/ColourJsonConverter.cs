using System;
using Newtonsoft.Json;
using UnityEngine;

namespace LuciReality.UI
{
  public class ColourJsonConverter : JsonConverter<Color>
  {
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      ColourParams newValue = serializer.Deserialize<ColourParams>(reader);
      newValue ??= new ColourParams { Red = 0, Green = 0, Blue = 0 };
      return new Color(newValue.Red, newValue.Green, newValue.Blue);
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
      var colour = new ColourParams { Red = value.r, Green = value.g, Blue = value.b };
      serializer.Serialize(writer, colour);
    }
  }
}