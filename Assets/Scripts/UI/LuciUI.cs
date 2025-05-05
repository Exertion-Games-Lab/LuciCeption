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
using Unity.Logging;

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
      if (isGenerating)
      {
        return;
      }

      Log.Info("[{logType}]: The user clicked submit prompt", "USER_INTERFACE");
      var modelResponse = await TextQuery.Query(model.Prompt.Text);
      UpdateModel(modelResponse);

      isGenerating = true;
      try
      {
        Task[] tasks = {
          QueryModel(),
          QueryImage()
        };
        await Task.WhenAll(tasks);
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

    private async Task QueryModel()
    {
      var modelResponse = await TextQuery.Query(model.Prompt.Text);
      UpdateModel(modelResponse);
    }

    private async Task QueryImage()
    {
      await ImageQuery.Query(model.Prompt.Text, tex);
      VisualElement image = root.Q("Image");
      image.style.backgroundImage = tex;
    }

    private async void OnGenerateClicked()
    {
      Log.Info("[{logType}]: The user clicked generate image", "USER_INTERFACE");
      if (isGenerating)
      {
        return;
      }

      isGenerating = true;
      try
      {
        await QueryImage();
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
      Bind(root.Q<TextField>("PromptInput"), "value", model.Prompt, nameof(SettingsViewModel.Prompt.Text));
      Bind(root.Q<Toggle>("AudioToggle"), "value", model.Audio, nameof(SettingsViewModel.Audio.Enabled));
      Bind(root.Q<Slider>("AudioVolume"), "value", model.Audio, nameof(SettingsViewModel.Audio.Volume));
      Bind(root.Q<Toggle>("LightToggle"), "value", model.Lights, nameof(SettingsViewModel.Lights.Enabled));
      Bind(root.Q<Slider>("LightIntensity"), "value", model.Lights, nameof(SettingsViewModel.Lights.Intensity));
      Bind(root.Q<Toggle>("EmsToggle"), "value", model.Ems, nameof(SettingsViewModel.Ems.Enabled));
      Bind(root.Q<Slider>("EmsIntensity"), "value", model.Ems, nameof(SettingsViewModel.Ems.Intensity));
      Bind(root.Q<Toggle>("AppleWatchToggle"), "value", model.Lra, nameof(SettingsViewModel.Lra.Enabled));
      Bind(root.Q<Slider>("AppleWatchIntensity"), "value", model.Lra, nameof(SettingsViewModel.Lra.Intensity));
      Bind(root.Q<Toggle>("FanToggle"), "value", model.Fan, nameof(SettingsViewModel.Fan.Enabled));
      Bind(root.Q<Slider>("FanIntensity"), "value", model.Fan, nameof(SettingsViewModel.Fan.Intensity));
    }

    private void Bind<T>(T element, string binding, object source, string name) where T : VisualElement
    {
      element.dataSource = source;
      element.SetBinding(binding, new DataBinding
      {
        bindingMode = BindingMode.TwoWay,
        updateTrigger = BindingUpdateTrigger.EveryUpdate,
        dataSource = source,
        dataSourcePath = PropertyPath.FromName(name)
      });
    }

    protected void Update()
    {
    }
  }
}