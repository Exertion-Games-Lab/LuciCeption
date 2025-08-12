using UnityEngine;

namespace LuciReality
{
  public class SystemConfig : MonoBehaviour
  {
    public string Prompt {get; set;}

    public bool AudioToggle {get; set;}

    [Range(0, 1)]
    public float AudioVolume {get; set;}

    public bool LightsToggle;
    public Color LightsColour;

    [Range(0, 1)]
    public float LightsIntensity;

    public bool EmsToggle;

    [Range(0, 1)]
    public float EmsIntensity;

    public bool AppleWatchToggle;

    [Range(0, 1)]
    public float AppleWatchIntensity;

    public bool FanToggle;
    
    [Range(0, 1)]
    public bool FanIntensity;
  }
}