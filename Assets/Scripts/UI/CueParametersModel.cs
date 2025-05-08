using LuciReality.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace LuciReality
{
  [System.Serializable]
  public class CueParametersModel
  {
    public CueParametersModel()
    {
      Audio = new();
      Lights = new();
      Lra = new();
      Ems = new();
      Fan = new();
    }

    [JsonProperty("Audio")] public AudioParameters Audio;
    [JsonProperty("Light")] public LightParameters Lights;
    [JsonProperty("LRA")] public LraParameters Lra;
    [JsonProperty("EMS")] public EmsParameters Ems;
    [JsonProperty("Fan")] public FanParameters Fan;
  }

  [System.Serializable]
  public class PromptParameters
  {
    public string Text;
  }

  [System.Serializable]
  public class AudioParameters
  {
    public bool Enabled;
    [JsonProperty("Intensity")] [Range(0, 1)] public float Volume;
  }

  [System.Serializable]
  public class LightParameters
  {
    public bool Enabled;
    [JsonIgnore] public Color Colour;

    // Allow for deserialisation but not serialisation
    [JsonProperty("Colour")]
    private Color ColourSetter
    {
      set { Colour = value; }
    }

    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class LraParameters
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class EmsParameters
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class FanParameters
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }
}