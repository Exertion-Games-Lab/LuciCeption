import os
import time
import pygame
import random


def play_audio(file_path):
    pygame.mixer.music.load(file_path)
    pygame.mixer.music.play()
    while pygame.mixer.music.get_busy():
        time.sleep(0.1)
    
        
def counting_play(arg: bool):
    pygame.mixer.init(frequency=44100, size=-16, channels=2)

    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    sfx_ding = os.path.join(BASE_DIR, 'audio/sfx_ding_.wav')
    start = os.path.join(BASE_DIR, 'audio/start_.wav')
    end = os.path.join(BASE_DIR, 'audio/end_.wav')
    # sfx_ding = '/Users/pperciva1/Desktop/Percival/Cosmos_project/audio/sfx_ding_.wav'
    # start = '/Users/pperciva1/Desktop/Percival/Cosmos_project/audio/start_.wav'
    # end = '/Users/pperciva1/Desktop/Percival/Cosmos_project/audio/end_.wav'

    total_time = 5 * 60
    start_time = time.time()
    print("Start")
    
    play_audio(start)
    next_interval = random.randint(30, 60)
    next_trigger = next_interval
    print(f"{next_interval} later, play")


    while True:
        elapsed = int(time.time() - start_time)

        if elapsed >= total_time:
            print("Time's up!")
            play_audio(end)
            if arg:
                play_audio(sfx_ding)
                print("End with sfx_ding")
            else:
                print("End without sfx_ding")
            break

        if elapsed >= next_trigger:
            print(f"[{elapsed}s] play LR_signal audio")
            play_audio(start)

            next_interval = random.randint(30, 60)
            next_trigger = elapsed + next_interval
            print(f"{next_interval} later, play")


# counting_play(True)



if __name__ == "__main__":
    counting_play(False)
