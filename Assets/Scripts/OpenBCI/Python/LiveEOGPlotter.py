import time
import matplotlib.pyplot as plt
import numpy as np
from multiprocessing import Queue
import queue
import multiprocessing as mp

class PlotterProcess:
  def __init__(self):
     pass
  
  def __call__(self, queue: Queue, ylim=(-20, 20)):
    self.queue = queue
    plt.ion()
    self.fig, self.ax = plt.subplots()
    self.line_left, = self.ax.plot([], [], label="Left Eye", color='blue')
    self.line_right, = self.ax.plot([], [], label="Right Eye", color='red')
    self.scatter_left = None
    self.scatter_right = None
    self.ax.set_ylim(*ylim)
    self.ax.set_title("Live EOG Signal")
    self.ax.set_xlabel("Sample")
    self.ax.set_ylabel("Amplitude")
    self.ax.grid(True)
    self.ax.legend()
    self.update_loop()
  
  def update_loop(self):
    while True:
      try:
        entry = self.queue.get(timeout=0.1)
        self.update(
          entry["left_data"],
          entry["left_detections"],
          entry["right_data"],
          entry["right_detections"])
      except Exception:
        plt.pause(0.1)
        pass
  
  def update(self, left_eog, detected_indices_left, right_eog=None, detected_indices_right=None):
    x = np.arange(len(left_eog))
    self.line_left.set_data(x, left_eog)
    self.ax.set_xlim(0, len(left_eog))
    # 畫右眼訊號
    if right_eog is not None:
        self.line_right.set_data(x, right_eog)
    else:
        self.line_right.set_data([], [])

    # 移除舊的 scatter
    if self.scatter_left:
        self.scatter_left.remove()
    if self.scatter_right:
        self.scatter_right.remove()

    # 畫左眼 detected indices
    if detected_indices_left is not None and len(detected_indices_left) > 0:
        self.scatter_left = self.ax.scatter(
            detected_indices_left,
            np.array(left_eog)[detected_indices_left],
            color='orange', marker='o', label='Left Detected'
        )
    else:
        self.scatter_left = None

    # 畫右眼 detected indices（用綠色）
    if right_eog is not None and detected_indices_right is not None and len(detected_indices_right) > 0:
        self.scatter_right = self.ax.scatter(
            detected_indices_right,
            np.array(right_eog)[detected_indices_right],
            color='green', marker='x', label='Right Detected'
        )
    else:
        self.scatter_right = None

    # 更新 legend
    handles, labels = self.ax.get_legend_handles_labels()
    self.ax.legend(handles, labels)

    self.fig.canvas.draw()
    self.fig.canvas.flush_events()
    plt.pause(0.01)

class EogPlotter:
  def __init__(self, ylim=(-20, 20)):
    self.queue = Queue()
    self.plotter = PlotterProcess()
    self.process = mp.Process(
      target=self.plotter,
      args=(self.queue, ylim),
      daemon=True
    )
    self.process.start()
  
  def plot(self, left_eog, detected_indices_left, right_eog=None, detected_indices_right=None):
    self.queue.put({
      "left_data": left_eog,
      "left_detections": detected_indices_left,
      "right_data": right_eog,
      "right_detections": detected_indices_right
    })