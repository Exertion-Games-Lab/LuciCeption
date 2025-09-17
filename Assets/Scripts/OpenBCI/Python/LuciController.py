from __future__ import annotations

from enum import Enum, IntEnum
import multiprocessing as mp
from multiprocessing import Process, Pipe, Manager, Value
from multiprocessing.connection import PipeConnection
from multiprocessing.managers import SyncManager
import socket
from threading import Thread
import time
import os
from typing import Any
import asyncio

from LuciDetector import CytonDetector, Detector, SyntheticDetector

class UnityClientMessagesTypes(IntEnum):
  GO_IDLE = 0
  START_CALIBRATION = 1
  GO_ACTIVE = 2
  EXIT = 3
  CALIBRATION_RESULT = 4
  SET_THRESHOLD = 5
  LR_DETECTED = 6

class UnityCommunication:
  """
  Receives messages from unity and updates detector states.
  """
  def __init__(self, detector: Detector):
    """
    Creates a TCP server for IPC with Unity.
    """
    self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    self.exit = False
    self.detector = detector
    self.is_calibrating = False
    self.is_active = False

    host = "127.0.0.1"
    port = 8080

    self.socket.bind((host, port))
    self.socket.listen()
    print("Started server")
    self.server_loop()

  def accept(self):
    self.conn, addr = self.socket.accept()
    print(f"Accepted connection from {addr}")
  
  def server_loop(self):
    while self.should_continue():
      self.accept()
      self.poll_messages(self.conn)

  def poll_messages(self, conn: socket.socket):
    while self.should_continue():
      data = UnityClientMessagesTypes(int.from_bytes(conn.recv(1)))
      if not data:
        print("Disconnected")
        break
      self.process_message(conn, data)

    conn.close()

  def process_message(self, conn: socket.socket, message: UnityClientMessagesTypes):
    if message == UnityClientMessagesTypes.GO_ACTIVE: asyncio.run(self.receive_go_active())
    elif message == UnityClientMessagesTypes.GO_IDLE: self.receive_go_idle()
    elif message == UnityClientMessagesTypes.START_CALIBRATION: asyncio.run(self.receive_start_calibration())
    elif message == UnityClientMessagesTypes.EXIT: self.receive_exit()
    elif message == UnityClientMessagesTypes.SET_THRESHOLD: self.detector.set_threshold(int.from_bytes(conn.recv(4), signed=True, byteorder='little'))
    print("Received Message: ", message)

  # Message Receive Handlers
  async def receive_go_active(self):
    if self.is_active or self.is_calibrating: return
    self.is_active = True
    print("System is ON")
    # self.detector.perform_calibration(self.calibration_callback)
    self.detector.start_detecting(self.lr_detected_callback)
    self.is_active = False
    print("System is OFF")

  def receive_go_idle(self):
    pass

  async def receive_start_calibration(self):
    if self.is_active or self.is_calibrating: return
    self.is_calibrating = True
    self.detector.perform_calibration(self.calibration_callback)
    self.is_calibrating = False

  def calibration_callback(self, left_signals: int, right_signals: int, minAmplitude: int, maxAmplitude: int):
    self.send_calibration_results(left_signals, right_signals, minAmplitude, maxAmplitude)

  def lr_detected_callback(self):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.LR_DETECTED).to_bytes(length=1, signed=True, byteorder='little'))
    self.conn.send(msg)

  def receive_exit(self):
    self.exit = True

  # Message Send Handlers
  def send_calibration_results(self, left_signals: int, right_signals: int, minAmplitude: int, maxAmplitude: int):
    msg = bytearray()
    msg.extend(int(UnityClientMessagesTypes.CALIBRATION_RESULT).to_bytes(length=1, signed=True, byteorder='little'))
    msg.extend(left_signals.to_bytes(length=4, signed=True, byteorder='little'))
    msg.extend(right_signals.to_bytes(length=4, signed=True, byteorder='little'))
    msg.extend(minAmplitude.to_bytes(length=4, signed=True, byteorder='little'))
    msg.extend(maxAmplitude.to_bytes(length=4, signed=True, byteorder='little'))
    self.conn.send(msg)
  
  def should_continue(self):
    return not self.exit

  def __del__(self):
    print("Closing socket")
    self.socket.close()

class LuciController:
  def __init__(self, manager: SyncManager):
    detector = CytonDetector()
    server = UnityCommunication(detector)

def run():
  with Manager() as manager:
    controller = LuciController(manager)

if __name__ == '__main__':
  run()