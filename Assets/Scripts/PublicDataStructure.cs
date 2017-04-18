using UnityEngine;
using System.Collections;
using System.IO;

struct Pose
{
    public Vector3 pos;
    public Quaternion rot;
}

enum TrackerRole
{
    HandRight,
    HandLeft,
    Torso,
    FootRight,
    FootLeft,
    Head,
}

class DumbConfigFile
{
    const string configFileName = "height_config.txt";

    public static bool ReadString(out string strValue)
    {
        strValue = "";
        if (!File.Exists(configFileName))
            return false;

        strValue = File.ReadAllText(configFileName);
        return true;
    }

    public static bool ReadFloat(out float fValue)
    {
        fValue = 0f;
        string strValue;
        if (!ReadString(out strValue))
            return false;

        return float.TryParse(strValue.Trim(), out fValue);
    }
}

class IKDemoModelState
{
    public Vector3 eyePos;
    public Vector3 ankleMarkerLeftPos;
    public Vector3 ankleMarkerRightPos;
    public Vector3 markerHeadPos;
    public Vector3 modelScale;
}