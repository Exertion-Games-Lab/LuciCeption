using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LuciReality.Prompt;
using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Logging;

namespace LuciReality.AI
{
  /// <summary>
  /// Queries the Gemini API for a text response.
  /// </summary>
  public static class TextQuery
  {
    private static string Endpoint() => $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-04-17:generateContent?key={ApiKeys.Gemini}";
    /// <summary>Queries the Gemini model with a user prompt.</summary>
    /// <param name="prompt">The user prompt.</param>
    /// <returns>Gemini's response.</returns>
    public static async Task<CueParametersModel> Query(string prompt)
    {
      Log.Info("{logType}] {queryType} query started with: \'{prompt}\'", "GEMINI", "text", prompt);
      string chat = await BuildQuery(prompt);
      Log.Debug("{logType}] chat message created: \'{chat}\'", "GEMINI", chat);

      var textResponse = await Gemini.QueryModel<GeminiMessage>(Endpoint(), chat);
      Debug.Log(textResponse);
      Log.Debug("{logType}] response received: \'{geminiResponse}\'", "GEMINI", JsonConvert.SerializeObject(textResponse));
      var sysParams = JsonConvert.DeserializeObject<CueParametersModel>(textResponse.candidates[0].content.parts[1].text);
      Log.Info("{logType}] system parameters received from Gemini: \'{logParams}\'", "GEMINI", JsonConvert.SerializeObject(sysParams));
      Log.Info("[{logType}] reasoning was: [{reasoning}]", "GEMINI", textResponse.candidates[0].content.parts[0].text);

      sysParams.ReasoningText = textResponse.candidates[0].content.parts[0].text;

      return sysParams;
    }

    /// <summary>Generates a json chat object containing a user defined prompt that can be sent to Gemini as a request.</summary>
    /// <param name="prompt">The user defined prompt.</param>
    /// <returns>The json-encoded chat object</returns>
    private static async Task<string> BuildQuery(string prompt)
    {
      var query = new GeminiQuery {
        systemInstruction = JObject.Parse(await TextQuerySchema.Instructions()),
        contents = new List<Content>() {
          new Content() {
            role = "user",
            parts = new List<Part>() { Part.FromText(prompt) }
          }
        },
        generationConfig = new GenerateContentConfig
        {
          responseModalities = new List<string>() { "text" },
          responseMimeType = "application/json",
          responseSchema = JObject.Parse(await TextQuerySchema.ResponseSchema()),
          thinkingConfig = new ThinkingConfig
          {
            includeThoughts = true,
          },
        }
      };

      var jsonQuery = JsonConvert.SerializeObject(query);
      return jsonQuery;
    }
  }

  /// <summary>
  /// Queries the Gemini API for an image response.
  /// </summary>
  public static class ImageQuery
  {
    private static string PromptTemplate(string prompt) => $"Generate a vibrant dream-like painting to help user dream about: \"{prompt}\"";
    private static string Endpoint() => $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp-image-generation:streamGenerateContent?key={ApiKeys.Gemini}";
    public static async Task Query(string prompt, Texture2D img)
    {
      Log.Info("[{logType}] {queryType} query started with: \'{prompt}\'", "GEMINI", "image", prompt);
      string chat = await BuildQuery(PromptTemplate(prompt));
      Log.Debug("[{logType}] chat message created: \'{chat}\'", "GEMINI", chat);

      var messages = await Gemini.QueryModel<List<GeminiMessage>>(Endpoint(), chat);

      foreach (var msg in messages)
      {
        if (msg.candidates == null || msg.candidates.Count == 0) { continue; }
        if (msg.candidates[0].content == null || msg.candidates[0].content.parts == null) { continue; }
        if (msg.candidates[0].content.parts.Count == 0) { continue; }

        var inlineData = msg.candidates[0].content.parts[0].inlineData;
        if (inlineData != null)
        {
          var dataBytes = DataToBytes(inlineData.data);

          var filename = string.Format("{0:yyyy-MM-dd-HH-mm-ss-fff}.png", DateTime.Now);
          using var writer = File.Create(Path.Join(Application.dataPath, "Logs", filename));
          writer.Write(dataBytes);
          writer.Close();
          Log.Info("[{logType}] image received: {mimeType}, written to file: {filename}", "GEMINI", inlineData.mimeType, filename);

          switch (inlineData.mimeType)
          {
            case "image/png":
            case "image/jpeg":
              ImageConversion.LoadImage(img, dataBytes);
              break;
            default:
              throw new Exception("Unsupported MIME type");
          }
          return;
        }
      }
    }

    private static byte[] DataToBytes(string data) => Convert.FromBase64String(data);

    private static async Task<string> BuildQuery(string prompt)
    {
      var query = new GeminiQuery
      {
        contents = new List<Content>()
        {
          new Content()
          {
            role = "user",
            parts = new List<Part>() { Part.FromText(prompt) }
          }
        },
        generationConfig = new GenerateContentConfig
        {
          responseModalities = new List<string>() { "text", "image" },
          responseMimeType = "text/plain",
        },
      };

      var jsonQuery = JsonConvert.SerializeObject(query, Formatting.Indented, new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore
});
      return jsonQuery;
    }
  }

  /// <summary>
  /// Allows interaction with the Gemini API.
  /// </summary>
  public class GeminiClient
  {
    private string apiKey;


    public GeminiClient(string apiKey)
    {
      this.apiKey = apiKey;
    }

    private static Byte[] Encode(string text) => Encoding.UTF8.GetBytes(text);
  }

  /// <summary>
  /// Helper class that contains a method for making requests to the Gemini API.
  /// </summary>
  public static class Gemini
  {
    public static bool TestMode = false;
    private static byte[] Encode(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Makes a request to the Gemini model and returns its response as .NET object.
    /// </summary>
    /// <param name="chat">A Gemini chat history encoded in json format.</param>
    /// <returns>The most recent response from the Gemini model.</returns>
    public static async Task<T> QueryModel<T>(string endpoint, string chat) where T : class
    {
      string responseData = await MakeRequest(endpoint, chat);
      Debug.Log(responseData);
      // LuciLogs.Log(responseData);
      // var chatResponse = JsonConvert.DeserializeObject<List<GeminiMessage>>(responseData);
      // var chatResponse = JsonUtility.FromJson<GeminiResponse>(responseData);
      // var textResponse = chatResponse.candidates[0].content.parts[0].text;

      JsonReader reader = new JsonTextReader(new StringReader(responseData));
      JToken startToken = await JToken.LoadAsync(reader);

      switch (startToken.Type)
      {
        case JTokenType.Object:
          if (typeof(T) != typeof(GeminiMessage))
          {
            throw new Exception($"Generic parameter {nameof(T)} must be {nameof(GeminiMessage)} for text responses.");
          }
          var msg = JsonConvert.DeserializeObject<GeminiMessage>(responseData);
          return msg as T;
        case JTokenType.Array:
          if (typeof(T) != typeof(List<GeminiMessage>))
          {
            throw new Exception($"Generic parameter {nameof(T)} must be {nameof(List<GeminiMessage>)} for image responses.");
          }
          var msgList = JsonConvert.DeserializeObject<List<GeminiMessage>>(responseData);
          return msgList as T;
        default:
          throw new Exception("Gemini returned an unhandled response type.");
      }
    }
    /// <summary>Sends an API request to the Gemini API.</summary>
    /// <param name="chat">A Gemini chat history encoded in json format.</param>
    /// <returns>The Gemini model's response.</returns>
    private static async Awaitable<string> MakeRequest(string endpoint, string chat)
    {
      if (!TestMode)
      {
        UnityWebRequest request = BuildRequest(endpoint, Encode(chat));
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
          // LuciLogs.LogError("Connection Error: " + request.error);
          break;
        case UnityWebRequest.Result.DataProcessingError:
          // LuciLogs.LogError("Data Processing Error: " + request.error);
          break;
        case UnityWebRequest.Result.ProtocolError:
          // LuciLogs.LogError("Protocol Error: " + request.error);
          break;
        case UnityWebRequest.Result.Success:
          // LuciLogs.Log("Received: " + request.downloadHandler.text);
          break;
        default:
          // LuciLogs.LogError($"Unhandled request result status: {request.result}");
          break;
      }
    }
  }
}