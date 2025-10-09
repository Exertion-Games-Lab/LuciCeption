public enum ParticipantId
{
  P1,
  P2
}

public static class ParticipantConfig
{
  public static readonly ParticipantId Participant = ParticipantId.P2; // Set this to change which participant the system is for.

  public static string GetFanCommand(int speed)
  {
    if (Participant == ParticipantId.P1)
    {
      return $"https://maker.ifttt.com/trigger/fan_speed_{speed}/with/key/88IAzsMm2FG355HNDbZjq";
    }
    else if (Participant == ParticipantId.P2)
    {
      return $"https://maker.ifttt.com/trigger/p2_fan_speed_{speed}/with/key/88IAzsMm2FG355HNDbZjq";
    }
    return "";
  }

  public static string GetMotionCommand()
  {
    if (Participant == ParticipantId.P1)
    {
      return $"http://170.64.229.86:8080/get_motion_detected?key=xglluciception&participant=p1";
    }
    else if (Participant == ParticipantId.P2)
    {
      return $"http://170.64.229.86:8080/get_motion_detected?key=xglluciception&participant=p2";
    }
    return "";
  }
  
  public static string SetMotionCommand()
  {
    if (Participant == ParticipantId.P1)
    {
      return $"http://170.64.229.86:8080/set_motion_detected?key=xglluciception&participant=p1&value=false";
    }
    else if (Participant == ParticipantId.P2)
    {
      return $"http://170.64.229.86:8080/set_motion_detected?key=xglluciception&participant=p2&value=false";
    }
    return "";
  }

  
}