using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System;

namespace LuciReality.EMS
{
  public enum ElectrodeChannel
  {
    A,
    B,
    C,
    D,
  };

  public class EmsController : MonoBehaviour
  {
    public string DeviceName = "Paizo";
    public string DeviceService = "19B10000-E8F2-537E-4F6C-D104768A1214";
    public string DeviceCharacteristic = "19B10001-E8F2-537E-4F6C-D104768A1214";
    private BluetoothScanner scanner;
    private BluetoothDevice device;


    protected void Awake()
    {
      scanner = FindFirstObjectByType<BluetoothScanner>();
    }

    protected async void Start()
    {
      await TestDevice();
    }

    public async Awaitable TestDevice()
    {
      var device = (await scanner.ScanForDevices())
        .Find(x => x.Name == DeviceName);
      
      if (device == null)
      {
        Debug.LogError("Could not find device!");
        return;
      }
      else
      {
        Debug.Log("Device found");
      }

      // bool success = await device.Subscribe("19B10000-E8F2-537E-4F6C-D104768A1214", "19B10001-E8F2-537E-4F6C-D104768A1214");
      // CheckError(success);

      // var onMsg = Encoding.ASCII.GetBytes("" + 'A' + '\n');
      // var offMsg = Encoding.ASCII.GetBytes("" + 'a' + '\n');
      // var disconnectMsg = Encoding.ASCII.GetBytes("" + 'r' + '\n');
      // success = await device.SendData(onMsg, (short)onMsg.Length, DeviceService, DeviceCharacteristic);
      // CheckError(success);

      // await Awaitable.WaitForSecondsAsync(5); // on for 5 seconds
      // success = await device.SendData(offMsg, (short)offMsg.Length, DeviceService, DeviceCharacteristic);
      // CheckError(success);

      // await Awaitable.WaitForSecondsAsync(1);
      // success = await device.SendData(disconnectMsg, (short)disconnectMsg.Length, DeviceService, DeviceCharacteristic);
      // CheckError(success);
    }

    public async Task SetChannel(ElectrodeChannel channel, bool toggle)
    {
      char character = 'r';
      switch (channel)
      {
        case ElectrodeChannel.A:
          character = 'a';
          break;
        case ElectrodeChannel.B:
          character = 'b';
          break;
        case ElectrodeChannel.C:
          character = 'c';
          break;
        case ElectrodeChannel.D:
          character = 'd';
          break;
      }

      if (toggle)
      {
        character = char.ToUpper(character);
      }

      byte[] message = Encoding.ASCII.GetBytes("" + character);
      await device.SendDataWithResponse(message, (short)(sizeof(byte) * message.Length), DeviceService, DeviceCharacteristic);
    }

    private void CheckError(bool success)
    {
      if (!success)
      {
        BleApi.ErrorMessage buf = new();
        BleApi.GetError(out buf);
        Debug.LogError(buf.msg);
      }
    }
  }
}