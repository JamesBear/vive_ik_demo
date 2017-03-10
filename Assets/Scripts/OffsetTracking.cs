using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class OffsetTracking {


    public TrackerRole trackerRole;
    public Transform targetTrans;
    public int deviceIndex;
    public Dictionary<int, Transform> deviceMarkerDict = new Dictionary<int, Transform>();
    
    GameObject child;


    Pose GetPose(int index)
    {
        var trans = deviceMarkerDict[index];
        Pose pose = new Pose { pos = trans.position, rot = trans.rotation };
        return pose;
    }

    public void StartTracking()
    {
        child = new GameObject("marker for " + trackerRole);
        child.transform.parent = deviceMarkerDict[deviceIndex];
        child.transform.position = targetTrans.position;
        child.transform.rotation = targetTrans.rotation;
    }


    public void UpdateOffsetTracking()
    {
        targetTrans.position = child.transform.position;
        targetTrans.rotation = child.transform.rotation;
    }
    

    
}
