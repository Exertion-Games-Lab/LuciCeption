using System;
using Unity.AppUI.Core;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [UxmlElement]
  public partial class NavMenu : VisualElement, INavigationScreen
  {
    public const string className = "luci-reality--fullscreen";
    public Panel RootPanel;
    public VisualElement NavContainer;
    public Unity.AppUI.UI.Button SettingsWindowLink => this.Q<Unity.AppUI.UI.Button>("settings-window-link");
    public Unity.AppUI.UI.Button CalibrationWindowLink => this.Q<Unity.AppUI.UI.Button>("calibration-window-link");
    public Unity.AppUI.UI.Button ModulesWindowLink => this.Q<Unity.AppUI.UI.Button>("modules-window-link");

    private Action SettingsWindowLinkOnClick;
    private Action CalibrationWindowLinkOnClick;
    private Action ModulesWindowLinkOnClick;

    public NavMenu()
    {
      AddToClassList(className);

      var asset = Resources.Load<VisualTreeAsset>("NavMenu");
      asset.CloneTree(this);
    }

    public void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
      
      SettingsWindowLinkOnClick = () => controller.Navigate(Actions.settings_link); // This is the navigation destination
      CalibrationWindowLinkOnClick = () => controller.Navigate(Actions.calibration_link);
      ModulesWindowLinkOnClick = () => controller.Navigate("ModulesWindow");

      SettingsWindowLink.clicked += SettingsWindowLinkOnClick; // Now we have created a closure to hold `controller` we assign this as a callback.
      CalibrationWindowLink.clicked += CalibrationWindowLinkOnClick;
      ModulesWindowLink.clicked += ModulesWindowLinkOnClick;
    }

    public void OnExit(NavController controller, NavDestination destination, Argument[] args)
    {
      // We remove these here to prevent leaking memory.
      SettingsWindowLink.clicked -= SettingsWindowLinkOnClick;
      CalibrationWindowLink.clicked -= CalibrationWindowLinkOnClick;
      ModulesWindowLink.clicked -= ModulesWindowLinkOnClick;
    }
  }
}