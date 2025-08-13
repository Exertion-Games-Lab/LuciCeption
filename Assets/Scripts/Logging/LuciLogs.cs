using System;
using System.IO;
using System.Text;
using LuciReality.Prompt;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;

namespace LuciReality
{

  public class LuciLogs : MonoBehaviour
  {
    protected void Awake()
    {
      Unity.Logging.Log.Logger = new Unity.Logging.Logger(new LoggerConfig()
      .MinimumLevel.Debug()
      .SyncMode.FatalIsSync()
      .CaptureStacktrace()
      .WriteTo.JsonFile($"{Application.dataPath}/Logs/{string.Format("{0:yyyy-MM-dd-HH-mm-ss-fff}", DateTime.Now)}.json", minLevel: LogLevel.Debug)
      .WriteTo.UnityEditorConsole(minLevel: LogLevel.Debug));
    }
    
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