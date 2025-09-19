from enum import IntEnum

class UnityClientMessagesTypes(IntEnum):
  GO_IDLE = 0
  START_CALIBRATION = 1
  GO_ACTIVE = 2
  EXIT = 3
  CALIBRATION_RESULT = 4
  SET_THRESHOLD = 5
  LR_DETECTED = 6