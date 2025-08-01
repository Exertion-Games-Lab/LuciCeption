using UnityEngine;

using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Unity.Logging;
using System;


namespace LuciReality.OpenBCI
{
  public enum OpenBciClientState
  {
    CONNECTED,
    DISCONNECTED
  }

  public class OpenBCIClient : MonoBehaviour
  {
    public const int MessageTypeLength = 1;
    public event Action Disconnected;
    public event Action Connected;
    public event Action MessageReceived;
    public event Action<CalibrationResult> Calibrated;

    public static class ObciMessages
    {
      public const int GO_IDLE = 0;
      public const int START_CALIBRATION = 1;
      public const int GO_ACTIVE = 2;
      public const int EXIT = 3;
      public const int CALIBRATION_RESULT = 4;
    }
    
    // Connection Parameters
    [Range(0, 65535), SerializeField]
    private ushort port = 7777;

    [SerializeField]
    private bool loopback = true;

    // Socket Client
    private Socket client;
    private OpenBciClientState currentState = OpenBciClientState.DISCONNECTED;

    // Messages
    readonly byte[] messages = new byte[128];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
      Disconnected += OnDisconnected;
      Connected += OnConnected;

      Task.Run(Connect);
    }
    private async Task Connect()
    {
      if (client is not null)
      {
        client.Close();
        client.Dispose();
      }

      // Loop until we successfully (re-)connect to the server.
      while (currentState != OpenBciClientState.CONNECTED)
      {
        client = new Socket(SocketType.Stream, ProtocolType.Tcp);
        Log.Debug("[OpenBCI] Connecting...");

        Task timeout = Task.Run(async () =>
        {
          await Task.Delay(2000);
          if (client.Connected) { return; }
          client.Close();
          client.Dispose();
        });

        try { client.Connect(IPAddress.Loopback, port); }
        catch { }

        await timeout; // Ensure we wait for the timeout before attempting another connection.

        if (client.Connected)
        {
          Log.Info("[OpenBCI] Connected to OpenBCI server.");
          currentState = OpenBciClientState.CONNECTED;
          Connected?.Invoke();
        }
      }

      await ProcessMessageQueue();
    }
    #region Messaging
    private async Task ProcessMessageQueue()
    {
      while (true)
      {
        ClearMessages();
        bool disconnected = await ReceiveMessage();

        if (disconnected)
        {
          if (!Application.exitCancellationToken.IsCancellationRequested)
          {
            Disconnected?.Invoke();
          }
          break;
        }

        for (int i = 0; i < messages.Length; i++)
        {
          int step = ParseMessageAt(i);
          if (step == 0) { break; }
          else { i += step; }
        }
      }
    }
    private async Task<bool> ReceiveMessage()
    {
      bool disconnected = false;

      try { await client.ReceiveAsync(messages, SocketFlags.None); }
      catch { disconnected = true; }

      return disconnected;
    }

    private void ClearMessages()
    {
      for (int i = 0; i < messages.Length; i++)
      {
        messages[i] = 0;
      }
    }

    public async Task StartCalibration() => await SendMessage(ObciMessages.START_CALIBRATION);
    public async Task EndCalibration() => await SendMessage(ObciMessages.GO_IDLE);
    public async Task StartSystem() => await SendMessage(ObciMessages.GO_ACTIVE);
    public async Task StopSystem() => await SendMessage(ObciMessages.GO_IDLE);
    public async Task Exit() => await SendMessage(ObciMessages.EXIT);

    public async Task SendMessage(byte task)
    {
      Log.Debug("[OpenBCI] Sending message to OpenBCI server: {0}", task);
      try
      {
        await client.SendAsync(
          new byte[] { task },
          SocketFlags.None,
          Application.exitCancellationToken
        );
      }
      catch
      {
        if (!Application.exitCancellationToken.IsCancellationRequested)
        {
          Disconnected?.Invoke();
        }
      }
      Log.Debug("[OpenBCI] Successfully sent message to OpenBCI server: {0}", task);
    }
    public int ParseMessageAt(int index)
    {
      byte messageType = messages[index];

      int messageLength;

      switch (messageType)
      {
        case ObciMessages.CALIBRATION_RESULT:
          ReadCalibrationResult(index + 1);
          messageLength = 1 + sizeof(int) * 3;
          break;
        default:
          messageLength = 0;
          break;
      }

      return messageLength;
    }
    #endregion
    public void ReadCalibrationResult(int start)
    {
      CalibrationResult res = new()
      {
        nSignals = BitConverter.ToInt32(messages, start + sizeof(int) * 0),
        maxAmplitude = BitConverter.ToInt32(messages, start + sizeof(int) * 1),
        minAmplitude = BitConverter.ToInt32(messages, start + sizeof(int) * 2),
      };

      Calibrated?.Invoke(res);

      Log.Info("[OpenBCI] Calibration finished with result: nSignals = {0}, maxAmplitude = {1}, minAmplitude = {2}", res.nSignals, res.maxAmplitude, res.minAmplitude);
    }
    #region NetworkEvents
    void OnMessageReceived()
    {

    }
    void OnDisconnected()
    {
      currentState = OpenBciClientState.DISCONNECTED;
      client.Close();
      client.Dispose();
      Log.Info("[OpenBCI] Disconnected from OpenBCI server.");
      Task.Run(Connect);
    }
    void OnConnected()
    {
      Task.Run(StartCalibration);
    }
    #endregion
    void OnDestroy()
    {
      client.Close();
      client.Dispose();
    }
  }
}