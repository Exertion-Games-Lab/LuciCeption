from matplotlib.collections import PathCollection
import numpy as np
import numpy.typing as npt
import scipy.signal as sig
import os
import matplotlib.pyplot as plt
from brainflow.board_shim import BoardIds, BoardShim, BrainFlowInputParams
from brainflow.data_filter import (
    DataFilter,
    DetrendOperations,
    FilterTypes,
)
import math

from scipy.ndimage import uniform_filter1d
from scipy.ndimage import binary_dilation

def filter_data(eog_data, min_bandpass=0.1, max_bandpass=40):
  sampling_rate = BoardShim.get_sampling_rate(BoardIds.CYTON_BOARD)
  decimate_factor = math.ceil(32 / sampling_rate)

  eog_data = eog_data.copy()
  # DataFilter.perform_bandpass(
  #   eog_data, sampling_rate, min_bandpass, max_bandpass, 4, FilterTypes.CHEBYSHEV_TYPE_1.value, 0
  # )

  highband = max_bandpass / (sampling_rate/2)
  lowband = min_bandpass / (sampling_rate/2)

  b1, a1 = sig.butter(4, Wn=[min_bandpass, max_bandpass], btype='bandpass', analog=False, fs=sampling_rate, output='ba')
  # eog_data = sig.sosfilt(sos, eog_data)
  f1 = sig.filtfilt(b1, a1, eog_data)

  f1 = sig.decimate(f1, decimate_factor)


  # sos = sig.cheby1(6, rp=1, Wn=band_freqs, btype='bandpass', fs=sampling_rate, output='sos')
  # DataFilter.detrend(eog_data, DetrendOperations.LINEAR.value)
  return f1


def detect_lr(horizontal, vertical):
  pass

def update_plots(ax, min_bandpass, max_bandpass, vertical, horizontal, line_vertical, line_horizontal, scatter_detections):

  fs = BoardShim.get_sampling_rate(BoardIds.CYTON_BOARD)

  v = filter_data(vertical, min_bandpass, max_bandpass)
  h = filter_data(horizontal, min_bandpass, max_bandpass)
  line_vertical.set_ydata(v)
  line_horizontal.set_ydata(h)

  v_low_smooth = uniform_filter1d((abs(v) < 10).astype(float), size=int(fs * 0.5))
  valid_region_mask = v_low_smooth > 0.99
  valid_region_mask = binary_dilation(valid_region_mask, iterations=10)
  h_peaks, _ = sig.find_peaks(abs(h), height = abs(v) + 10, prominence = 5, distance = fs * 0.1)
  h_peaks = [[p for p in h_peaks if valid_region_mask[p]]]


  if scatter_detections is not None:
    scatter_detections.remove()
  # scatter_detections = ax.scatter(v_peaks, np.array(v)[v_peaks], color='green', marker='x', label='Peaks Detected')
  scatter_detections = ax.scatter(h_peaks, np.array(h)[h_peaks], color='purple', marker='x', label='Peaks Detected')

  return scatter_detections
  # sig.find_peak(h, height = 5, distance=16)

class TestPlots:
  def __init__(self):
    current_dir = os.path.dirname(os.path.abspath(__file__))

    left_samples = np.genfromtxt(os.path.join(current_dir, "left.csv"), delimiter=",")
    right_samples = np.genfromtxt(os.path.join(current_dir, "right.csv"), delimiter=",")
    up_samples = np.genfromtxt(os.path.join(current_dir, "up.csv"), delimiter=",")
    down_samples = np.genfromtxt(os.path.join(current_dir, "down.csv"), delimiter=",")

    horizontal = (left_samples - right_samples) * 0.04
    vertical = (up_samples - down_samples) * 0.04

    plt.ion()
    self.fig, self.ax = plt.subplots()

    x = np.arange(len(filter_data(horizontal)))

    line_vertical, = self.ax.plot(x, filter_data(vertical), label="EOG Vertical", color='blue')
    line_horizontal, = self.ax.plot(x, filter_data(horizontal), label="EOG Horizontal", color='red')

    

    min_ax = self.fig.add_axes([0.25, 0.01, 0.65, 0.03])
    max_ax = self.fig.add_axes([0.25, 0.05, 0.65, 0.03])

    min_slider = plt.Slider(
        ax=min_ax,
        label='Minimum Bandpass Frequency [Hz]',
        valmin=0.01,
        valmax=2,
        valinit=0.15,
    )

    max_slider = plt.Slider(
        ax=max_ax,
        label='Maximum Bandpass Frequency [Hz]',
        valmin=1,
        valmax=100,
        valinit=15,
    )

    self.scatter_detections = None
    def update(val):
      self.scatter_detections = update_plots(self.ax, min_slider.val, max_slider.val, vertical, horizontal, line_vertical, line_horizontal, self.scatter_detections)

    self.ax.set_ylim((-60, 60))
    self.ax.set_title("EOG Signal (Real Data)")
    self.ax.set_xlabel("Sample")
    self.ax.set_ylabel("Amplitude")
    self.ax.grid(True)
    self.ax.legend()

    min_slider.on_changed(update)
    max_slider.on_changed(update)

    while True:
      plt.pause(1)

TestPlots()
