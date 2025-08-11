using System;

namespace LuciReality.OpenBCI
{
  public struct CalibrationResult
  {
    public int leftSignals;
    public int rightSignals;
    public int maxAmplitude;
    public int minAmplitude;

    public static int GetSize()
    {
      return sizeof(int) * 4;
    }

    public static CalibrationResult ParseResult(byte[] message, int start)
    {
      return new CalibrationResult
      {
        leftSignals = BitConverter.ToInt32(message, start + sizeof(int) * 0),
        rightSignals = BitConverter.ToInt32(message, start + sizeof(int) * 1),
        maxAmplitude = BitConverter.ToInt32(message, start + sizeof(int) * 2),
        minAmplitude = BitConverter.ToInt32(message, start + sizeof(int) * 3),
      };
    }
  }
}