using System;
using UnityEngine;

namespace LuciReality.UI
{
  [Serializable]
  public class PromptModel
  {
    public PromptModel()
    {
      Text = "";
      Image = new Texture2D(2, 2)
      {
        hideFlags = HideFlags.DontSave
      };
    }

    public string Text
    {
      get;
      set;
    }

    public Texture2D Image
    {
      get;
      set;
    }
  }
}