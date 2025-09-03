using System;
using LuciReality.OpenBCI;

namespace LuciReality.Events
{
  public class EventBus
  {
    public event Action MovementDetected;
    public event Action LrSignalDetected;
    public event Action<CalibrationResult> Calibrated;
    private readonly OpenBCIClient openBCIClient;

    public EventBus(OpenBCIClient openBCIClient)
    {
      this.openBCIClient = openBCIClient;

      openBCIClient.Calibrated += result =>
      {
        Calibrated?.Invoke(result);
      };

      openBCIClient.LrSignalDetected += () =>
      {
        LrSignalDetected?.Invoke();
      };
    }
  }
}