using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LuciReality.Discovery
{
  /// <summary>Goes through an IP range and sends a request to each device to determine which are our cue modules.</summary>
  public class FindDevices : MonoBehaviour
  {
    List<UnityWebRequestAsyncOperation> requests = new();

    bool allComplete = false;

    void Start()
    {
      for (int i = 0; i < 2; i++)
      {
        for (int j = 0; j < 255; j++)
        {
          var ip = string.Join('.', new int[] { 196, 0, i, j });

          var request = UnityWebRequest.Get($"{ip}:9500/linkDevice");
          requests.Add(request.SendWebRequest());
        }
      }


    }

    void Update()
    {
      if (allComplete) { return; }
      bool areRequestsFinished = true;

      foreach (var request in requests)
      {
        areRequestsFinished = areRequestsFinished && request.isDone;
        if (!areRequestsFinished)
        {
          break;
        }
      }

      if (areRequestsFinished)
      {
        allComplete = true;
        OnCompleted();
      }
    }

    void OnCompleted()
    {
      Debug.Log("requests completed");
    }
  }
}