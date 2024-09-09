# Multithreaded Unity Python MediaPipe Hands
NOTICE: this project has been replaced by [Tracking4All](https://ko-fi.com/s/709e6d6f6f) which is actively supported and features much better quality, is cross-platform with embedded Unity support and more!

Testing hand tracking inside of Unity using Google MediaPipe Hands Python framework. Webcam readings, piping, and MediaPipe Hands all run on a different thread.
![](https://i.imgur.com/YCnpuB4.gif)

# Instructions
1. Requires Python, Unity Hub, a WebCam, and decently fast CPU.
2. pip install mediapipe
3. Clone this repository.
4. Run the Unity project FIRST which acts as a server.
5. Next run main.py which actually runs MediaPipe Hands.
6. Go back to the running Unity project to see your hands inside of the game view.

# Tips
* You can set the DEBUG flag True in hands.py to visualize what is being seen and how your hands are being interpreted.
* Improve the accuracy of the model by setting MODEL_COMPLEXITY to 1 inside hands.py
