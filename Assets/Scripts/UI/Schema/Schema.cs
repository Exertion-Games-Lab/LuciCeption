using System.Collections.Generic;
using UnityEngine;

namespace LuciReality.Prompt
{
  [System.Serializable]
  public class ChatRoot
  {
    public List<Candidate> candidates;
    public string modelVersion;
  }

  [System.Serializable]
  public class Candidate
  {
    public Content content;
    public string finishReason;
    public double avgLogprobs;
  }

  [System.Serializable]
  public class Content
  {
    [SerializeField] public string role;
    [SerializeField] public List<Part> parts;
  }

  [System.Serializable]
  public class Part
  {
    [SerializeField] public string text;
  }

  [System.Serializable]
  public class ChatContents
  {
    [SerializeField] public List<Content> contents;
  }

  [System.Serializable]
  public class AppleWatch
  {
    public bool Enabled;
    public double Intensity;
  }

  [System.Serializable]
  public class Audio
  {
    public bool Enabled;
    public double Volume;
  }

  [System.Serializable]
  public class EMS
  {
    public bool Enabled;
    public double Intensity;
  }

  [System.Serializable]
  public class Fan
  {
    public bool Enabled;
    public double Intensity;
  }

  [System.Serializable]
  public class Light
  {
    public bool Enabled;
    public double Intensity;
  }
  
  [System.Serializable]
  public class ModelReponse
  {
    public AppleWatch AppleWatch;
    public Audio Audio;
    public EMS EMS;
    public Fan Fan;
    public Light Light;
  }
}