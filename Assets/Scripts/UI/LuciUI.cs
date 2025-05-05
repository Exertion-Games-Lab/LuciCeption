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

namespace LuciReality.UI
{
  // Presenter
  [RequireComponent(typeof(UIDocument))]
  public class LuciUI : MonoBehaviour
  {
    [SerializeField] Texture2D tex;
    [SerializeField] SettingsViewModel model;
    private UIDocument document; // View
    private VisualElement root;

    private bool isGenerating = false;

    private bool useApis = false;

    protected void Awake()
    {
      tex = new Texture2D(2, 2);
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
        var fanIntensity = 1 + Mathf.RoundToInt(model.Fan.Intensity * 8);
        Task.WaitAll(SetFanIntensity(fanIntensity));
      };

      var generateImage = root.Q<Button>("GenerateImage");
      generateImage.clicked += OnGenerateClicked;
    }

    // private async void OnPromptClicked() => UpdateModel(await TextQuery.Query(model.Prompt));
    private async void OnPromptClicked()
    {
      var modelResponse = await TextQuery.Query(model.Prompt.Text);
      UpdateModel(modelResponse);
      await ImageQuery.Query(model.Prompt.Text, tex);
      VisualElement image = root.Q("Image");
      image.style.backgroundImage = tex;
    }

    private async void OnGenerateClicked()
    {
      if (isGenerating)
      {
        return;
      }

      isGenerating = true;
      try
      {
        await ImageQuery.Query(model.Prompt.Text, tex);
        VisualElement image = root.Q("Image");
        image.style.backgroundImage = tex;
      }
      catch (System.Exception e)
      {
        throw e;
      }
      finally
      {
        isGenerating = false;
      }
    }

    private void UpdateModel(SystemParameters modelResponse)
    {
      model.Audio.Enabled = modelResponse.Audio.Enabled;
      model.Audio.Volume = (float)modelResponse.Audio.Volume;
      model.Lights.Enabled = modelResponse.Light.Enabled;
      model.Lights.Intensity = (float)modelResponse.Light.Intensity;
      model.Ems.Enabled = modelResponse.EMS.Enabled;
      model.Ems.Intensity = (float)modelResponse.EMS.Intensity;
      model.Lra.Enabled = modelResponse.LRA.Enabled;
      model.Lra.Intensity = (float)modelResponse.LRA.Intensity;
      model.Fan.Enabled = modelResponse.Light.Enabled;
      model.Fan.Intensity = (float)modelResponse.Fan.Intensity;
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
      Bind(root.Q<TextField>("PromptInput"), "value", nameof(SettingsViewModel.Prompt.Text));
      Bind(root.Q<Toggle>("AudioToggle"), "value", nameof(SettingsViewModel.Audio.Enabled));
      Bind(root.Q<Slider>("AudioVolume"), "value", nameof(SettingsViewModel.Audio.Volume));
      Bind(root.Q<Toggle>("LightToggle"), "value", nameof(SettingsViewModel.Lights.Enabled));
      Bind(root.Q<Slider>("LightIntensity"), "value", nameof(SettingsViewModel.Lights.Intensity));
      Bind(root.Q<Toggle>("EmsToggle"), "value", nameof(SettingsViewModel.Ems.Enabled));
      Bind(root.Q<Slider>("EmsIntensity"), "value", nameof(SettingsViewModel.Ems.Intensity));
      Bind(root.Q<Toggle>("AppleWatchToggle"), "value", nameof(SettingsViewModel.Lra.Enabled));
      Bind(root.Q<Slider>("AppleWatchIntensity"), "value", nameof(SettingsViewModel.Lra.Intensity));
      Bind(root.Q<Toggle>("FanToggle"), "value", nameof(SettingsViewModel.Fan.Enabled));
      Bind(root.Q<Slider>("FanIntensity"), "value", nameof(SettingsViewModel.Fan.Intensity));
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