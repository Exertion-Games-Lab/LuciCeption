using LuciReality.AI;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Logging;
using System.Threading.Tasks;
using UnityEditor;
using Unity.AppUI.Core;

namespace LuciReality.UI
{
  public class UiRefactor : MonoBehaviour
  {
    private UIDocument document;
    private VisualElement root;

    public CueParametersViewModel cueParametersViewModel;
    public PromptViewModel promptViewModel;

    public CueParametersModel model;

    private Texture2D tex;
    public bool isGenerating = false;

    protected void Awake()
    {
      tex = new Texture2D(2, 2)
      {
        hideFlags = HideFlags.DontSave
      };

      cueParametersViewModel = new();
      promptViewModel = new();
      model = cueParametersViewModel.Model = new CueParametersModel();

      document = GetComponent<UIDocument>();      
    }

    protected async void Start()
    {
      root = document.rootVisualElement;
      SetupActions();
      Bind();
    }

    private void SetupActions()
    {
      var promptPane = root.Query<Pane>().First();
      var imageFromPrompt = promptPane.Q<ActionButton>("image-from-prompt-button");

      imageFromPrompt.clicked += async () => {
        if (cueParametersViewModel.GeneratingImage) { return; }

        Log.Info("[{logType}]: The user clicked generate image", "USER_INTERFACE");
        cueParametersViewModel.GeneratingImage = true;
        try { await QueryImage(); }
        finally { cueParametersViewModel.GeneratingImage = false; }
      };

      var cuesFromPrompt = promptPane.Q<ActionButton>("cues-from-prompt-button");
      cuesFromPrompt.clicked += async () => {
        if (cueParametersViewModel.GeneratingCues)
        {
          return;
        }

        Log.Info("[{logType}]: The user clicked generate cues", "USER_INTERFACE");

        cueParametersViewModel.GeneratingCues = true;
        try { await QueryModel(); }
        finally { cueParametersViewModel.GeneratingCues = false; }
      };

      var dreamPromptInfo = root.Q<IconButton>("dream-prompt-info");
      dreamPromptInfo.clicked += () => {
        var popoverContent = new VisualElement();
        var infoText = new Text
        {
          text = "This section generates an image using AI.\nDescribe what you would like to dream about in the text area, and clicking the button will prompt the AI to generate an image based on it."
        };
        popoverContent.Add(infoText);

        Popover.Build(dreamPromptInfo, popoverContent)
        .SetKeyboardDismiss(true)
        .SetContainerPadding(4)
        .SetOutsideClickDismiss(true)
        .SetArrowVisible(true)
        .SetOffset(2)
        .Show();
      };

      var dreamImageInfo = root.Q<IconButton>("dream-image-info");
      dreamImageInfo.clicked += () =>
      {
        var popoverContent = new VisualElement();
        var infoText = new Text
        {
          text = "Once you are happy with the image that the AI generated, click the generate cues button.\nThis will configure the cues using your text prompt and selected image.\nAfter the cues have been generated, you are free to customise them as you wish."
        };
        popoverContent.Add(infoText);

        Popover.Build(dreamImageInfo, popoverContent)
        .SetKeyboardDismiss(true)
        .SetContainerPadding(4)
        .SetOutsideClickDismiss(true)
        .SetArrowVisible(true)
        .SetOffset(2)
        .Show();
      };

      var viewReasoningButton = root.Q<Unity.AppUI.UI.Button>();
      viewReasoningButton.clicked += () =>
      {
        var reasoningWindow = new ReasoningWindow();
        reasoningWindow.dataSource = cueParametersViewModel;
        reasoningWindow.SetBinding("text", new DataBinding
        {
          dataSourcePath = PropertyPath.FromName(nameof(CueParametersViewModel.ReasoningText))
        });
        var modal = Modal.Build(viewReasoningButton, reasoningWindow)
        .SetKeyboardDismiss(false)
        .SetOutsideClickDismiss(true);

        reasoningWindow.OnCloseClicked += () => modal.Dismiss();

        modal.Show();
      };

      var audioClipSelector = root.Q<Unity.AppUI.UI.Button>("audio-clip-selector");
      audioClipSelector.AddManipulator(new Unity.AppUI.UI.Clickable(evt =>
      {
        var fileBrowser = new FileBrowser();
        var modal = Modal.Build(audioClipSelector, fileBrowser)
        .SetKeyboardDismiss(false)
        .SetOutsideClickDismiss(true);

        fileBrowser.OnCloseClicked += () => modal.Dismiss();

        modal.Show();
      }));
    }

    private async Task QueryModel() => cueParametersViewModel.Model = await TextQuery.Query(cueParametersViewModel.PromptText);

    private async Task QueryImage()
    {
      await ImageQuery.Query(cueParametersViewModel.PromptText, tex);
      VisualElement image = root.Q("generated-image-box");
      image.style.backgroundImage = tex;
    }

    private CueParametersViewModel GetViewModel() => cueParametersViewModel;

    private void Bind()
    {
      var promptPane = root.Query<Pane>().First();
      promptPane.dataSource = cueParametersViewModel;
      var promptTextArea = promptPane.Q<TextArea>("prompt-text");
      promptTextArea.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.PromptText))
      });

      var imageProgressIndicator = promptPane.Q("image-progress-indicator");
      var imageProgressIndicatorBinding = new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingImage)),
        bindingMode = BindingMode.ToTarget,
      };
      imageProgressIndicatorBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => (StyleEnum<DisplayStyle>)(isGenerating ?  DisplayStyle.Flex : DisplayStyle.None));
      imageProgressIndicator.SetBinding("style." + nameof(imageProgressIndicator.style.display), imageProgressIndicatorBinding);

      var imageFromPrompt = promptPane.Q<ActionButton>("image-from-prompt-button");
      var imageFromPromptBinding = new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingImage)),
        bindingMode = BindingMode.ToTarget,
      };
      imageFromPromptBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => !isGenerating);
      imageFromPrompt.SetBinding("enabledSelf", imageFromPromptBinding);
      var cuesFromPrompt = promptPane.Q<ActionButton>("cues-from-prompt-button");
      var cuesFromPromptBinding = new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingCues)),
        bindingMode = BindingMode.ToTarget,
      };
      cuesFromPromptBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => !isGenerating);
      cuesFromPrompt.SetBinding("enabledSelf", cuesFromPromptBinding);

      var cuePane = root.Query<Pane>().AtIndex(1);
      cuePane.dataSource = cueParametersViewModel;
      var visualCueIntensity = cuePane.Q<SliderFloat>("visual-cue-intensity");
      visualCueIntensity.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LightsIntensity))
      });

      // There appears to be a bug with AppUI's ColorPicker.
      // It succesfully invokes change events but fails to notify the runtime binding system when its value changes.
      // Therefore, we replicate the binding behaviour with a work-around.

      var visualCueColor = cuePane.Q<ColorPicker>("visual-cue-colour");

      var cueProgressIndicator = cuePane.Q("cue-progress-indicator");
      var cueProgressIndicatorBinding = new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.GeneratingCues)),
        bindingMode = BindingMode.ToTarget,
      };
      cueProgressIndicatorBinding.sourceToUiConverters.AddConverter((ref bool isGenerating) => (StyleEnum<DisplayStyle>)(isGenerating ?  DisplayStyle.Flex : DisplayStyle.None));
      cueProgressIndicator.SetBinding("style." + nameof(cueProgressIndicator.style.display), cueProgressIndicatorBinding);

      visualCueColor.RegisterCallback<ChangeEvent<Color>>((evt) => GetViewModel().SetLightsColourWithoutNotify(evt.newValue));
      cueParametersViewModel.PropertyChanged += (v, evt) => {
        if (evt.PropertyName != nameof(CueParametersViewModel.LightsColour)) { return; }
        if (v is not CueParametersViewModel viewModel) { return; }
        if (visualCueColor.value == viewModel.LightsColour) { return; }

        visualCueColor.SetValueWithoutNotify(viewModel.LightsColour);
      };

      var visualCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("visual-cue-enabled");
      visualCueEnabled.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LightsEnabled))
      });

      var audioCueVolume = cuePane.Q<SliderFloat>("audio-cue-volume");
      audioCueVolume.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.AudioVolume))
      });

      var audioCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("audio-cue-enabled");
      audioCueEnabled.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.AudioEnabled))
      });

      var lraCueIntensity = cuePane.Q<SliderFloat>("lra-cue-intensity");
      lraCueIntensity.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LraIntensity))
      });

      var lraCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("lra-cue-enabled");
      lraCueEnabled.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LraEnabled))
      });

      var emsCueIntensity = cuePane.Q<SliderFloat>("ems-cue-intensity");
      emsCueIntensity.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.EmsIntensity))
      });

      var emsCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("ems-cue-enabled");
      emsCueEnabled.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.EmsEnabled))
      });

      var fanCueIntensity = cuePane.Q<SliderFloat>("fan-cue-intensity");
      fanCueIntensity.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.FanIntensity))
      });

      var fanCueEnabled = cuePane.Q<Unity.AppUI.UI.Toggle>("fan-cue-enabled");
      fanCueEnabled.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.FanEnabled))
      });
    }
  }
}