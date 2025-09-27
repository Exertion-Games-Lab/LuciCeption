import argparse
import os
import time
import socket
from datetime import date, datetime, timedelta
from typing import Any, List, Tuple
from enum import Enum
import numpy as np
import numpy.typing as npt
from brainflow.board_shim import BoardIds, BoardShim, BrainFlowInputParams
from brainflow.data_filter import (
    DataFilter,
    DetrendOperations,
    FilterTypes,
)
from mne import create_info
from scipy.signal import find_peaks, find_peaks_cwt
import math

# our own graph drawer
from LiveEOGPlotter import EogPlotter, PlotterProcess

# import OpenBCIStyleLogger
from OpenBCIStyleLogger import OpenBCIStyleLogger
from PrintLogger import PrintLogger

class Detector:
  buffer_window_duration = 10 # length in seconds

  def setup(self, board_id, params):
    self.board_id = board_id
    self.params = params
    
    self.channels: List[int] = []
    self.eog_channel_left: int = -1
    self.eog_channel_right: int = -1
    
    self.setup_board()
    self.connect_to_board()
    self.setup_vars()
    self.setup_timeout_counter()
    self.setup_logger()

    self.plotter = EogPlotter()

  def setup_logger(self):

    self.BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    self.time_str = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    self.RUN_DIR = os.path.join(self.BASE_DIR, "results", f"Run_{self.time_str}")
    os.makedirs(self.RUN_DIR, exist_ok=True)

    # current_dir = os.path.dirname(os.path.abspath(__file__))
    # date_str = datetime.now().strftime("%Y-%m-%d")
    print_logger_filename=os.path.join(self.RUN_DIR, f"{self.time_str}_printLog.txt")
    self.print_logger = PrintLogger(filename=print_logger_filename)
    # self.print_logger = PrintLogger(
    #   filename=os.path.join(current_dir, "results/printLog.txt")
    # )

    OPENBCI_logger_filename=os.path.join(self.RUN_DIR, f"{self.time_str}_OpenBCI_Formatted.txt")
    self.OPENBCI_logger = OpenBCIStyleLogger(
        filename=OPENBCI_logger_filename,
        channel_names=["EOG Left", "EOG Right"],
        sampling_rate=self.sampling_rate
    )

  def setup_board(self):
    self.sampling_rate = BoardShim.get_sampling_rate(self.board_id)
    self.board = BoardShim(self.board_id, self.params)

  def connect_to_board(self):
    """
    Connect to the board and prepare to start streaming data
    """
    while not self.board.is_prepared():
      print("trying to connect to board...")
      try:
        self.board.prepare_session()
        self.board.start_stream()
      except:
        print("reconnection failed. Try again...")
        time.sleep(1)  ## wait for a bit
    print("reconnection finished")
  
  def setup_vars(self):
    self.eeg_data = np.ndarray(0)
    self.eog_class = "neutral"
    self.threshold = 5

    # LR signal counters
    self.eog_data_left = np.ndarray(0)
    self.eog_data_right = np.ndarray(0)
    self.N_count = 0
    self.L_count = 0
    self.LR_count = 0
    self.T_count = 0
    self.LR_count_perm = 0
  
  def setup_timeout_counter(self):
    self.timeoutCnt = 0
    self.TIMEOUT_THRESHOLD = 600

  def filter_eog_data(self, eog_data, removed_window: int = 1000):
    """
    Apply bandpass and detrend processing to the left and right channels of the eog_data
    """
    eog_data = self.convert_to_microvolt(eog_data)
    # eog_data = eog_data * 0.04
    DataFilter.perform_bandpass(
      eog_data, self.sampling_rate, 0.5, 15, 4, FilterTypes.BUTTERWORTH.value, 0
    )
    DataFilter.detrend(eog_data, DetrendOperations.LINEAR.value)
    # Remove the first `removed_window` samples from all channels because they will be noisy after filtering
    eog_data = eog_data[removed_window:]

    return eog_data
  
  def perform_calibration(self, callback):
    """
    sampling_rate=1000,
    min_peak_distance_ms=200,
    max_interval_ms=3000,
    peak_threshold=0.3,
    """
    
    self.retry_connection()
    calibration_interval: int = 10 

    self.eog_buffer_left: List[float] = []
    self.eog_buffer_right: List[float] = []

    total_left: int = 0
    total_right: int = 0
    max_amplitude = -math.inf
    min_amplitude = math.inf
    detecting_pattern = "LRLRLRLRLRLR"

    calibration_iterations = 0

    while calibration_iterations <= calibration_interval:
      if not self.hasNewData():
        continue

      new_eog_left, new_eog_right = self.update_data(self.get_buffer_size())

      if (not self.should_try_detect()):
        continue
      
      found_left, found_right, left_eog, right_eog, detected_left, detected_right = self.try_detect(detecting_pattern)
      # found_left, found_right, left_eog, right_eog, detected_left, detected_right = self.try_detect()

      max_amplitude = max(
        max_amplitude,
        np.max(left_eog),
        np.max(right_eog),
      )

      min_amplitude = min(
        min_amplitude,
        np.min(left_eog),
        np.min(right_eog),
      )

      self.write_info(new_eog_left, left_eog, detected_left, new_eog_right, right_eog, detected_right)

      if (found_left and found_right):
        self.print_logger.print(f"✅ Both eyes detected {detecting_pattern} pattern!")
        # self.write_info(new_eog_left, left_eog, detected_left, new_eog_right, right_eog, detected_right)
        total_left += len(detected_left)
        total_right += len(detected_right)
      else:
        self.print_logger.print(f"❌ Not both eyes detected {detecting_pattern} pattern.")

      calibration_iterations = calibration_iterations + 1
      print("Calibrating")
      time.sleep(1)

    callback(total_left, total_right, int(max_amplitude), int(min_amplitude))
    print("Calibration Finished")
  
  def start_detecting(self, detected_callback):
    self.eog_buffer_left: List[float] = []
    self.eog_buffer_right: List[float] = []
    while True:
      self.retry_connection()
      time.sleep(1)

      if not self.hasNewData():
        continue
    
      new_eog_left, new_eog_right = self.update_data(self.get_buffer_size())

      if (not self.should_try_detect()):
        continue

      detecting_pattern = "LRLRLRLRLRLR"
      
      found_left, found_right, left_eog, right_eog, detected_left, detected_right = self.try_detect(detecting_pattern)

      if (found_left and found_right):
        self.print_logger.print(f"✅ Both eyes detected {detecting_pattern} pattern!")
        detected_callback()
      self.write_info(new_eog_left,left_eog, detected_left, new_eog_right, right_eog, detected_right)


  def retry_connection(self):
    """
    Attempts to reconnect to the board if the timeout threshold is surpassed
    """
    if self.timeoutCnt >= self.TIMEOUT_THRESHOLD: # is this why it stops working after a certain amount of time? why is this line here?
      self.timeoutCnt = 0
      self.print_logger.print("time up. reconnect...")
      self.board.release_session()
      time.sleep(1)
      self.connect_to_board()
  
  def get_buffer_size(self):
    return int(self.sampling_rate * Detector.buffer_window_duration)
  
  def should_try_detect(self):
    return len(self.eog_buffer_left) >= self.get_buffer_size() and len(self.eog_buffer_right) >= self.get_buffer_size()

  """
  BOARD COMMUNICATION
  """

  def update_data(self, buffer_size: int):
    """
    Retrieves data from the board and updates the left and right EOG channels.
    """
    eog_data = self.board.get_board_data()

    # get the latest EOG data for both left and right channels
    new_eog_left: List[float] = (eog_data[self.eog_channel_left]).tolist()
    new_eog_right: List[float] = (eog_data[self.eog_channel_right]).tolist()

    # update the EOG buffers
    self.eog_buffer_left.extend(new_eog_left)
    self.eog_buffer_right.extend(new_eog_right)

    # resize the buffers to keep only the last `buffer_size` elements
    if len(self.eog_buffer_left) > buffer_size:
      self.eog_buffer_left = self.eog_buffer_left[-buffer_size:]
    if len(self.eog_buffer_right) > buffer_size:
      self.eog_buffer_right = self.eog_buffer_right[-buffer_size:]
    
    return new_eog_left, new_eog_right

  def hasNewData(self):
    return self.board.get_board_data_count() > 0

  """
  SIGNAL DETECTION
  """

  # Detect LRs Matching a Pattern.
  def detect_lr_pattern(
    self,
    eog_signal,
    detecting_pattern="LRLRLRLRLRLR",
    min_peak_distance_ms=200,
    max_interval_ms=3000,
    peak_threshold=5,
  ):
    signal = np.asarray(eog_signal, dtype=np.float64).flatten()
    distance = int((min_peak_distance_ms / 1000) * self.sampling_rate)
    max_interval = int((max_interval_ms / 1000) * self.sampling_rate)

    right_peaks, _ = find_peaks(signal, height=peak_threshold, distance=distance)
    left_peaks, _ = find_peaks(-signal, height=peak_threshold, distance=distance)

    # Merge and sort peaks with direction
    events = [(i, "R") for i in right_peaks] + [(i, "L") for i in left_peaks]
    events.sort()

    sequence = ""
    last_idx = None
    last_dir = None
    seq_indices = []

    for idx, direction in events:
      if last_idx is not None:
        if idx - last_idx > max_interval:
          sequence = ""
          seq_indices = []
      if direction != last_dir:
        sequence += direction
        seq_indices.append(idx)
        last_dir = direction
        last_idx = idx
        if sequence.endswith(detecting_pattern):
          # 只回傳最後這段 sequence 的 peak index
          return True, sequence, seq_indices[-len(detecting_pattern) :]
      else:
        last_idx = idx

    return False, sequence, seq_indices[-len(detecting_pattern) :]
  
  # Detects LR patterns in either channel
  def try_detect(self, detecting_pattern = "LRLRLRLR") -> Tuple[bool, bool, np.ndarray, np.ndarray, List[int], List[int]]:
    # detecting_pattern = "LRLRLRLR"

    left_eog: np.ndarray = np.array(self.eog_buffer_left, dtype=np.float64)
    # left_eog = self.convert_to_microvolt(left_eog)
    left_eog = self.filter_eog_data(left_eog, removed_window=1000)

    right_eog: np.ndarray = np.array(self.eog_buffer_right, dtype=np.float64)
    # right_eog = self.convert_to_microvolt(right_eog)
    right_eog = self.filter_eog_data(right_eog, removed_window=1000)

    found_left, sequence_left, detected_left = (
      self.detect_lr_pattern(
        left_eog,
        detecting_pattern,
        peak_threshold=self.threshold,
      )
    )
    found_right, sequence_right, detected_right = (
      self.detect_lr_pattern(
        right_eog,
        detecting_pattern,
        peak_threshold=self.threshold,
      )
    )

    self.print_logger.print(f"Left Detected sequence: {sequence_left}")
    self.print_logger.print(f"Right Detected sequence: {sequence_right}")

    return (found_left, found_right, left_eog, right_eog, detected_left, detected_right)

  def write_info(self, raw_left_eog, left_eog, detected_left, raw_right_eog, right_eog, detected_right):
    self.plotter.plot(
      left_eog,
      detected_left,
      right_eog,
      detected_right,
    )

    # current_dir = os.path.dirname(os.path.abspath(__file__))
    # self.time_str = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    # self.RUN_DIR = os.path.join(self.current_dir, "results", f"Run_{self.time_str}")

    # Save data to directory
    # os.makedirs(self.RUN_DIR, exist_ok=True)

    # np.savetxt(
    #   os.path.join(current_dir, f"results/{date_str}_{time_str}_eog_raw_data_left_.csv"),
    #   left_eog,
    #   delimiter=",",
    # )
    # np.savetxt(
    #   os.path.join(current_dir, f"results/{date_str}_{time_str}_eog_raw_data_right.csv"),
    #   right_eog,
    #   delimiter=",",
    # )
    for l,r in zip(raw_left_eog, raw_right_eog):
      self.OPENBCI_logger.update([l, r])


  def convert_to_microvolt(self, raw_data):
    Vref = 4.5      # Volts
    gain = 24       # Default Cyton gain
    factor = (Vref / (gain * (2**23))) * 1e6  # convert to μV
    return raw_data * factor
  
  def set_threshold(self, threshold):
    self.threshold = threshold
    print("Set threshold to: " + str(threshold))

class SyntheticDetector(Detector):
  """
  A synthetic / mock detector which uses fake data.
  """
  
  def __init__(self):
    self.params = BrainFlowInputParams()
    self.board_id = BoardIds.SYNTHETIC_BOARD.value
    self.setup(self.board_id, self.params)

    self.channels = BoardShim.get_eeg_channels(self.board_id)

    self.eeg_channel = self.channels[0]
    print("eeg channel number=", self.channels[0])
    self.eog_channel = self.channels[2]
    # Rohit's EOG classifier
    self.eog_channel_left = self.channels[2]  # left electrode Channel 3
    print("eog channel number=", self.channels[2])
    self.eog_channel_right = self.channels[1]  # right electrode Channel 2
    print("eog channel number=", self.channels[1])


class CytonDetector(Detector):
  """
  A detector based on the Open BCI Cyton Board
  """

  def __init__(self):
    self.params = BrainFlowInputParams()
    self.params.serial_port = "COM15"
    self.board_id = BoardIds.CYTON_BOARD.value
    self.setup(self.board_id, self.params)
    
    self.channels = BoardShim.get_eeg_channels(self.board_id)

    self.eeg_channel = self.channels[0]
    print("eeg channel number=", self.channels[0])
    self.eog_channel = self.channels[2]
    # Rohit's EOG classifier
    self.eog_channel_left = self.channels[2]  # left electrode Channel 3
    print("eog channel number=", self.channels[2])
    self.eog_channel_right = self.channels[1]  # right electrode Channel 2
    print("eog channel number=", self.channels[1])


class MuseDetector(Detector):
  def __init__(self):
    self.params = BrainFlowInputParams()
    args = self.init_arguments()
    self.params.ip_port = args.ip_port
    self.params.serial_port = args.serial_port
    self.params.mac_address = args.mac_address
    self.params.other_info = args.other_info
    self.params.serial_number = args.serial_number
    self.params.ip_address = args.ip_address
    self.params.ip_protocol = args.ip_protocol
    self.params.timeout = args.timeout
    self.params.file = args.file
    self.board_id = args.board_id
    self.setup(self.board_id, self.params)

    self.channels = BoardShim.get_eeg_channels(self.board_id)

    self.eeg_channel = self.channels[-1]
    # MUSE_S does not have EOG channel
    self.eog_channel = self.channels[2]


  def init_arguments(self):
    parser = argparse.ArgumentParser()
    parser.add_argument(
      "--timeout",
      type=int,
      help="timeout for device discovery or connection",
      required=False,
      default=0,
    )
    parser.add_argument(
      "--ip-port", type=int, help="ip port", required=False, default=0
    )
    parser.add_argument(
      "--ip-protocol",
      type=int,
      help="ip protocol, check IpProtocolType enum",
      required=False,
      default=0,
    )
    parser.add_argument(
      "--ip-address", type=str, help="ip address", required=False, default=""
    )
    parser.add_argument(
      "--serial-port",
      type=str,
      help="serial port",
      required=False,
      default="",
    )
    parser.add_argument(
      "--mac-address",
      type=str,
      help="mac address",
      required=False,
      default="",
    )
    parser.add_argument(
      "--other-info", type=str, help="other info", required=False, default=""
    )
    parser.add_argument(
      "--streamer-params",
      type=str,
      help="streamer params",
      required=False,
      default="",
    )
    parser.add_argument(
      "--serial-number",
      type=str,
      help="serial number",
      required=False,
      default="MuseS-864C",
    )
    parser.add_argument(
      "--board-id",
      type=int,
      help="board id, check docs to get a list of supported boards",
      default=39,
    )
    parser.add_argument(
      "--file", type=str, help="file", required=False, default=""
    )
    args = parser.parse_args()
    return args