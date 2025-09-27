import os
from datetime import datetime

class OpenBCIStyleLogger:
    def __init__(self, filename, channel_names, sampling_rate):
        """
        Initialize the storage file and write the header.
        :param filename: Name of the txt file to save
        :param channel_names: list of channel labels (e.g., ["EOG Left", "EOG Right"])
        :param sampling_rate: Data sampling rate (Hz)
        """
        self.filename = filename
        self.sampling_rate = sampling_rate
        self.channel_names = channel_names
        self.start_time = datetime.now()

        # Create and write header
        with open(self.filename, 'w') as f:
            f.write("%OpenBCI Raw EXG Data\n")
            # f.write(f"%Number of channels = {len(channel_names)}\n")
            f.write(f"%Number of channels = 8\n")
            f.write(f"%Sample Rate = {sampling_rate} Hz\n")
            f.write("%Board = OpenBCI_GUI$BoardCytonSerial\n")
            f.write("Sample Index,EXG Channel 0,EXG Channel 1,EXG Channel 2,EXG Channel 3,EXG Channel 4,EXG Channel 5,EXG Channel 6,EXG Channel 7,Accel Channel 0,Accel Channel 1,Accel Channel 2,Other_1,Other_1,Other_1,Other_1,Other_1,Other_1,Other_1,Other_1,Other_2,Other_3,Other_4,Other_5,Other_6,Other_7,Analog Channel 0,Analog Channel 1,Analog Channel 2,Timestamp,Other,Timestamp (Formatted)\n")

            # f.write(f"%Date: {self.start_time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            # f.write(f"%Time, {', '.join(channel_names)}\n")
        
            self.sample_index = 0  # Used to generate timestamp
    
    def update(self, data_by_channel):
        """
        Update with one data sample, appending a line in OpenBCI style.
        :param data_by_channel: list of float values, corresponding to each channel (e.g., [val_left, val_right])
        """
        assert len(data_by_channel) == len(self.channel_names), "Channel count mismatch"

        # timestamp_sec = self.sample_index / self.sampling_rate
        

        timestamp_str = datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]
        # line = f"{float(self.sample_index)}, " + ", ".join(f"{float(x):.6f}" for x in data_by_channel) + ", -187500.0 "*6+ ", 0.0"*13 +", "+ f"{datetime.now().timestamp():.3f}" + ", 0.0, " + timestamp_str + "\n"
        line = f"{float(self.sample_index)}, " + ", ".join(f"{float(x):.6f}" for x in data_by_channel) + ", 0.0 "*6+ ", 0.0"*13 +", "+ f"{datetime.now().timestamp():.3f}" + ", 0.0, " + timestamp_str + "\n"
        with open(self.filename, 'a') as f:
            f.write(line)
        self.sample_index += 1
        if self.sample_index % 256 == 0:
            self.sample_index = 0  # Reset sample index every 256 samples to avoid overflow
