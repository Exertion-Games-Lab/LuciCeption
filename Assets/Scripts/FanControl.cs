using UnityEngine;
using UnityEngine.Networking;

public class FanControl : MonoBehaviour
{
  protected async void Start()
  {
    await Awaitable.MainThreadAsync();

    UnityWebRequest request;
    for (int i = 1; i <= 9; i++)
    {
      Debug.Log($"Setting intensity to {i}...");
      
      request = UnityWebRequest.Get(ParticipantConfig.GetFanCommand(i));
      await request.SendWebRequest();
      Debug.Log($"Response: {request.downloadHandler.text}");
      await Awaitable.WaitForSecondsAsync(5);
    }

    Debug.Log("Resetting Intensity...");
    request = UnityWebRequest.Get(ParticipantConfig.GetFanCommand(1));
    await request.SendWebRequest();
  }
}
