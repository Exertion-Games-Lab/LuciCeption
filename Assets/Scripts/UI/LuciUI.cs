using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LuciReality.Prompt;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using LuciReality.AI;

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

    private bool useApis = false;

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
      submitPrompt.clicked += OnPromptClicked;

      var startButton = root.Q<Button>("StartButton");
      startButton.clicked += () => {
        var fanIntensity = 1 + Mathf.RoundToInt(model.FanIntensity * 8);
        Task.WaitAll(SetFanIntensity(fanIntensity));
      };
    }

    private async void OnPromptClicked() => UpdateModel(await TextQuery.Query(model.Prompt));

    private void UpdateModel(ModelResponse modelResponse)
    {
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
    }

    async Task SetFanIntensity(int i)
    {
      if (!useApis)
      {
        Debug.Log($"Fan intensity set to: {i}");
        return;
      }

      var request = UnityWebRequest.Get($"https://maker.ifttt.com/trigger/fan_intensity_{i}/with/key/88IAzsMm2FG355HNDbZjq");
      await request.SendWebRequest();
      if (Application.exitCancellationToken.IsCancellationRequested)
        return;

      Debug.Log($"Response: {request.downloadHandler.text}");
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