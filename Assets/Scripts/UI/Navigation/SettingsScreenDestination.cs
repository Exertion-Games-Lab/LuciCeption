using System;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuciReality.UI
{
  [Serializable]
  public class SettingsWindowDestination : NavDestinationTemplate
  {
    private SettingsWindow window;
    public override INavigationScreen CreateScreen(NavHost host)
    {
      // Instantiate the screen here, or use a factory method if you have a more complex setup
      // (like caching screen per destination).
      return window ??= new SettingsWindow();
    }
  }
}