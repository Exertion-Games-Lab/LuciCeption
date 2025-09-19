import socket
import time
from UnityMessageEnums import UnityClientMessagesTypes
from LuciDetector import CytonDetector, Detector, SyntheticDetector
from LuciController import UnityCommunication
import asyncio

class MockClient():
  def __init__(self):
    self.host = "127.0.0.1"
    self.port = 8080
    self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

  def connect(self):
    self.socket.connect((self.host, self.port))
    print("Client connected")
  
  def disconnect(self):
    self.socket.close()

  def start_detector(self):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.GO_ACTIVE).to_bytes(length=1, signed=True, byteorder='little'))
    self.socket.send(msg)
    print("Sent message to start detector.")
  
  def stop_detector(self):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.GO_IDLE).to_bytes(length=1, signed=True, byteorder='little'))
    self.socket.send(msg)
    print("Sent message to stop detector.")
  
  def start_calibrating(self):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.START_CALIBRATION).to_bytes(length=1, signed=True, byteorder='little'))
    self.socket.send(msg)
  
  def set_threshold(self, threshold):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.SET_THRESHOLD).to_bytes(length=1, signed=True, byteorder='little'))
    msg.extend(int(threshold).to_bytes(length=4, signed=True, byteorder='little'))
    self.socket.send(msg)

async def start_detector(barrier: asyncio.Barrier):
  mockDetector = SyntheticDetector()
  server = UnityCommunication(mockDetector)
  await barrier.wait()

async def start_client(barrier: asyncio.Barrier):
  print("Building client...")
  mockClient = MockClient()

  await barrier.wait()
  mockClient.connect()
  time.sleep(2)
  mockClient.start_detector()

async def Test():
  barrier = asyncio.Barrier(2)

  client = asyncio.create_task(start_client(barrier))
  detector = asyncio.create_task(start_detector(barrier))
  # await detector
  await client

if __name__ == "__main__":
  asyncio.run(Test())