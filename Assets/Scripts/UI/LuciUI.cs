using System.Collections.Generic;
using System.IO;
using System.Text;
using LuciReality.Prompt;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace LuciReality.EMS
{
  [System.Serializable]
  public class LuciModel
  {
    [SerializeField] public string Prompt;
    [SerializeField] public bool AudioToggle;

    [Range(0, 1)]
    [SerializeField] public float AudioVolume;
    [SerializeField] public bool LightsToggle;
    [SerializeField] public Color LightsColour;

    [Range(0, 1)]
    [SerializeField] public float LightsIntensity;

    [SerializeField] public bool EmsToggle;

    [Range(0, 1)]
    [SerializeField] public float EmsIntensity;

    [SerializeField] public bool AppleWatchToggle;

    [Range(0, 1)]
    [SerializeField] public float AppleWatchIntensity;

    [SerializeField] public bool FanToggle;
    
    [Range(0, 1)]
    [SerializeField] public float FanIntensity;
  }

  // Presenter
  [RequireComponent(typeof(UIDocument))]
  public class LuciUI : MonoBehaviour
  {
    [SerializeField] LuciModel model;
    private UIDocument document; // View
    private VisualElement root;

    protected void Awake()
    {
      document = GetComponent<UIDocument>();
    }

    protected void Start()
    {
      BindElements();
      ApplyCallbacks();
    }

    private void ApplyCallbacks()
    {
      var submitPrompt = root.Q<Button>("SubmitPrompt");
      submitPrompt.clicked += async () => {
        Debug.Log("Submitted");
        var contents = new ChatContents();
        var userPrompt = new Content {
          role = "user"
        };

        var textPrompt = new Part() {
          text = $"Fill out the JSON datastructure for a dream of: {model.Prompt}."
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

        Debug.Log(serializedPrompt);

        byte[] postData = Encoding.UTF8.GetBytes(serializedPrompt);

        var request = new UnityWebRequest($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={ApiKeys.Gemini}", "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        await request.SendWebRequest();
        if (request.isNetworkError)
          Debug.Log("Error While Sending: " + request.error);
        else
          Debug.Log("Received: " + request.downloadHandler.text);

        var responseData = request.downloadHandler.text;

        // string responseData = "";
        // using (var reader = new StreamReader("sample_response.json"))
        // {
        //   responseData = reader.ReadToEnd();
        // }

        var chat = JsonUtility.FromJson<ChatRoot>(responseData);

        var serializedSettings = chat.candidates[0].content.parts[0].text;

        Debug.Log(serializedSettings);

        var modelResponse = JsonUtility.FromJson<ModelReponse>(serializedSettings);

        model.AudioToggle = modelResponse.Audio.Enabled;
        model.AudioVolume = (float)modelResponse.Audio.Volume;
        model.LightsToggle = modelResponse.Light.Enabled;
        model.LightsIntensity = (float)modelResponse.Light.Intensity;
        model.EmsToggle = modelResponse.EMS.Enabled;
        model.EmsIntensity = (float)modelResponse.EMS.Intensity;
        model.AppleWatchToggle = modelResponse.AppleWatch.Enabled;
        model.AppleWatchIntensity = (float)modelResponse.AppleWatch.Intensity;
        model.FanToggle = modelResponse.Light.Enabled;
        model.FanIntensity = (float)modelResponse.Fan.Intensity;
      };

      var startButton = root.Q<Button>("StartButton");
      startButton.clicked += () => {

      };
    }

    private void BindElements()
    {
      root = document.rootVisualElement;

      // TODO: Create a reusable control for these.
      Bind(root.Q<TextField>("PromptInput"), "value", nameof(LuciModel.Prompt));
      Bind(root.Q<Toggle>("AudioToggle"), "value", nameof(LuciModel.AudioToggle));
      Bind(root.Q<Slider>("AudioVolume"), "value", nameof(LuciModel.AudioVolume));
      Bind(root.Q<Toggle>("LightToggle"), "value", nameof(LuciModel.LightsToggle));
      Bind(root.Q<Slider>("LightIntensity"), "value", nameof(LuciModel.LightsIntensity));
      Bind(root.Q<Toggle>("EmsToggle"), "value", nameof(LuciModel.EmsToggle));
      Bind(root.Q<Slider>("EmsIntensity"), "value", nameof(LuciModel.EmsIntensity));
      Bind(root.Q<Toggle>("AppleWatchToggle"), "value", nameof(LuciModel.AppleWatchToggle));
      Bind(root.Q<Slider>("AppleWatchIntensity"), "value", nameof(LuciModel.AppleWatchIntensity));
      Bind(root.Q<Toggle>("FanToggle"), "value", nameof(LuciModel.FanToggle));
      Bind(root.Q<Slider>("FanIntensity"), "value", nameof(LuciModel.FanIntensity));
    }

    private void Bind<T>(T element, string binding, string name) where T : VisualElement
    {
      element.dataSource = model;
      element.SetBinding(binding, new DataBinding
      {
        bindingMode = BindingMode.TwoWay,
        updateTrigger = BindingUpdateTrigger.EveryUpdate,
        dataSource = model,
        dataSourcePath = PropertyPath.FromName(name)
      });
    }

    protected void Update()
    {
    }
  }
}