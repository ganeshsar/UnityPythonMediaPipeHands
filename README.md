# Multithreaded Unity Python MediaPipe Hands
Testing hand tracking inside of Unity using Google MediaPipe Hands Python framework. Webcam readings, piping, and MediaPipe Hands all run on a different thread.

# Instructions
1. Requires Python, Unity Hub, a WebCam, and decently fast CPU.
2. pip install mediapipe
3. Clone this repository.
4. Run the Unity project FIRST which acts as a server.
5. Next run main.py which actually runs MediaPipe Hands.
6. Go back to the running Unity project to see your hands inside of the game view.

# Tips
* You can set the DEBUG flag True in hands.py to visualize what is being seen and how your hands are being interpreted.
