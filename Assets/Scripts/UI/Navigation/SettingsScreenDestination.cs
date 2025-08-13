using System;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [Serializable]
  public class SettingsWindowDestination : NavDestinationTemplate
  {
    public override INavigationScreen CreateScreen(NavHost host)
    {
      return LuciRealityApp.current.services.GetRequiredService<SettingsWindow>();
    }
  }
}