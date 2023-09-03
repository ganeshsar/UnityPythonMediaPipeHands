using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

/* Currently very messy because both the server code and hand-drawn code is all in the same file here.
 * But it is still fairly straightforward to use as a reference/base.
 */

public class PipeServer : MonoBehaviour
{
    public Transform rParent;
    public Transform lParent;
    public GameObject landmarkPrefab;
    public GameObject linePrefab;
    public float multiplier = 10f;
    public int average = 1;

    NamedPipeServerStream server;

    const int LANDMARK_COUNT = 21;
    public enum Landmark { 
        Wrist = 0,
        Thumb1 = 1,
        Thumb2 = 2,
        Thumb3 = 3,
        Thumb4 = 4,
        Index1 = 5,
        Index2 = 6,
        Index3 = 7,
        Index4 = 8,
        Middle1 = 9,
        Middle2 = 10,
        Middle3 = 11,
        Middle4 = 12,
        Ring1 = 13,
        Ring2 = 14,
        Ring3 = 15,
        Ring4 = 16,
        Pinky1 = 17,
        Pinky2 = 18,
        Pinky3 = 19,
        Pinky4 = 20
    }
    public class Hand 
    {
        public Vector3[] positionsBuffer = new Vector3[LANDMARK_COUNT];
        public GameObject[] instances = new GameObject[LANDMARK_COUNT];
        public LineRenderer[] lines = new LineRenderer[5];

        public float reportedSamplesPerSecond;
        public float lastSampleTime;
        public float samplesCounter;

        public Hand(Transform parent, GameObject landmarkPrefab, GameObject linePrefab)
        {
            for (int i = 0; i < instances.Length; ++i)
            {
                instances[i] = Instantiate(landmarkPrefab);// GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instances[i].transform.localScale = Vector3.one * 0.1f;
                instances[i].transform.parent = parent;
            }
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = Instantiate(linePrefab).GetComponent<LineRenderer>();
            }
        }
        public void UpdateLines()
        {
            lines[0].positionCount = 5;
            lines[1].positionCount = 5;
            lines[2].positionCount = 5;
            lines[3].positionCount = 5;
            lines[4].positionCount = 5;

            lines[0].SetPosition(0, instances[(int)Landmark.Wrist].transform.position);
            lines[0].SetPosition(1, instances[(int)Landmark.Thumb1].transform.position);
            lines[0].SetPosition(2, instances[(int)Landmark.Thumb2].transform.position);
            lines[0].SetPosition(3, instances[(int)Landmark.Thumb3].transform.position);
            lines[0].SetPosition(4, instances[(int)Landmark.Thumb4].transform.position);

            lines[1].SetPosition(0, instances[(int)Landmark.Wrist].transform.position);
            lines[1].SetPosition(1, instances[(int)Landmark.Index1].transform.position);
            lines[1].SetPosition(2, instances[(int)Landmark.Index2].transform.position);
            lines[1].SetPosition(3, instances[(int)Landmark.Index3].transform.position);
            lines[1].SetPosition(4, instances[(int)Landmark.Index4].transform.position);

            lines[2].SetPosition(0, instances[(int)Landmark.Wrist].transform.position);
            lines[2].SetPosition(1, instances[(int)Landmark.Middle1].transform.position);
            lines[2].SetPosition(2, instances[(int)Landmark.Middle2].transform.position);
            lines[2].SetPosition(3, instances[(int)Landmark.Middle3].transform.position);
            lines[2].SetPosition(4, instances[(int)Landmark.Middle4].transform.position);

            lines[3].SetPosition(0, instances[(int)Landmark.Wrist].transform.position);
            lines[3].SetPosition(1, instances[(int)Landmark.Ring1].transform.position);
            lines[3].SetPosition(2, instances[(int)Landmark.Ring2].transform.position);
            lines[3].SetPosition(3, instances[(int)Landmark.Ring3].transform.position);
            lines[3].SetPosition(4, instances[(int)Landmark.Ring4].transform.position);

            lines[4].SetPosition(0, instances[(int)Landmark.Wrist].transform.position);
            lines[4].SetPosition(1, instances[(int)Landmark.Pinky1].transform.position);
            lines[4].SetPosition(2, instances[(int)Landmark.Pinky2].transform.position);
            lines[4].SetPosition(3, instances[(int)Landmark.Pinky3].transform.position);
            lines[4].SetPosition(4, instances[(int)Landmark.Pinky4].transform.position);
        }

        public float GetFingerAngle(Landmark referenceFrom, Landmark referenceTo, Landmark from, Landmark to)
        {
            Vector3 reference = (instances[(int)referenceTo].transform.position - instances[(int)referenceFrom].transform.position).normalized;
            Vector3 direction = (instances[(int)to].transform.position - instances[(int)from].transform.position).normalized;
            return Vector3.SignedAngle(reference, direction, Vector3.Cross(reference, direction));
        }
    }
    private Hand left;
    private Hand right;

    public float sampleThreshold = 0.25f; // how many seconds of data should be averaged to produce a single pose of the hand.

    private void Start()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        left = new Hand(lParent,landmarkPrefab,linePrefab);
        right = new Hand(rParent, landmarkPrefab,linePrefab);

        Thread t = new Thread(new ThreadStart(Run));
        t.Start();

    }
    private void Update()
    {
        UpdateHand(left);
        UpdateHand(right);
    }
    private void UpdateHand(Hand h)
    {
        if (h.samplesCounter == 0) return;

        if (Time.timeSinceLevelLoad - h.lastSampleTime >= sampleThreshold)
        {
            for (int i = 0; i < LANDMARK_COUNT; ++i)
            {
                h.instances[i].transform.localPosition = h.positionsBuffer[i] / (float)h.samplesCounter * multiplier;
                h.positionsBuffer[i] = Vector3.zero;
            }

            h.reportedSamplesPerSecond = h.samplesCounter / (Time.timeSinceLevelLoad - h.lastSampleTime);
            h.lastSampleTime = Time.timeSinceLevelLoad;
            h.samplesCounter = 0f;

            h.UpdateLines();
        }
    }

    void Run()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        // Piping Method based heavily off of: https://gist.github.com/JonathonReinhart/bbfa618f9ad19e2ca48d5fd10914b069
        // Open the named pipe.
        server = new NamedPipeServerStream("UnityMediaPipeHands");

        print("Waiting for connection...");
        server.WaitForConnection();

        print("Connected.");
        var br = new BinaryReader(server, Encoding.UTF8);

        while (true)
        {
            try
            {
                Hand h = null;
                var len = (int)br.ReadUInt32();
                var str = new string(br.ReadChars(len));

                string[] lines = str.Split('\n');
                foreach (string l in lines)
                {
                    string[] s = l.Split('|');
                    if (s.Length < 5) continue;
                    int i;
                    if (s[0] == "Left") h = left;
                    else if (s[0] == "Right") h = right;
                    if (!int.TryParse(s[1], out i)) continue;
                    h.positionsBuffer[i] += new Vector3(float.Parse(s[2]), float.Parse(s[3]), float.Parse(s[4]));
                    h.samplesCounter += 1f / LANDMARK_COUNT;
                }
            }
            catch (EndOfStreamException)
            {
                break;                    // When client disconnects
            }
        }

    }

    private void OnDisable()
    {
        print("Client disconnected.");
        server.Close();
        server.Dispose();
    }
}
