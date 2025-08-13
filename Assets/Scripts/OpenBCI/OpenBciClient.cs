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

  /// <summary>
  /// This class represents an IPC connection to the Python EOG Detection server that will be running along side Unity.<br/>
  /// It can be extended to support additional message types by modifying <c>ObciMessages</c> and adding parsing logic for the specific message to <c>ParseMessageAt</c>.
  /// Internally, this class manages a TCP Socket, which it uses to connect to the EOG Detection server.
  /// It will automatically re-connect when disconnected and can prompt the server to begin certain tasks like calibration.
  /// </summary>
  public class OpenBCIClient : MonoBehaviour
  {
    private static OpenBCIClient _instance;
    public static OpenBCIClient Instance
    {
      get => _instance;
      set
      {
        if (_instance == null)
        {
          _instance = value;
        }
      }
    }
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
    private NetworkStream networkStream;
    private OpenBciClientState currentState = OpenBciClientState.DISCONNECTED;

    protected void Start()
    {
      Instance = this;

      Disconnected += OnDisconnected;
      Connected += OnConnected;

      Task.Run(Connect);
    }

    /// <summary>Attempts to connect to the Python instance running the EOG Detector Server.<br/>
    /// Will attempt to re-connect if it disconnects.</summary>
    private async Task Connect()
    {
      // Loop until we successfully (re-)connect to the server.
      while (currentState != OpenBciClientState.CONNECTED)
      {
        // (Re-)create the client so we can connect to the server.
        if (client is not null) { KillClient(); }
        client = new Socket(SocketType.Stream, ProtocolType.Tcp);

        Log.Debug("[OpenBCI] Connecting...");

        Task<bool> timeout = ConnectionTimeout();
        try { client.Connect(IPAddress.Loopback, port); }
        catch { }

        if (await timeout) // Ensure we wait for the timeout before attempting another connection.
        {
          Log.Info("[OpenBCI] Connected to OpenBCI server.");
          currentState = OpenBciClientState.CONNECTED;
          Connected?.Invoke();
        }
      }

      await ProcessMessages();
    }

    /// <summary>Waits some time between connections/re-connections.</summary>
    private async Task<bool> ConnectionTimeout()
    {
      await Task.Delay(2000);
      return client.Connected;
    }

    #region Messaging
    /// <summary>Reads messages from the Socket and dispatches them appropriately</summary>
    private async Task ProcessMessages()
    {
      while (true)
      {
        (bool success, byte[] buffer) = await TryReceiveMessage();

        if (!success)
        {
          if (!Application.exitCancellationToken.IsCancellationRequested)
          {
            Disconnected?.Invoke();
          }
          break;
        }

        for (int i = 0; i < buffer.Length; i++)
        {
          int step = ParseMessageAt(buffer, i);
          if (step == 0) { break; }
          else { i += step; }
        }
      }
    }

    /// <summary>Attempts to receive a message. If this fails (e.g., due to a disconnection) it will return false. Otherwise it will return true</summary>
    /// <returns>A tuple consisting of a bool and a byte array. A true value for the bool indicates success.</returns>
    private async Task<(bool, byte[])> TryReceiveMessage()
    {
      byte[] buffer = new byte[1024];
      try { await client.ReceiveAsync(buffer, SocketFlags.None); }
      catch { return (false, buffer); } // Disconnected
      return (true, buffer); // Did not disconnect
    }

    // Below are specific messages that can be sent to the EOG Detection server.

    public async Task StartCalibration() => await SendMessage(ObciMessages.START_CALIBRATION);
    public async Task EndCalibration() => await SendMessage(ObciMessages.GO_IDLE);
    public async Task StartSystem() => await SendMessage(ObciMessages.GO_ACTIVE);
    public async Task StopSystem() => await SendMessage(ObciMessages.GO_IDLE);
    public async Task Exit() => await SendMessage(ObciMessages.EXIT);

    /// <summary>Sends a single byte message to the EOG Detector server.</summary>
    /// <param name="task">The message to be sent</param>
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
      catch // We were unable to send, therefore we must have disconnected.
      {
        if (!Application.exitCancellationToken.IsCancellationRequested)
        {
          Disconnected?.Invoke();
        }
      }
      Log.Debug("[OpenBCI] Successfully sent message to OpenBCI server: {0}", task);
    }
    /// <summary>Attempts to parse a message from the messages buffer.</summary>
    /// <param name="buffer">The buffer containing message data</param>
    /// <param name="index">Offset to start parsing from</param>
    /// <returns>The size of the message</returns>
    public int ParseMessageAt(byte[] buffer, int index)
    {
      byte messageType = buffer[index];

      int messageLength;

      switch (messageType)
      {
        case ObciMessages.CALIBRATION_RESULT:
          ReadCalibrationResult(buffer, index + 1);
          messageLength = 1 + CalibrationResult.GetSize(); // Should return where the next message begins
          break;
        default:
          messageLength = 0;
          break;
      }

      return messageLength;
    }
    #endregion
    /// <summary>Converts a message containing a calibration result into a CalibrationResult object.</summary>
    /// <param name="buffer">The buffer containing the message data</param>
    /// <param name="start">Offset to start parsing from</param>
    public void ReadCalibrationResult(byte[] buffer, int start)
    {
      var res = CalibrationResult.ParseResult(buffer, start);
      Log.Info(
        "[OpenBCI] Calibration finished with result: leftSignals = {0}, rightSignals = {1}, maxAmplitude = {2}, minAmplitude = {3}",
        res.leftSignals, res.rightSignals, res.maxAmplitude, res.minAmplitude
      );

      try
      {
        Calibrated?.Invoke(res);
      }
      catch (Exception e)
      {
        Debug.LogError(e);
      }
    }
    #region NetworkEvents
    /// <summary>Called when a message is received from the EOG Detection server</summary>
    void OnMessageReceived()
    {

    }

    /// <summary>Called when a disconnection is detected</summary>
    void OnDisconnected()
    {
      currentState = OpenBciClientState.DISCONNECTED;
      KillClient();
      Log.Info("[OpenBCI] Disconnected from OpenBCI server.");
      Task.Run(Connect);
    }

    /// <summary>Closes the client and cleans up allocated memory</summary>
    void KillClient()
    {
      client.Close();
      client.Dispose();
    }

    /// <summary>Called when a connection with the EOG detection server has been established</summary>
    void OnConnected()
    {
      // Task.Run(StartCalibration);
    }
    #endregion
    void OnDestroy()
    {
      KillClient();
    }
  }
}