using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LuciReality.Prompt;
using UnityEngine;
using UnityEngine.Networking;

namespace LuciReality.AI
{
  public class Gemini : MonoBehaviour
  {
    public static async Awaitable<string> AskAsync(string prompt)
    {
      string geminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={ApiKeys.Gemini}";
      byte[] data = Encoding.UTF8.GetBytes(prompt);
      
      UnityWebRequest request = MakeJsonRequest(geminiEndpoint, data);
      await request.SendWebRequest();

      LogResponse(request);

      return request.downloadHandler.text;
    }

    private static UnityWebRequest MakeJsonRequest(string geminiEndpoint, byte[] data)
    {
      var request = new UnityWebRequest(geminiEndpoint, "POST");
      request.SetRequestHeader("Content-Type", "application/json");
      request.uploadHandler = new UploadHandlerRaw(data);
      request.downloadHandler = new DownloadHandlerBuffer();
      return request;
    }

    private static void LogResponse(UnityWebRequest request)
    {
      switch (request.result)
      {
        case UnityWebRequest.Result.ConnectionError:
          Debug.LogError("Connection Error: " + request.error);
          break;
        case UnityWebRequest.Result.DataProcessingError:
          Debug.LogError("Data Processing Error: " + request.error);
          break;
        case UnityWebRequest.Result.ProtocolError:
          Debug.LogError("Protocol Error: " + request.error);
          break;
        case UnityWebRequest.Result.Success:
          Debug.Log("Received: " + request.downloadHandler.text);
          break;
        default:
          Debug.LogError($"Unhandled request result status: {request.result}");
          break;
      }
    }

    public static string MakePrompt(string promptText)
    {
      var contents = new ChatContents();
      var userPrompt = new Content
      {
        role = "user"
      };

      var textPrompt = new Part()
      {
        text = $"Fill out the JSON datastructure for a dream of: {promptText}."
      };

      userPrompt.parts = new List<Part>
        {
          textPrompt
        };

      contents.contents = new List<Content>
        {
          userPrompt
        };

      var serializedPrompt = JsonUtility.ToJson(contents, true);

      serializedPrompt = serializedPrompt[..^2];
      serializedPrompt += ",\n";

      using (var reader = new StreamReader("prompt.json"))
      {
        var promptData = reader.ReadToEnd();
        serializedPrompt += promptData;
      }

      return serializedPrompt;
    }
  }
}