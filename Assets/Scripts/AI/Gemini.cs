using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LuciReality.Prompt;
using UnityEngine;
using UnityEngine.Networking;

namespace LuciReality.AI
{
  /// <summary>
  /// Queries the Gemini API for a text response.
  /// </summary>
  public static class TextQuery
  {
    /// <summary>Queries the Gemini model with a user prompt.</summary>
    /// <param name="promptText">The user prompt.</param>
    /// <returns>Gemini's response.</returns>
    public static async Task<ModelResponse> Query(string promptText)
    {
      Logger.Log(promptText);
      string chat = BuildChat(promptText);
      Logger.Log(chat);

      return await Gemini.QueryModel(chat);
    }

    /// <summary>Generates a json chat object containing a user defined prompt that can be sent to Gemini as a request.</summary>
    /// <param name="prompt">The user defined prompt.</param>
    /// <returns>The json-encoded chat object</returns>
    private static string BuildChat(string prompt)
    {
      var chat = new Chat();
      var content = new Content { role = "user" };
      var userInstructions = new Part() { text = prompt };
      content.parts = new List<Part> { userInstructions };
      chat.contents = new List<Content> { content };
      
      var jsonQuery = JsonUtility.ToJson(chat, true)[..^2] + ",\n";
      using (var reader = new StreamReader("text_query_template.json"))
      {
        var template = reader.ReadToEnd();
        jsonQuery += template;
      }
      return jsonQuery;
    }
  }

  /// <summary>
  /// Queries the Gemini API for an image response.
  /// </summary>
  public static class ImageQuery
  {
    public static async void Query()
    {
      
    }
  }

  /// <summary>
  /// Helper class that contains a method for making requests to the Gemini API.
  /// </summary>
  public static class Gemini
  {
    public static bool SaveToFile = false;
    public static bool ShowDebugLogs = true;
    public static bool TestMode = false;

    private static string Endpoint() => $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={ApiKeys.Gemini}";
    private static byte[] Encode(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Makes a request to the Gemini model and returns its response.
    /// </summary>
    /// <param name="chat">A Gemini chat history encoded in json format.</param>
    /// <returns>The most recent chat response from the Gemini model.</returns>
    public static async Task<ModelResponse> QueryModel(string chat)
    {
      string responseData = await MakeRequest(chat);

      var chatResponse = JsonUtility.FromJson<ChatRoot>(responseData);
      var textResponse = chatResponse.candidates[0].content.parts[0].text;

      Logger.Log(textResponse);

      return JsonUtility.FromJson<ModelResponse>(textResponse);
    }
    /// <summary>Sends an API request to the Gemini API.</summary>
    /// <param name="chat">A Gemini chat history encoded in json format.</param>
    /// <returns>The Gemini model's response.</returns>
    private static async Awaitable<string> MakeRequest(string chat)
    {
      if (!TestMode)
      {
        UnityWebRequest request = BuildRequest(Endpoint(), Encode(chat));
        await request.SendWebRequest();
        LogResponse(request);

        return request.downloadHandler.text;
      }
      else
      {
        // Load a sample response using the same format.
        using var reader = new StreamReader("sample_response.json");
        return reader.ReadToEnd();
      }
    }

  /// <summary>Generates a UnityWebRequest object that can be used to make a Gemini API request.</summary>
  /// <param name="geminiEndpoint">The Gemini API endpoint for the required model.</param>
  /// <param name="data">A byte array representing the gemini chat history and system instructions.</param>
  /// <returns>UnityWebRequest with required fields set.</returns>
    private static UnityWebRequest BuildRequest(string geminiEndpoint, byte[] data)
    {
      var request = new UnityWebRequest(geminiEndpoint, "POST");
      request.SetRequestHeader("Content-Type", "application/json");
      request.uploadHandler = new UploadHandlerRaw(data);
      request.downloadHandler = new DownloadHandlerBuffer();
      return request;
    }

    /// <summary>Logs errors that occurred during the web request.</summary>
    private static void LogResponse(UnityWebRequest request)
    {
      switch (request.result)
      {
        case UnityWebRequest.Result.ConnectionError:
          Logger.LogError("Connection Error: " + request.error);
          break;
        case UnityWebRequest.Result.DataProcessingError:
          Logger.LogError("Data Processing Error: " + request.error);
          break;
        case UnityWebRequest.Result.ProtocolError:
          Logger.LogError("Protocol Error: " + request.error);
          break;
        case UnityWebRequest.Result.Success:
          Logger.Log("Received: " + request.downloadHandler.text);
          break;
        default:
          Logger.LogError($"Unhandled request result status: {request.result}");
          break;
      }
    }
  }
}