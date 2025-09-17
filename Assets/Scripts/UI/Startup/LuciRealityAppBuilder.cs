using LuciReality.OpenBCI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine;

namespace LuciReality.UI
{
  public class NavigationStoreService
  {
    public NavGraphViewAsset GraphAsset;
    public NavigationStoreService()
    {

    }
  }

  public class LuciRealityAppBuilder : UIToolkitAppBuilder<LuciRealityApp>
  {
    [SerializeField] private NavGraphViewAsset graphAsset;

    protected override void OnAppInitialized(LuciRealityApp app)
    {
      base.OnAppInitialized(app);
      var navStore = app.services.GetRequiredService<NavigationStoreService>();
      navStore.GraphAsset = graphAsset;
      app.CreatePage();
      Debug.Log("LuciRealityAppBuilder.OnAppInitialized");
    }

    protected override void OnConfiguringApp(AppBuilder builder)
    {
      base.OnConfiguringApp(builder);
      Debug.Log("LuciRealityAppBuilder.OnConfiguringApp");

      // Add services here
      builder.services.AddSingleton<NavigationStoreService>();
      builder.services.AddSingleton<OpenBCIClient>();

      // Add ViewModels and Views as services
      builder.services.AddSingleton<CueParametersViewModel>();
      builder.services.AddSingleton<PromptViewModel>();
      builder.services.AddSingleton<CalibrationViewModel>();
      builder.services.AddSingleton<CuesActiveViewModel>();

      builder.services.AddSingleton<MainPage>();
      builder.services.AddSingleton<SettingsWindow>();
      builder.services.AddSingleton<SystemCalibration>();
      builder.services.AddSingleton<CuesActive>();

      builder.services.AddSingleton<FileBrowser>();
    }

    protected override void OnAppShuttingDown(LuciRealityApp app)
    {
      base.OnAppShuttingDown(app);
      Debug.Log("LuciRealityAppBuilder.OnAppShuttingDown");
    }
  }
}