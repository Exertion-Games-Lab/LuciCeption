using LuciReality.AI;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Logging;
using System.Threading.Tasks;

namespace LuciReality.UI
{
  public class UiRefactor : MonoBehaviour
  {
    private UIDocument document;
    private VisualElement root;

    public CueParametersViewModel cueParametersViewModel;
    public PromptViewModel promptViewModel;

    public CueParametersModel model;

    public Texture2D tex;
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
      var submitButton = promptPane.Q<ActionButton>("prompt-submit-button");

      submitButton.clicked += async () => {
        if (isGenerating)
        {
          return;
        }

        Log.Info("[{logType}]: The user clicked submit prompt", "USER_INTERFACE");
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
      };

      var generateButton = promptPane.Q<ActionButton>("image-generate-button");
      generateButton.clicked += async () => {
        if (isGenerating)
        {
          return;
        }

        Log.Info("[{logType}]: The user clicked generate image", "USER_INTERFACE");
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
      };
    }

    private async Task QueryModel() {
      cueParametersViewModel.Model = await TextQuery.Query(promptViewModel.Text);
      var visualCueColor = root.Q<Unity.AppUI.UI.ColorPicker>("visual-cue-colour");
      visualCueColor.value = cueParametersViewModel.Model.Lights.Colour;
    }

    private async Task QueryImage()
    {
      await ImageQuery.Query(promptViewModel.Text, tex);
      VisualElement image = root.Q("generated-image-box");
      image.style.backgroundImage = tex;
    }

    private void Bind()
    {
      var promptPane = root.Query<Pane>().First();
      promptPane.dataSource = promptViewModel;
      var promptTextArea = promptPane.Q<TextArea>("prompt-text");
      promptTextArea.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(promptViewModel.Text))
      });

      var cuePane = root.Query<Pane>().AtIndex(1);
      cuePane.dataSource = cueParametersViewModel;
      var visualCueIntensity = cuePane.Q<SliderFloat>("visual-cue-intensity");
      visualCueIntensity.SetBinding("value", new DataBinding {
        dataSourcePath = PropertyPath.FromName(nameof(cueParametersViewModel.LightsIntensity))
      });

      var visualCueColor = cuePane.Q<Unity.AppUI.UI.ColorPicker>("visual-cue-colour");

      visualCueColor.RegisterCallback<ChangeEvent<Color>>((evt) =>
      {
          cueParametersViewModel.LightsColour = evt.newValue;
      });

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