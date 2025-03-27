using System;
using System.Threading.Tasks;

namespace LuciReality.EMS
{
  public class BluetoothDevice
  {
    public string Uuid;
    public string Name;

    public override int GetHashCode()
    {
      return HashCode.Combine(Uuid, Name);
    }

    /// <summary>
    /// Subscribe to a characteristic. Read the slides in the documentation if you would like to know more about BLE.
    /// </summary>
    /// <param name="serviceId">The uuid of the service the characteristic is associated with.</param>
    /// <param name="characteristicId">The uuid for the characteristic.</param>
    /// <returns></returns>
    public async Task<bool> Subscribe(string serviceId, string characteristicId) => await Task.Run(() => BleApi.SubscribeCharacteristic(Uuid, serviceId, characteristicId, true));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buf">A buffer containing data to send to the device.</param>
    /// <param name="size">The number of bytes that the buffer contains.</param>
    /// <param name="serviceId">The uuid of the service the characteristic is associated with.</param>
    /// <param name="characteristicId">The uuid for the characteristic.</param>
    /// <returns></returns>
    public async Task<bool> SendData(byte[] buf, short size, string serviceUuid, string characteristicUuid) => await Task.Run(() => BleApi.SendData(new BleApi.BLEData {
      buf = buf,
      size = size,
      deviceId = Uuid,
      serviceUuid = serviceUuid,
      characteristicUuid = characteristicUuid
    }, true));
  }
}