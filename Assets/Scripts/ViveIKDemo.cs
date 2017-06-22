using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A tracker is 'bound' to one foot, then its pose(position and rotation) can be 
/// used to calculate the ankle position used in IK.
/// Similarly, the torso pose is calculated by a bound tracker or controller, then 
/// the derived arm positions and thigh positions are used in IK.
/// 
/// </summary>
public class ViveIKDemo : MonoBehaviour {


    enum Stage
    {
        Stage0,
        Stage1,
        Stage2,
        Stage3
    }

    public Text logText;
    public GameObject ankleMarkerLeft;
    public GameObject ankleMarkerRight;
    public List<GameObject> deviceMarkers;
    public GameObject leftHandOffsetObject;
    public GameObject rightHandOffsetObject;
    public GameObject markerHead;
    public Transform eyeTransform;

    Dictionary<int, Transform> deviceMarkerDict = new Dictionary<int, Transform>();
    Stage stage = Stage.Stage0;
    Queue<object> logQueue = new Queue<object>(); // logs in logQueue can be seen in VR
    int maxLogCount = 8;
    float initHeight = 1.75f;
    IKDemoModelState initModelState;

    Dictionary<TrackerRole, int> trackers = new Dictionary<TrackerRole, int>();
    List<OffsetTracking> offsetTrackedList = new List<OffsetTracking>();
	Transform leftHandTarget = null;
	Transform rightHandTarget = null;
    AnimationBlendTest animationBlender;
    List<ThreePointIK> ikList = new List<ThreePointIK>();// to fully control the execution order of the IK solvers


    // Use this for initialization
    void Start () {
        foreach (var item in deviceMarkers)
        {
            deviceMarkerDict[(int)item.GetComponent<SteamVR_TrackedObject>().index] = item.transform;
        }
        //InitWithCustomHeight();

        animationBlender = GetComponent<AnimationBlendTest>();
        RecordInitModelState();
	}
	
    void RecordInitModelState()
    {
        initModelState = new IKDemoModelState();
        initModelState.eyePos = eyeTransform.position;
        initModelState.ankleMarkerLeftPos = ankleMarkerLeft.transform.position;
        initModelState.ankleMarkerRightPos = ankleMarkerRight.transform.position;
        initModelState.markerHeadPos = markerHead.transform.position;
        initModelState.modelScale = transform.localScale;
    }

	// Update is called once per frame
	void Update () {
        
		UpdateIK();

        bool gripClicked = false;
        for (int i = (int)SteamVR_TrackedObject.EIndex.Device1; i < (int)SteamVR_TrackedObject.EIndex.Device15; i++)
        {
            var device = SteamVR_Controller.Input(i);
            gripClicked |= device.GetPressUp(SteamVR_Controller.ButtonMask.Grip);
        }

        if (stage == Stage.Stage0)
        {
            AutoAdjustHeight();
        }

        if (stage == Stage.Stage0 && (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetMouseButtonUp(2) || gripClicked))
        {
            gripClicked = false;
            if (AssignTrackers())
            {
                stage = Stage.Stage2;
                MyLog("Entering stage2");
            }
            else
            {
                MyLogError("Not enough tracked devices found");
            }
        }

        if (stage == Stage.Stage1)
        {
            var pos = Camera.main.transform.position;
            pos.y = 0;
            pos.z -= 0.2f;
            transform.position = pos;
        }



        if (stage == Stage.Stage1 && (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetMouseButtonUp(2) || gripClicked))
        {
            gripClicked = false;
            stage = Stage.Stage2;
            MyLog("Entering stage2");
        }

        if (stage == Stage.Stage2 && (Input.GetKeyUp(KeyCode.Alpha2) || gripClicked))
        {
            gripClicked = false;
            StartOffsetTracking();
            StartIK();
            stage = Stage.Stage3;
            MyLog("Entering stage3");
        }

        if (stage == Stage.Stage3)
        {
            UpdateOffsetTracking();
        }
    }

    void AutoAdjustHeight()
    {

        float actualEyeHeight = Camera.main.transform.position.y;

        actualEyeHeight = Mathf.Clamp(actualEyeHeight, 0.7f, 2.5f);

        float eyeHeightToBodyHeadRatio = initModelState.eyePos.y / initHeight;
        float estimatedHeight = actualEyeHeight / eyeHeightToBodyHeadRatio;

        AdjustToHeight(estimatedHeight);
    }

    void InitWithCustomHeight()
    {
        float customHeight;
        if (DumbConfigFile.ReadFloat(out customHeight))
        {
            MyLog("Adjusting to height = " + customHeight);
            AdjustToHeight(customHeight);
        }
        else
        {
            Debug.Log("no height config file found, using default");
        }
    }

    void AdjustToHeight(float customHeight)
    {
        float ratio = customHeight / initHeight;
        ankleMarkerLeft.transform.position = initModelState.ankleMarkerLeftPos*ratio;
        ankleMarkerRight.transform.position = initModelState.ankleMarkerRightPos*ratio;
        markerHead.transform.position = initModelState.markerHeadPos*ratio;
        transform.localScale = initModelState.modelScale*ratio;
    }


    void StartIK()
    {

        int headIndex = -1;
        

        var tpIkComps = GetComponents<ThreePointIK>();

        foreach (var item in tpIkComps)
        {
            item.manualUpdateIK = true;
            item.enabled = true;

            ikList.Add(item);
        }


        headIndex = ikList.FindIndex(item => item.bendNormalStrategy == ThreePointIK.BendNormalStrategy.head);
        if (headIndex >= 0)
            Swap(ikList, 0, headIndex);
    }


    void UpdateIK()
	{

        foreach (var item in ikList)
        {
            item.UpdateIK();
        }
	}

    Pose GetPose(int index)
    {
        var trans = deviceMarkerDict[index];
        Pose pose = new Pose { pos = trans.position, rot = trans.rotation };
        return pose;
    }

    void StartOffsetTracking()
    {
        List<TrackerRole> keys = new List<TrackerRole> { TrackerRole.Torso, TrackerRole.FootLeft, TrackerRole.FootRight };
        List<Transform> values = new List<Transform> { transform, ankleMarkerLeft.transform, ankleMarkerRight.transform };
        
        foreach (var item in trackers)
        {
            int index = keys.IndexOf(item.Key);
            if (index >= 0)
            {
                var trackedInfo = new OffsetTracking();
                trackedInfo.deviceIndex = item.Value;
                trackedInfo.trackerRole = keys[index];
                trackedInfo.targetTrans = values[index];
                trackedInfo.deviceMarkerDict = deviceMarkerDict;
                trackedInfo.StartTracking();
                offsetTrackedList.Add(trackedInfo);
            }
        }

        markerHead.transform.parent = Camera.main.transform;
        
    }

    void UpdateOffsetTracking()
    {
        foreach (var item in offsetTrackedList)
            item.UpdateOffsetTracking();
    
        if (animationBlender != null && trackers.ContainsKey(TrackerRole.HandLeft))
        {
            int leftHandIndex = trackers[TrackerRole.HandLeft];
            float triggerValue = SteamVR_Controller.Input(leftHandIndex).GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            animationBlender.lerpValue = triggerValue;
        }

        if (trackers.ContainsKey(TrackerRole.HandRight))
        {
            int leftHandIndex = trackers[TrackerRole.HandRight];
            float triggerValue = SteamVR_Controller.Input(leftHandIndex).GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            UpdatePistolEffect(triggerValue);
        }
    }

    void UpdatePistolEffect(float triggerValue)
    {

    }

    void Swap<T>(List<T> list, int indexA, int indexB)
    {
        T temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }

    bool AssignTrackers()
    {
        trackers.Clear();

        List<KeyValuePair<int, Vector3>> devices = new List<KeyValuePair<int, Vector3>>();
        for (int i = (int)SteamVR_TrackedObject.EIndex.Device1; i < (int)SteamVR_TrackedObject.EIndex.Device15; i++)
        {
            var device = SteamVR_Controller.Input(i);
            if (device.hasTracking && device.connected && device.valid)
            {
                var deviceClass = Valve.VR.OpenVR.System.GetTrackedDeviceClass((uint)i);
                if (deviceClass == Valve.VR.ETrackedDeviceClass.Controller || deviceClass == Valve.VR.ETrackedDeviceClass.GenericTracker)
                {
                    devices.Add(new KeyValuePair<int, Vector3>(i, GetPose(i).pos));
                    Debug.Log((SteamVR_TrackedObject.EIndex)i + ", type = " + deviceClass);
                }
                else
                {
                    MyLog("Device"+i+" is a basestation, type = " + deviceClass);
                }
            }
        }

        MyLog("device count = " + devices.Count);

        devices.Sort((a, b) => a.Value.y.CompareTo(b.Value.y));
               
        if (devices.Count == 5)
        {
            if (devices[0].Value.x < 0f)
                Swap(devices, 0, 1);
            if (devices[3].Value.x < 0f)
                Swap(devices, 3, 4);

            trackers[TrackerRole.FootRight] = devices[0].Key;
            trackers[TrackerRole.FootLeft] = devices[1].Key;
            trackers[TrackerRole.Torso] = devices[2].Key;
            trackers[TrackerRole.HandRight] = devices[3].Key;
            trackers[TrackerRole.HandLeft] = devices[4].Key;

			rightHandTarget = deviceMarkerDict[devices[3].Key];
			leftHandTarget = deviceMarkerDict [devices [4].Key];
        }
        else if (devices.Count == 4)
        {
            if (devices[0].Value.x < 0f)
                Swap(devices, 0, 1);

            trackers[TrackerRole.FootRight] = devices[0].Key;
            trackers[TrackerRole.FootLeft] = devices[1].Key;
            trackers[TrackerRole.Torso] = devices[2].Key;

			if (devices [3].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [3].Key;
				leftHandTarget = deviceMarkerDict[devices[3].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [3].Key;
				rightHandTarget = deviceMarkerDict[devices[3].Key];
			}
        }
        else if (devices.Count == 3)
        {
            trackers[devices[0].Value.x < 0f? TrackerRole.FootLeft : TrackerRole.FootRight] = devices[0].Key;
			trackers[TrackerRole.Torso] = devices[1].Key;
			if (devices [2].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [2].Key;
				leftHandTarget = deviceMarkerDict[devices[2].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [2].Key;
				rightHandTarget = deviceMarkerDict[devices[2].Key];
			}
        }
        else if (devices.Count == 2)
        {
			trackers[TrackerRole.Torso] = devices[0].Key;
			if (devices [1].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [1].Key;
				leftHandTarget = deviceMarkerDict[devices[1].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [1].Key;
				rightHandTarget = deviceMarkerDict[devices[1].Key];
			}
        }
        else
        {
            return false;
        }

        if (leftHandTarget != null && leftHandOffsetObject != null)
        {
            AssignChildAndKeepLocalTrans(ref leftHandTarget, leftHandOffsetObject.transform);
        }
        if (rightHandTarget != null && rightHandOffsetObject != null)
        {
            AssignChildAndKeepLocalTrans(ref rightHandTarget, rightHandOffsetObject.transform);
        }

        string strTrackers = "";
        foreach (var item in trackers.Keys)
            strTrackers += item + ",";
        MyLog("bound body parts: " + strTrackers);

        return true;
    }

    void AssignChildAndKeepLocalTrans(ref Transform parent, Transform child)
    {
        Vector3 localPos = child.localPosition;
        Vector3 localScale = child.localScale;
        Quaternion localRot = child.localRotation;
        child.parent = parent;
        child.localPosition = localPos;
        child.localScale = localScale;
        child.localRotation = localRot;
        parent = child;
    }

    void MyLog(object msg)
    {
        Debug.Log(msg);

        if (logText != null)
        {
            logQueue.Enqueue(msg);
            if (logQueue.Count > maxLogCount)
                logQueue.Dequeue();
            DisplayLogQueue();
        }
    }

    void MyLogError(object msg)
    {
        Debug.LogError(msg);

        if (logText != null)
        {
            logQueue.Enqueue("ERROR: " + msg);
            if (logQueue.Count > maxLogCount)
                logQueue.Dequeue();
            DisplayLogQueue();
        }
    }

    void DisplayLogQueue()
    {
        string str = "";
        foreach (var item in logQueue)
        {
            str += item + "\n";
        }
        logText.text = str;
    }
}
