#pipe server
from hands import HandThread
import time
import struct

thread = HandThread()
thread.start()

# Piping method based heavily on: https://gist.github.com/JonathonReinhart/bbfa618f9ad19e2ca48d5fd10914b069
f = open(r'\\.\pipe\UnityMediaPipeHands', 'r+b', 0)

while True:
    
    if thread.dirty:
        s = thread.data.encode('ascii')
        
        f.write(struct.pack('I', len(s)) + s)   
        f.seek(0)                          

        thread.dirty = False

    time.sleep(16/1000) # enforces a hard limit on the speed of sending data
    
quit()

