import argparse
from datetime import date, datetime
import time
import numpy as np 
import pickle
import time
from numpy import random
import yasa
import mne
from mne import create_info
from mne.io import RawArray
# import flask
# from flask import Flask, jsonify, request
from brainflow.board_shim import BoardShim, BrainFlowInputParams, BoardIds
from brainflow.data_filter import DataFilter, WindowOperations, DetrendOperations, FilterTypes, AggOperations

#from the new code
import matplotlib.pyplot as plt
from scipy.signal import find_peaks, savgol_filter

# our own graph drawer
from LiveEOGPlotter import LiveEOGPlotter

# import OpenBCIStyleLogger
from OpenBCIStyleLogger import OpenBCIStyleLogger

from PrintLogger import PrintLogger

import pygame

import os
import warnings



def detect_lr_lr_pattern(eog_signal, detecting_pattern='LRLR', sampling_rate=1000, min_peak_distance_ms=200, max_interval_ms=3000, peak_threshold=0.3):
    signal = np.asarray(eog_signal, dtype=np.float64).flatten()
    distance = int((min_peak_distance_ms / 1000) * sampling_rate)
    max_interval = int((max_interval_ms / 1000) * sampling_rate)

    right_peaks, _ = find_peaks(signal, height=peak_threshold, distance=distance)
    left_peaks, _ = find_peaks(-signal, height=peak_threshold, distance=distance)

    # Merge and sort peaks with direction
    events = [(i, 'R') for i in right_peaks] + [(i, 'L') for i in left_peaks]
    events.sort()

    sequence = ''
    last_idx = None
    last_dir = None
    seq_indices = []

    for idx, direction in events:
        if last_idx is not None:
            if idx - last_idx > max_interval:
                sequence = ''
                seq_indices = []
        if direction != last_dir:
            sequence += direction
            seq_indices.append(idx)
            last_dir = direction
            last_idx = idx
            if sequence.endswith(detecting_pattern):
                # 只回傳最後這段 sequence 的 peak index
                return True, sequence, seq_indices[-len(detecting_pattern):]
        else:
            last_idx = idx

    return False, sequence, seq_indices[-len(detecting_pattern):]

class Parameter(object):
    pass

# A global variable to store the REM state
rem_state = {}

def play_audio(file_path):
    pygame.mixer.music.load(file_path)
    pygame.mixer.music.play()
    while pygame.mixer.music.get_busy():
       time.sleep(0.1)


class Detector:
    def __init__(self, board_name = "SYNTHETIC", threshold= 5, commandParameters = None):
        ## Init board and streaming params 
        self.params = BrainFlowInputParams()
        self.threshold = threshold

        # Identify serial port location of Wifi dongle
        # if you are using windows or mac, you will need to change below to your specific serial port directiory
        # if you are using a raspberry pi, it should work without needing to modify it
        # if you are using synthetic data, this dosent matter as it wont get used
        if board_name =="SYNTHETIC":
            # Synthetic for generating fake data, cyton for using the actual real board
            self.board_id = BoardIds.SYNTHETIC_BOARD.value
        elif board_name =="OPEN_BCI":
            self.params.serial_port = "COM15" #WINDOWS
            # self.params.serial_port = "/dev/cu.usbserial-DM00D4TL" #MAC
            #self.params.serial_port = "/dev/ttyUSB0" # Pi or Linux 
            self.board_id = BoardIds.CYTON_BOARD.value
        elif board_name =="MUSE_S":
            parser = argparse.ArgumentParser()
            parser.add_argument('--timeout', type=int, help='timeout for device discovery or connection', required=False,
                                default=0)
            parser.add_argument('--ip-port', type=int, help='ip port', required=False, default=0)
            parser.add_argument('--ip-protocol', type=int, help='ip protocol, check IpProtocolType enum', required=False,
                                default=0)
            parser.add_argument('--ip-address', type=str, help='ip address', required=False, default='')
            parser.add_argument('--serial-port', type=str, help='serial port', required=False, default='')
            parser.add_argument('--mac-address', type=str, help='mac address', required=False, default='')
            parser.add_argument('--other-info', type=str, help='other info', required=False, default='')
            parser.add_argument('--streamer-params', type=str, help='streamer params', required=False, default='')
            parser.add_argument('--serial-number', type=str, help='serial number', required=False, default='MuseS-864C')
            parser.add_argument('--board-id', type=int, help='board id, check docs to get a list of supported boards',default=39)
            parser.add_argument('--file', type=str, help='file', required=False, default='')
            args = parser.parse_args()

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
        else:
            print("Board ID wrong, using synthetic board")
            self.board_id = BoardIds.SYNTHETIC_BOARD.value

        
        # board_id = BoardIds.SYNTHETIC_BOARD.value
        # board_id = BoardIds.CYTON_BOARD.value

        self.channels = BoardShim.get_eeg_channels(self.board_id)
        if board_name == "MUSE_S":
            #todo: MUSE_S has 4 EEG channels, can be used together to improve accuracy
            self.eeg_channel = self.channels[-1]
            #MUSE_S does not have EOG channel
            self.eog_channel = self.channels[2]
        else:
            self.eeg_channel = self.channels[0]
            print('eeg channel number=', self.channels[0] )
            self.eog_channel = self.channels[2]
            #Rohit's EOG classifier
            self.eog_channel_left = self.channels[2] #left electrode Channel 3
            print('eog channel number=', self.channels[2] )
            self.eog_channel_right = self.channels [1] #right electrode Channel 2
            print('eog channel number=', self.channels[1] )


        self.sleep_stage = 'Awake'

        self.sampling_rate = BoardShim.get_sampling_rate(self.board_id)
        self.nfft = DataFilter.get_nearest_power_of_two(self.sampling_rate)
        self.board = BoardShim(self.board_id, self.params)
        self.num_channels = 3

        # Loading the sleep stageing model from file
        # Get the directory of the current script
        current_dir = os.path.dirname(os.path.abspath(__file__))
        # Construct the path to the file you want to load
        sleep_stager_file_path = os.path.join(current_dir, 'SleepStager_bands_SVM.sav')

        ## Initialise the sleep stage classifier 
        # self.sleep_stager = pickle.load(open(sleep_stager_file_path, 'rb'))

        ## Connect to the board and prepare to start streaming data
        while self.board.is_prepared()==False:
                    print("trying to connect to board...")
                    try:
                        self.board.prepare_session()
                        self.board.start_stream()
                        # Graph(board)
                    except:
                        print("reconnection failed. Try again...")
                        time.sleep(1) ## wait for a bit 
        print("reconnection finished")

        # mean_EOG = 0
        # time_counter = 0
        self.eeg_data = np.ndarray(0)
        self.eog_class = "neutral"
        # LR signal counters
        self.eog_data_left = np.ndarray(0)
        self.eog_data_right = np.ndarray(0)
        self.N_count = 0
        self.L_count = 0
        self.LR_count = 0
        self.T_count = 0
        self.LR_count_perm = 0
        

        # yasa counter
        # self.sleep_stage_array_eeg = np.ndarray(0)
        # self.sleep_stage_array_eog = np.ndarray(0)
        self.sleep_stage_array = np.ndarray((2, 0))
        self.stager_info = create_info(ch_names=["EEG","EOG"], sfreq = self.sampling_rate, ch_types = ["eeg","eog"])
        self.started_staging = 0
        self.yasa_filename = date.today().strftime("%d_%m_%Y") + '_EEG-log'
        
        # participant and save file data
        participant_name = "Cosmos"
        self.sleep_data_file_name = date.today().strftime("%d_%m_%Y") + '_' + participant_name + '_log.txt'

        #reconnected counter
        self.timeoutCnt = 0
        self.TIMEOUT_THRESHOLD = 600

        #calculate REM every TIME_WINDOW
        self.TIME_WINDOW = 450
        #how much percentage can be count as a real REM
        self.accepted_REM_percentage = 0.6
        #list of sleep stage within last TIME_PERIOD
        self.sleep_stage_list = ["NaN"]* self.TIME_WINDOW

        #how many REM are in the list
        self.REM_cnt = 0
        #final result
        self.sleep_stage_with_period = "NaN"

        # 
        self.BASE_DIR = os.path.dirname(os.path.abspath(__file__))
        self.time_str = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
        self.RUN_DIR = os.path.join(self.BASE_DIR, "results", f"Run_{self.time_str}")
        os.makedirs(self.RUN_DIR, exist_ok=True)

        # 初始化時
        self.logger = OpenBCIStyleLogger(
            # filename=f"D:/Projects/Github/Eye-Movement-Detection/results/EOG_OpenBCI_Formatted_{date_str}.txt",
            filename = os.path.join(self.RUN_DIR, f'EOG_OpenBCI_Formatted_{self.time_str}.txt'),
            channel_names=["EOG Left", "EOG Right"],
            sampling_rate=self.sampling_rate
        )


        self.print_logger = PrintLogger(filename=os.path.join(self.RUN_DIR, f'printLog_{self.time_str}.txt'))




    def convert_to_microvolt(self, raw_data):
        Vref = 4.5      # Volts
        gain = 24       # Default Cyton gain
        factor = (Vref / (gain * (2**23))) * 1e6  # convert to μV
        return raw_data * factor

    def filter_eog_data(self, eog_data, removed_window=1000):
        """
        對 eog_data 的左右通道進行 bandpass 與 detrend 處理
        """
        # DataFilter.perform_bandpass(
        #     eog_data[self.eog_channel_left], self.sampling_rate, 0.5, 15, 4, FilterTypes.BUTTERWORTH.value, 0
        # )
        # DataFilter.perform_bandpass(
        #     eog_data[self.eog_channel_right], self.sampling_rate, 0.5, 15, 4, FilterTypes.BUTTERWORTH.value, 0
        # )
        # DataFilter.detrend(
        #     eog_data[self.eog_channel_left], DetrendOperations.LINEAR.value
        # )
        # DataFilter.detrend(
        #     eog_data[self.eog_channel_right], DetrendOperations.LINEAR.value
        # )
        # eog_data = eog_data - np.mean(eog_data)
        DataFilter.perform_bandpass(
            eog_data, self.sampling_rate, 0.5, 15, 4, FilterTypes.BUTTERWORTH.value, 0
        )

        DataFilter.detrend(
            eog_data, DetrendOperations.LINEAR.value
        )

        # print("EOG data shape before trimming:", eog_data.shape)

        # Remove the first `removed_window` samples from all channels because they will be noisy after filtering
        eog_data = eog_data[removed_window:]
        # self.eog_data_left = (eog_data[self.eog_channel_left]).tolist()
        return eog_data

    def update(self):
        plotter = LiveEOGPlotter(ylim=(-20, 20))
        buffer_duration_sec = 10  # buffer (sec)
        buffer_size = int(self.sampling_rate * buffer_duration_sec)
        self.eog_buffer_left = []
        self.eog_buffer_right = []
        
        # set 5 min 
        pygame.mixer.init(frequency=44100, size=-16, channels=2)
        total_time = 5 * 60
        start_time = time.time()
        self.BASE_DIR = os.path.dirname(os.path.abspath(__file__))
        sfx_ding = os.path.join(self.BASE_DIR, 'audio/sfx_ding_.wav')
        start = os.path.join(self.BASE_DIR, 'audio/start_.wav')
        end = os.path.join(self.BASE_DIR, 'audio/end_.wav')
        play_audio(start)
        next_interval = random.randint(30, 60)
        next_trigger = next_interval
        

        while True:

            elapsed = int(time.time() - start_time)
            
            if elapsed >= next_trigger:
                play_audio(start)
                next_interval = random.randint(30, 60)
                next_trigger = elapsed + next_interval
                print(f"{next_interval} later, play")
            
            if self.timeoutCnt >= self.TIMEOUT_THRESHOLD:
                self.timeoutCnt = 0
                self.print_logger.print("time up. reconnect...")
                self.board.release_session()
                time.sleep(1)
                while self.board.is_prepared() == False:
                    self.print_logger.print("trying to connect to board...")
                    try:
                        self.board.prepare_session()
                        self.board.start_stream()
                    except:
                        self.print_logger.print("reconnection failed. Try again...")
                        time.sleep(1)
                self.print_logger.print("reconnection finished")

            time.sleep(1)  # Sleep for 1 second to avoid busy waiting
            
            new_data_count = self.board.get_board_data_count()
            if new_data_count > 0:
                new_eog_data = self.board.get_board_data()
                # eog_data = self.convert_to_microvolt(eog_data)

                # get the latest EOG data for both left and right channels
                new_eog_left = (new_eog_data[self.eog_channel_left]).tolist()
                new_eog_right = (new_eog_data[self.eog_channel_right]).tolist()

                # update the EOG buffers
                self.eog_buffer_left.extend(new_eog_left)
                self.eog_buffer_right.extend(new_eog_right)
                # resize the buffers to keep only the last `buffer_size` elements
                if len(self.eog_buffer_left) > buffer_size:
                    self.eog_buffer_left = self.eog_buffer_left[-buffer_size:]
                if len(self.eog_buffer_right) > buffer_size:
                    self.eog_buffer_right = self.eog_buffer_right[-buffer_size:]

                # Only perform detection when buffer is filled for one second
                # Draw both eyes at the same time, use a different color for right eye detected indices
                if len(self.eog_buffer_left) >= buffer_size and len(self.eog_buffer_right) >= buffer_size:
                    detecting_pattern = 'LRLRLRLRLR' #detecting 6 times from prior literature saying REM can have 2-5 random LRs
                    
                    left_eog = np.array(self.eog_buffer_left, dtype=np.float64)
                    right_eog = np.array(self.eog_buffer_right, dtype=np.float64)
                    left_eog = self.convert_to_microvolt(left_eog)
                    right_eog = self.convert_to_microvolt(right_eog)
                    left_eog = self.filter_eog_data(left_eog, removed_window=1000)
                    right_eog = self.filter_eog_data(right_eog, removed_window=1000)

                    found_left, sequence_left, detected_indices_left = detect_lr_lr_pattern(
                        left_eog, detecting_pattern, sampling_rate=self.sampling_rate, peak_threshold=self.threshold
                    )
                    found_right, sequence_right, detected_indices_right = detect_lr_lr_pattern(
                        right_eog, detecting_pattern, sampling_rate=self.sampling_rate, peak_threshold=self.threshold
                    )

                    self.print_logger.print(f"Left Detected sequence: {sequence_left}")
                    self.print_logger.print(f"Right Detected sequence: {sequence_right}")

                    if found_left and found_right:
                        play_audio(sfx_ding)
                        self.print_logger.print("✅ Both eyes detected LRLRLRLR pattern!")
                    else:
                        self.print_logger.print("❌ Not both eyes detected LRLRLRLR pattern.")

                    # plot the EOG signals and detected indices
                    plotter.update(left_eog, detected_indices_left, right_eog, detected_indices_right)


                    # Save the EOG data to files
                    np.savetxt(os.path.join(self.RUN_DIR, f"eog_raw_data_left.csv_{self.time_str}"), self.eog_buffer_left, delimiter=",")
                    np.savetxt(os.path.join(self.RUN_DIR, f"eog_raw_data_right.csv_{self.time_str}"), self.eog_buffer_right, delimiter=",")
                    for l, r in zip(new_eog_left, new_eog_right):
                        self.logger.update([l, r])
                        
                        
            if elapsed >= total_time:
                play_audio(end)
                break



def main():
    # flask_thread = Thread(target=lambda: app.run(host='0.0.0.0',port = '5050', debug=True, use_reloader=False))
    # flask_thread.start()
    print("What is your threshold for LRLR detection? (default 5 uV)")
    threshold = input("Enter threshold in uV: ")
    if not threshold:
        threshold = 5
    else:
        threshold = int(threshold)
    board = Detector("OPEN_BCI", threshold=threshold)
    # board = Detector()
    board.update()


    # commandParameters = Parameter()
    # commandParameters.isInterrupt = False
    # commandParameters.induction = False
    # detection(commandParameters,board_name = "SYNTHETIC")

if __name__ == "__main__":
    main()
