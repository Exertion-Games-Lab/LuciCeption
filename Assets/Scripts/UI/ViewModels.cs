using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LuciReality.Prompt;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using LuciReality.AI;

namespace LuciReality.UI
{
  [System.Serializable]
  public class SettingsViewModel
  {
    [SerializeField] public PromptViewModel Prompt;
    [SerializeField] public AudioViewModel Audio;
    [SerializeField] public LightsViewModel Lights;
    [SerializeField] public LraViewModel Lra;
    [SerializeField] public EmsViewModel Ems;
    [SerializeField] public FanViewModel Fan;
  }

  [System.Serializable]
  public class PromptViewModel
  {
    public string Text;
  }

  [System.Serializable]
  public class AudioViewModel
  {
    public bool Enabled;
    [Range(0, 1)] public float Volume;
  }

  [System.Serializable]
  public class LightsViewModel
  {
    public bool Enabled;
    public Color Colour;
    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class LraViewModel
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class EmsViewModel
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }

  [System.Serializable]
  public class FanViewModel
  {
    public bool Enabled;
    [Range(0, 1)] public float Intensity;
  }
}