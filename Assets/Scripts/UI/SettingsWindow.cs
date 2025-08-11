using LuciReality.AI;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Logging;
using System.Threading.Tasks;
using UnityEditor;
using Unity.AppUI.Core;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Navigation;

namespace LuciReality.UI
{
  [UxmlElement]
  public partial class SettingsWindow : VisualElement, INavigationScreen
  {
    public CueParametersViewModel cueParametersViewModel;
    public PromptViewModel promptViewModel;
    private Texture2D tex;

    private List<string> savedFiles;

    public SettingsWindow()
    {
      tex = new Texture2D(2, 2)
      {
        hideFlags = HideFlags.DontSave
      };

      cueParametersViewModel = new()
      {
        Model = new CueParametersModel()
      };

      promptViewModel = new()
      {
        Model = new PromptModel()
      };

      var asset = Resources.Load<VisualTreeAsset>("SettingsWindow");
      asset.CloneTree(this);

      SetupActions();
      Bind();
    }

    private void SetupActions()
    {
      var promptPane = this.Query<Pane>().First();
      var imageFromPrompt = promptPane.Q<ActionButton>("image-from-prompt-button");
      imageFromPrompt.clicked += async () =>
      {
        if (promptViewModel.GeneratingImage) { return; }

        Log.Info("[{logType}]: The user clicked generate image", "USER_INTERFACE");
        promptViewModel.GeneratingImage = true;
        try { await QueryImage(); }
        finally { promptViewModel.GeneratingImage = false; }
      };

      var cuesFromPrompt = promptPane.Q<ActionButton>("cues-from-prompt-button");
      cuesFromPrompt.clicked += async () =>
      {
        if (cueParametersViewModel.GeneratingCues)
        {
          return;
        }

        Log.Info("[{logType}]: The user clicked generate cues", "USER_INTERFACE");

        cueParametersViewModel.GeneratingCues = true;
        try { await QueryModel(); }
        finally { cueParametersViewModel.GeneratingCues = false; }
      };

      // Show prompt section help tooltip
      var dreamPromptInfo = this.Q<IconButton>("dream-prompt-info");
      dreamPromptInfo.clicked += () =>
        cueParametersViewModel.OpenPromptHelpCommand.Execute(dreamPromptInfo);

      // Show image section help tooltip
      var dreamImageInfo = this.Q<IconButton>("dream-image-info");
      dreamImageInfo.clicked += () =>
        cueParametersViewModel.OpenImageHelpCommand.Execute(dreamImageInfo);

      // Open reasoning window
      var viewReasoningButton = this.Q<Unity.AppUI.UI.Button>();
      viewReasoningButton.clicked += () =>
        cueParametersViewModel.OpenReasoningCommand.Execute(viewReasoningButton);

      // Open audio clip selector
      var audioClipSelector = this.Q<Unity.AppUI.UI.Button>("audio-clip-selector");
      audioClipSelector.AddManipulator(new Unity.AppUI.UI.Clickable(evt =>
        cueParametersViewModel.OpenAudioClipBrowserCommand.Execute(audioClipSelector)
      ));

      var saveButton = this.Q<Unity.AppUI.UI.Button>("save-parameters-button");
      var saveFilename = this.Q<Unity.AppUI.UI.TextField>("save-parameters-filename");

      saveFilename.validateValue += (value) =>
      {
        if (value is null) { return true; }

        var valid = value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        saveButton.enabledSelf = valid;
        return valid;
      };

      // TODO: Confirmation dialog on load + delete.
      // TODO: Confirmation dialog on overwrite.
      // TODO: Notification with undo button.

      var loadButton = this.Q<Unity.AppUI.UI.Button>("load-parameters-button");
      var deleteButton = this.Q<Unity.AppUI.UI.Button>("delete-parameters-button");
      var loadDropdown = this.Q<Dropdown>("load-parameters-dropdown");

      deleteButton.clicked += () =>
      {
        var name = GetSelectedSave(loadDropdown);
        if (name is null) { return; }
        Log.Info("[{logType}]: The user deleted save {saveName}", "USER_INTERFACE", name);
        SaveSettings.Delete(name);

        savedFiles = GetSavedFiles();
        loadDropdown.sourceItems = savedFiles;
        loadDropdown.Refresh();

      };

      loadButton.clicked += async () =>
      {
        string name = GetSelectedSave(loadDropdown);
        if (name is null) { return; }
        Log.Info("[{logType}]: The user loaded {saveName}", "USER_INTERFACE", name);
        var (cues, prompts) = await SaveSettings.Load(name);
        cueParametersViewModel.Model = cues;
        promptViewModel.Model = prompts;
      };

      savedFiles = GetSavedFiles();
      loadDropdown.sourceItems = savedFiles;
      loadDropdown.bindItem = (item, i) => item.label = savedFiles[i];


      saveButton.clicked += async () =>
      {
        var name = saveFilename.value;
        if (name == null) { return; }
        Log.Info("[{logType}]: The user saved to {saveName}", "USER_INTERFACE", name);
        await SaveSettings.Save(name, cueParametersViewModel.Model, promptViewModel.Model);

        savedFiles = GetSavedFiles();
        foreach (var fileName in savedFiles)
        {
          Debug.Log(fileName);
        }
        loadDropdown.sourceItems = savedFiles;
        loadDropdown.Refresh();
      };


      AddSliderInteractions();
    }

    private string GetSelectedSave(Dropdown dropdown)
    {
      var selectedList = dropdown.value.ToList();

      if (selectedList.Count != 1)
      {
        return null;
      }

      var name = savedFiles[selectedList[0]];
      return name;
    }

    private List<string> GetSavedFiles()
    {
      var path = Path.Combine(Application.persistentDataPath, "saved-cues");

      if (!Directory.Exists(path))
      {
        Directory.CreateDirectory(path);
      }

      var files = new List<string>();

      foreach (var file in Directory.GetFiles(path))
      {
        files.Add(Path.GetFileNameWithoutExtension(file));
      }

      return files;
    }

    private void AddSliderInteractions()
    {
      // TODO: This would be a lot cleaner either using currying or creating the closures ourselves.
      // TODO: These log messages will also appear when Gemini changes the interface, however we can identify this through the timestamps.
      // Ideally this would be fixed though.

      var visualCueIntensity = this.Q<SliderFloat>("visual-cue-intensity");
      var visualCueEnabled = this.Q<Unity.AppUI.UI.Toggle>("visual-cue-enabled");
      visualCueIntensity.RegisterValueChangedCallback((evt) =>
      {
        UpdateToggleForSlider(evt, visualCueEnabled);
        Log.Info("[{logType}]: The user set {sliderName} from {previousValue} to {newValue}", "USER_INTERFACE", visualCueIntensity.name, evt.previousValue, evt.newValue);
      });
      visualCueEnabled.RegisterValueChangedCallback((evt) =>
      {
        Log.Info("[{logType}]: The user set {toggleName} from {previousValue} to {newValue}", "USER_INTERFACE", visualCueEnabled.name, evt.previousValue, evt.newValue);
      });

      var audioCueVolume = this.Q<SliderFloat>("audio-cue-volume");
      var audioCueEnabled = this.Q<Unity.AppUI.UI.Toggle>("audio-cue-enabled");
      audioCueVolume.RegisterValueChangedCallback((evt) =>
      {
        UpdateToggleForSlider(evt, audioCueEnabled);
        Log.Info("[{logType}]: The user set {sliderName} from {previousValue} to {newValue}", "USER_INTERFACE", audioCueVolume.name, evt.previousValue, evt.newValue);
      });
      audioCueEnabled.RegisterValueChangedCallback((evt) =>
      {
        Log.Info("[{logType}]: The user set {toggleName} from {previousValue} to {newValue}", "USER_INTERFACE", audioCueEnabled.name, evt.previousValue, evt.newValue);
      });

      var lraCueIntensity = this.Q<SliderFloat>("lra-cue-intensity");
      var lraCueEnabled = this.Q<Unity.AppUI.UI.Toggle>("lra-cue-enabled");
      lraCueIntensity.RegisterValueChangedCallback((evt) =>
      {
        UpdateToggleForSlider(evt, lraCueEnabled);
        Log.Info("[{logType}]: The user set {sliderName} from {previousValue} to {newValue}", "USER_INTERFACE", lraCueIntensity.name, evt.previousValue, evt.newValue);
      });
      lraCueEnabled.RegisterValueChangedCallback((evt) =>
      {
        Log.Info("[{logType}]: The user set {toggleName} from {previousValue} to {newValue}", "USER_INTERFACE", lraCueEnabled.name, evt.previousValue, evt.newValue);
      });

      var emsCueIntensity = this.Q<SliderFloat>("ems-cue-intensity");
      var emsCueEnabled = this.Q<Unity.AppUI.UI.Toggle>("ems-cue-enabled");
      emsCueIntensity.RegisterValueChangedCallback((evt) =>
      {
        UpdateToggleForSlider(evt, emsCueEnabled);
        Log.Info("[{logType}]: The user set {sliderName} from {previousValue} to {newValue}", "USER_INTERFACE", emsCueIntensity.name, evt.previousValue, evt.newValue);
      });
      emsCueEnabled.RegisterValueChangedCallback((evt) =>
      {
        Log.Info("[{logType}]: The user set {toggleName} from {previousValue} to {newValue}", "USER_INTERFACE", emsCueEnabled.name, evt.previousValue, evt.newValue);
      });

      var fanCueIntensity = this.Q<SliderFloat>("fan-cue-intensity");
      var fanCueEnabled = this.Q<Unity.AppUI.UI.Toggle>("fan-cue-enabled");
      fanCueIntensity.RegisterValueChangedCallback((evt) =>
      {
        UpdateToggleForSlider(evt, fanCueEnabled);
        Log.Info("[{logType}]: The user set {sliderName} from {previousValue} to {newValue}", "USER_INTERFACE", fanCueIntensity.name, evt.previousValue, evt.newValue);
      });
      fanCueEnabled.RegisterValueChangedCallback((evt) =>
      {
        Log.Info("[{logType}]: The user set {toggleName} from {previousValue} to {newValue}", "USER_INTERFACE", fanCueEnabled.name, evt.previousValue, evt.newValue);
      });
    }

    private static void UpdateToggleForSlider(ChangeEvent<float> evt, Unity.AppUI.UI.Toggle toggle)
    {
      if (evt.newValue == 0f && evt.previousValue != 0f && toggle.value)
      {
        toggle.SetValueWithoutNotify(false);
      }
      if (evt.newValue != 0f && evt.previousValue == 0f && !toggle.value)
      {
        toggle.SetValueWithoutNotify(true);
      }
    }

    private async Task QueryModel() => cueParametersViewModel.Model = await TextQuery.Query(promptViewModel.Text);

    private async Task QueryImage()
    {
      await ImageQuery.Query(promptViewModel.Text, tex);
      promptViewModel.Image = tex;
    }

    private CueParametersViewModel GetViewModel() => cueParametersViewModel;

    private void Bind()
    {
      var promptPane = this.Query<Pane>().First();
      promptPane.dataSource = promptViewModel;

      var promptTextArea = promptPane.Q<TextArea>("prompt-text");
      promptTextArea.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(promptViewModel.Text))
      });

      var promptImage = promptPane.Q<VisualElement>("generated-image-box");
      promptImage.SetBinding($"{nameof(promptImage.style)}.{nameof(promptImage.style.backgroundImage)}", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(promptViewModel.Image)),
        bindingMode = BindingMode.ToTarget,
      });

      var imageProgressIndicator = promptPane.Q("image-progress-indicator");
      var imageProgressIndicatorBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(promptViewModel.GeneratingImage)),
        bindingMode = BindingMode.ToTarget,
      };
      imageProgressIndicatorBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => (StyleEnum<DisplayStyle>)(isGenerating ? DisplayStyle.Flex : DisplayStyle.None));
      imageProgressIndicator.SetBinding("style." + nameof(imageProgressIndicator.style.display), imageProgressIndicatorBinding);

      var imageFromPrompt = promptPane.Q<ActionButton>("image-from-prompt-button");
      var imageFromPromptBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(promptViewModel.GeneratingImage)),
        bindingMode = BindingMode.ToTarget,
      };
      imageFromPromptBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => !isGenerating);
      imageFromPrompt.SetBinding("enabledSelf", imageFromPromptBinding);
      var cuesFromPrompt = promptPane.Q<ActionButton>("cues-from-prompt-button");
      cuesFromPrompt.dataSource = cueParametersViewModel;
      var cuesFromPromptBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingCues)),
        bindingMode = BindingMode.ToTarget,
      };
      cuesFromPromptBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => !isGenerating);
      cuesFromPrompt.SetBinding("enabledSelf", cuesFromPromptBinding);

      var cuePane = this.Query<Pane>().AtIndex(1);
      cuePane.dataSource = cueParametersViewModel;
      var visualCueIntensity = cuePane.Q<SliderFloat>("visual-cue-intensity");
      visualCueIntensity.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LightsIntensity))
      });

      // There appears to be a bug with AppUI's ColorPicker.
      // It succesfully invokes change events but fails to notify the runtime binding system when its value changes.
      // Therefore, we replicate the binding behaviour with a work-around.

      var visualCueColor = cuePane.Q<ColorPicker>("visual-cue-colour");

      var cueProgressIndicator = cuePane.Q("cue-progress-indicator");
      var cueProgressIndicatorBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingCues)),
        bindingMode = BindingMode.ToTarget,
      };
      cueProgressIndicatorBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => (StyleEnum<DisplayStyle>)(isGenerating ? DisplayStyle.Flex : DisplayStyle.None));
      cueProgressIndicator.SetBinding("style." + nameof(cueProgressIndicator.style.display), cueProgressIndicatorBinding);

      visualCueColor.RegisterCallback<ChangeEvent<Color>>((evt) => GetViewModel().SetLightsColourWithoutNotify(evt.newValue));
      cueParametersViewModel.PropertyChanged += (v, evt) =>
      {
        if (evt.PropertyName != nameof(CueParametersViewModel.LightsColour)) { return; }
        if (v is not CueParametersViewModel viewModel) { return; }
        if (visualCueColor.value == viewModel.LightsColour) { return; }

        visualCueColor.SetValueWithoutNotify(viewModel.LightsColour);
      };

      var visualCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("visual-cue-enabled");
      visualCueEnabled.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LightsEnabled))
      });

      var audioCueVolume = cuePane.Q<SliderFloat>("audio-cue-volume");
      audioCueVolume.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.AudioVolume))
      });

      var audioCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("audio-cue-enabled");
      audioCueEnabled.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.AudioEnabled))
      });

      var lraCueIntensity = cuePane.Q<SliderFloat>("lra-cue-intensity");
      lraCueIntensity.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LraIntensity))
      });

      var lraCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("lra-cue-enabled");
      lraCueEnabled.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LraEnabled))
      });

      var emsCueIntensity = cuePane.Q<SliderFloat>("ems-cue-intensity");
      emsCueIntensity.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.EmsIntensity))
      });

      var emsCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("ems-cue-enabled");
      emsCueEnabled.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.EmsEnabled))
      });

      var fanCueIntensity = cuePane.Q<SliderFloat>("fan-cue-intensity");
      fanCueIntensity.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.FanIntensity))
      });

      var fanCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("fan-cue-enabled");
      fanCueEnabled.SetBinding("value", new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.FanEnabled))
      });
    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // throw new System.NotImplementedException();
    }
  }
}