using Cysharp.Threading.Tasks;
using LuciReality.OpenBCI;
using Unity.AppUI.Core;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using Unity.Logging;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  public partial class SystemCalibration : VisualElement, INavigationScreen
  {
    public const string className = "luci-reality--system-calibration";

    private Unity.AppUI.UI.Button CalibrateSystemButton => this.Q<Unity.AppUI.UI.Button>("calibrate-system-button");

    private Text LeftLrsText => this.Q<Text>("result-left-detected");
    private Text LeftCountText => this.Q<Text>("result-left-count");
    private Text RightLrsText => this.Q<Text>("result-right-detected");
    private Text RightCountText => this.Q<Text>("result-right-count");
    private Text MinAmplitudeText => this.Q<Text>("result-min-amplitude");
    private Text MaxAmplitudeText => this.Q<Text>("result-max-amplitude");
    private InputLabel ThresholdLabel => this.Q<InputLabel>("threshold-label");
    private Stepper ThresholdStepper => this.Q<Stepper>("threshold-stepper");
    private VisualElement CalibrationProgress => this.Q<VisualElement>("calibration-progress");
    private VisualElement ConnectionProgress => this.Q<VisualElement>("connection-progress");
    private readonly CalibrationViewModel viewModel;

    public SystemCalibration(CalibrationViewModel viewModel)
    {
      this.viewModel = viewModel;

      AddToClassList(className);

      var asset = Resources.Load<VisualTreeAsset>("SystemCalibration");
      asset.CloneTree(this);

      CalibrateSystemButton.clicked +=
        async () => await viewModel.StartCalibrationAsyncCommand.ExecuteAsync(Application.exitCancellationToken);

      Bind();
    }

    private void Bind()
    {
      ThresholdStepper.RegisterValueChangedCallback((e) =>
      {
        if (e.newValue == e.previousValue) { return; }
        if (e.newValue == 1)
        {
          viewModel.IncrementThresholdAsyncCommand.ExecuteAsync(Application.exitCancellationToken);
        }
        if (e.newValue == -1)
        {
          viewModel.IncrementThresholdAsyncCommand.Execute(Application.exitCancellationToken);
        }
      });

      dataSource = viewModel;
      LeftLrsText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.LeftLRMessage))
      });
      LeftCountText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.LeftCountMessage))
      });
      RightLrsText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.RightLRMessage))
      });
      RightCountText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.RightCountMessage))
      });
      MinAmplitudeText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.MinAmplitudeMessage))
      });
      MaxAmplitudeText.SetBinding(nameof(Text.text), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.MaxAmplitudeMessage))
      });
      ThresholdLabel.SetBinding(nameof(InputLabel.label), new DataBinding
      {
        bindingMode = BindingMode.ToTarget,
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.ThresholdMessage))
      });
      var calibrationProgressBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.IsCalibrating)),
        bindingMode = BindingMode.ToTarget,
      };
      calibrationProgressBinding.sourceToUiConverters.AddConverter(
        (ref bool isCalibrating) => (StyleEnum<DisplayStyle>)(isCalibrating ? DisplayStyle.Flex : DisplayStyle.None)
      );
      CalibrationProgress.SetBinding($"{nameof(CalibrationProgress.style)}.{nameof(CalibrationProgress.style.display)}", calibrationProgressBinding);
      var connectionProgressBinding = new DataBinding
      {
        dataSourcePath = PropertyPath.FromName(nameof(CalibrationViewModel.IsConnected)),
        bindingMode = BindingMode.ToTarget,
      };
      connectionProgressBinding.sourceToUiConverters.AddConverter(
        (ref bool isCalibrating) => (StyleEnum<DisplayStyle>)(!isCalibrating ? DisplayStyle.Flex : DisplayStyle.None)
      );
      ConnectionProgress.SetBinding($"{nameof(ConnectionProgress.style)}.{nameof(ConnectionProgress.style.display)}", connectionProgressBinding);
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