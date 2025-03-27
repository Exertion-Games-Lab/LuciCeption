using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace LuciReality.EMS
{
  // Facilitates scanning for bluetooth devices using task-based parallelism
  internal class ScanDevicesTask
  {

  }

  public class BluetoothScanner : MonoBehaviour
  {
    protected async void Start()
    {
      var devices = await ScanForDevices(20);

      LogInfo("Devices returned: ");
      foreach (var device in devices)
      {
        LogInfo(device.Uuid);
      }
    }

    /// <summary>
    /// Initiates a scan for near by bluetooth devices.
    /// </summary>
    /// <param name="minLength">The minimum amount of time that the scan should run in seconds.</param>
    /// <returns>A task that on completion returns a list of bluetooth devices.</returns>
    public async Task<List<BluetoothDevice>> ScanForDevices(float minLength = 0)
    {
      LogInfo("Scan for devices initiated.");
      return await Task.Run(() => ScanDevicesTask(minLength));
    }

    private async Task<List<BluetoothDevice>> ScanDevicesTask(float minLength)
    {
      await Awaitable.MainThreadAsync(); // We can't get the time on threads other than the main thread.
      
      BleApi.StartDeviceScan();
      BleApi.ScanStatus scanStatus;

      Dictionary<string, BluetoothDevice> devices = new();

      float startTime = Time.time;
      scanStatus = await PollDevices(minLength, devices, startTime);

      if (scanStatus == BleApi.ScanStatus.FINISHED)
      {
        LogInfo("Finished Bluetooth Device Scan");
      }

      List<BluetoothDevice> listOfDevices = new();
      if (Application.exitCancellationToken.IsCancellationRequested)
      {
        BleApi.StopDeviceScan();
        return listOfDevices;
      }

      foreach (BluetoothDevice device in devices.Values)
      {
        listOfDevices.Add(device);
      }
      BleApi.StopDeviceScan();
      return listOfDevices;
    }

    // Polls devices and adds them to the 'devices' dictionary, which will contain all unique near by bluetooth devices that were found when the scan ends.
    private async Task<BleApi.ScanStatus> PollDevices(float minLength, Dictionary<string, BluetoothDevice> devices, float startTime)
    {
      BleApi.ScanStatus scanStatus;
      BleApi.DeviceUpdate deviceUpdate = new();

      do
      {
        scanStatus = BleApi.PollDevice(ref deviceUpdate, false);

        // If we're in the processing state we can continue work on the next frame.
        if (scanStatus == BleApi.ScanStatus.PROCESSING) { await Awaitable.NextFrameAsync(); continue; } 

        // If we're done and out of time, exit.
        if (scanStatus == BleApi.ScanStatus.FINISHED && !IsTimeRemaining(startTime, minLength)) { break; }
        
        // If we're done but not out of time, restart the scan.
        if (scanStatus == BleApi.ScanStatus.FINISHED && IsTimeRemaining(startTime, minLength)) { BleApi.StopDeviceScan(); BleApi.StartDeviceScan(); }

        if (devices.ContainsKey(deviceUpdate.id)) { continue; }

        var device = new BluetoothDevice { Uuid = deviceUpdate.id, Name = deviceUpdate.name };
        LogInfo($"Found Device: {deviceUpdate.id} {deviceUpdate.name}");
        devices.Add(deviceUpdate.id, device);
      } while ((scanStatus != BleApi.ScanStatus.FINISHED || IsTimeRemaining(startTime, minLength)) && !Application.exitCancellationToken.IsCancellationRequested);
      return scanStatus;
    }

    private bool IsTimeRemaining(float startTime, float minLength) => ElapsedTime(startTime) < minLength;
    private float ElapsedTime(float startTime) => Time.time - startTime;

    private void LogInfo(string msg)
    {
      Debug.Log(msg);
    }
  }
}