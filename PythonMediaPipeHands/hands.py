# MediaPipe Hands
import mediapipe as mp
import cv2
import numpy as np
import threading
import time

DEBUG = False # significantly reduces performance
MODEL_COMPLEXITY = 0 # set to 1 to improve accuracy at the cost of performance

# the capture thread captures images from the WebCam on a separate thread (for performance)
class CaptureThread(threading.Thread):
    cap = None
    ret = None
    frame = None
    isRunning = False
    counter = 0
    timer = 0.0
    def run(self):
        self.cap = cv2.VideoCapture(0) # sometimes it can take a while for certain video captures
        print("Opened Capture")
        while(True):
            self.ret, self.frame = self.cap.read()
            self.isRunning = True
            if DEBUG:
                self.counter = self.counter+1
                if time.time()-self.timer>=3:
                    print("Capture FPS: ",self.counter/(time.time()-self.timer))
                    self.counter = 0
                    self.timer = time.time()

# the hand thread actually does the processing of the captured images
class HandThread(threading.Thread):
    data = ""
    dirty = True

    def run(self):
        mp_drawing = mp.solutions.drawing_utils
        mp_hands = mp.solutions.hands
        
        capture = CaptureThread()
        capture.start()

        # Based Heavily on: https://github.com/nicknochnack/MediaPipeHandPose/blob/main/Handpose%20Tutorial.ipynb
        with mp_hands.Hands(min_detection_confidence=0.75, min_tracking_confidence=0.5, model_complexity = MODEL_COMPLEXITY) as hands: 
            while capture.isRunning==False:
                print("Waiting for capture")
                time.sleep(500/1000)
            print("beginning capture")
                
            while capture.cap.isOpened():
                #ret, frame = cap.read()
                ret = capture.ret
                frame = capture.frame
                
                # BGR 2 RGB
                image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                
                # Flip on horizontal
                image = cv2.flip(image, 1)
                
                # Set flag
                image.flags.writeable = DEBUG
                
                # Detections
                results = hands.process(image)
                
                # RGB 2 BGR
                image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
                
                # Rendering results
                if DEBUG:
                    if results.multi_hand_landmarks:
                        for num, hand in enumerate(results.multi_hand_landmarks):
                            mp_drawing.draw_landmarks(image, hand, mp_hands.HAND_CONNECTIONS, 
                                                    mp_drawing.DrawingSpec(color=(121, 22, 76), thickness=2, circle_radius=4),
                                                    mp_drawing.DrawingSpec(color=(250, 44, 250), thickness=2, circle_radius=2),
                                                    )
                            
                # Set up data for piping
                self.data = ""
                i = 0
                if results.multi_hand_world_landmarks:
                    for j in range(len(results.multi_handedness)):
                        hand_world_landmarks = results.multi_hand_world_landmarks[j]
                        for i in range(0,21):
                            self.data += "{}|{}|{}|{}|{}\n".format(results.multi_handedness[j].classification[0].label,i,hand_world_landmarks.landmark[i].x,hand_world_landmarks.landmark[i].y,hand_world_landmarks.landmark[i].z)
                    self.dirty = True
                    
                if DEBUG:
                    cv2.imshow('Hand Tracking', image)

                    if cv2.waitKey(5) & 0xFF == ord('q'):
                        break

        capture.cap.release()
        cv2.destroyAllWindows()