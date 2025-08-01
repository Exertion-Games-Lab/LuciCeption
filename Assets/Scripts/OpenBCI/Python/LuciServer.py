from enum import Enum, IntEnum
import socket
from threading import Thread
import time

from LuciDetector import Detector

class UnityClientMessagesTypes(IntEnum):
  GO_IDLE = 0
  START_CALIBRATION = 1
  GO_ACTIVE = 2
  EXIT = 3
  CALIBRATION_RESULT = 4

class DetectorServer:
  """
  Receives messages from unity and updates detector states.
  """
  def __init__(self, stateMachine):
    """
    Creates a TCP server for IPC with Unity.
    """
    self.stateMachine = stateMachine
    self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    self.shouldContinue = True
    
    host = "127.0.0.1"
    port = 8080

    self.socket.bind((host, port))
    self.socket.listen()
    print("Started server")
    
    poller = Thread(None, self.serverLoop)
    poller.start()
  

  def accept(self):
    self.conn, addr = self.socket.accept()
    print(f"Accepted connection from {addr}")
  
  def serverLoop(self):
    while self.shouldContinue:
      self.accept()
      self.pollMessages(self.conn)

  def pollMessages(self, conn: socket.socket):
    with conn:
      while self.shouldContinue:
        data = UnityClientMessagesTypes(int.from_bytes(conn.recv(1)))
        if not data:
          print("Disconnected")
          break

        if data == UnityClientMessagesTypes.GO_ACTIVE:
          self.receivedGoActive()
          continue
        if data == UnityClientMessagesTypes.GO_IDLE:
          self.receivedGoIdle()
          continue
        if data == UnityClientMessagesTypes.START_CALIBRATION:
          self.receivedStartCalibration()
          continue
        if data == UnityClientMessagesTypes.EXIT:
          self.receivedExit()
          continue
        print("data was: " + str(data))

  def receivedGoActive(self):
    print("Received Message: 'Go Active'")
    self.stateMachine.setState(DetectorStateEnums.ACTIVE)

  def receivedGoIdle(self):
    print("Received Message: 'Go Idle'")
    self.stateMachine.setState(DetectorStateEnums.IDLE)
  
  def receivedStartCalibration(self):
    print("Received Message: 'Start Calibration'")
    self.stateMachine.setState(DetectorStateEnums.CALIBRATION)

  def receivedExit(self):
    print("Received Message: 'Exit'")
    self.shouldContinue = False

  def sendCalibrationResults(self, signals: int, minAmplitude: int, maxAmplitude: int):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.CALIBRATION_RESULT).to_bytes(length=1, signed=True, byteorder='little'))
    msg.extend(signals.to_bytes(length=4, signed=True, byteorder='little'))
    msg.extend(minAmplitude.to_bytes(length=4, signed=True, byteorder='little'))
    msg.extend(maxAmplitude.to_bytes(length=4, signed=True, byteorder='little'))
    
    self.conn.send(msg)

  def __del__(self):
    print("Closing socket")
    self.socket.close()

class DetectorStateEnums(Enum):
  IDLE = 0
  CALIBRATION = 1
  ACTIVE = 2
  EXIT = 3

class DetectorStateMachine:
  """
  Manages detector behaviour.
  """
  
  def __init__(self, detector: Detector):
    self.state = DetectorStateEnums.IDLE
    self.detector = detector
    print("Init state machine.")
    t = Thread(None, self.loop)
    t.start()

  def setServer(self, server: DetectorServer):
    self.server = server
    
  def loop(self):
    print("Starting state machine loop.")
    while self.state != DetectorStateEnums.EXIT:
      self.update()

  def update(self):
    if self.state == DetectorStateEnums.IDLE:
      time.sleep(1) # Poll Unity for messages in other thread
    
    if self.state == DetectorStateEnums.CALIBRATION:
      print("State is CALIBRATION")
      self.detector.perform_calibration()
      self.server.sendCalibrationResults(4, -1, 1)
      self.setState(DetectorStateEnums.IDLE)

    if self.state == DetectorStateEnums.ACTIVE:
      self.setState(DetectorStateEnums.IDLE)
  
  def setState(self, state: DetectorStateEnums):
    self.state = state
  
  def exit(self):
    self.state = DetectorStateEnums.EXIT