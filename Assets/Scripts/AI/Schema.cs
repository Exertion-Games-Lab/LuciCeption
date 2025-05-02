using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LuciReality.Prompt
{

  [Serializable]
  public class InlineData
  {
    public string mimeType;
    public string data;
  }

  [Serializable]
  public class GeminiMessage
  {
    public List<Candidate> candidates;
    public string modelVersion;
  }

  [Serializable]
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

  [Serializable]
  public class GenerateContentConfig
  {
    public List<string> responseModalities;
    public string responseMimeType;
    public JObject responseSchema;
  }

  [Serializable]
  public class Part
  {
    public string text;
    public InlineData inlineData;

    public static Part FromText(string partText)
    {
      return new Part
      {
        text = partText
      };
    }
  }

  [System.Serializable]
  public class GeminiQuery
  {
    public JObject systemInstruction;
    [SerializeField] public GenerateContentConfig generationConfig;
    [SerializeField] public List<Content> contents;
  }

  [Serializable]
  public class Linear
  {
    public bool Enabled;
    public double Intensity;
  }

  [Serializable]
  public class Audio
  {
    public bool Enabled;
    public double Volume;
  }

  [Serializable]
  public class EMS
  {
    public bool Enabled;
    public double Intensity;
  }

  [Serializable]
  public class Fan
  {
    public bool Enabled;
    public double Intensity;
  }

  [Serializable]
  public class Light
  {
    public bool Enabled;
    public double Intensity;
  }
  
  [Serializable]
  public class ModelResponse
  {
    public Linear LRA;
    public Audio Audio;
    public EMS EMS;
    public Fan Fan;
    public Light Light;
  }
}