using System.Text;
using UnityEngine;

namespace LuciReality
{
  public static class Logger
  {
    public static void Log(string message)
    {
      Debug.Log(message);
    }

    public static void LogError(string message)
    {
      Debug.LogError(message);
    }
  }
}