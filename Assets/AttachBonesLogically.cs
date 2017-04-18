using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttachBonesLogically : MonoBehaviour {

	public bool btnTest;
	public Transform targetBonesRoot;

	// Use this for initialization
	void Start () {
		btnTest = false;
		AttachRecursively ();
	}
	
	// Update is called once per frame
	void Update () {
		if (btnTest) {
			btnTest = false;
			RunTest ();

		}
	}

	void AttachRecursively()
	{
		Dictionary<string, Transform> myBones = CollectBones (transform);
		Dictionary<string, Transform> targetBones = CollectBones (targetBonesRoot);

		foreach (var bone in myBones) {
			Transform target;
			if (targetBones.TryGetValue (bone.Key, out target)) {
				var follow = bone.Value.gameObject.AddComponent<FollowLogically> ();
				follow.target = target;
				follow.Initialize ();
			}
		}
	}

	void RunTest()
	{
		Dictionary<string, Transform> dict = new Dictionary<string, Transform> ();

		Traverse (transform, dict);

		List<string> bones = new List<string> (dict.Keys);
		bones.Sort ();

		string boneStr = "";
		foreach (var item in bones) {
			boneStr += item + ",";
		}

		Debug.Log (boneStr);
		Debug.Log ("bones = " + bones.Count);

	}

	void Traverse(Transform trans, Dictionary<string, Transform> dict)
	{
		if (dict.ContainsKey(trans.name))
			Debug.LogError("duplicated: " + trans.name);
		dict.Add (trans.name, trans);
		foreach (Transform child in trans)
		{
            if (child.name.Contains("(moved)"))
                continue;
			Traverse (child, dict);
		}
	}

	Dictionary<string, Transform> CollectBones(Transform trans)
	{

		Dictionary<string, Transform> dict = new Dictionary<string, Transform> ();

		Traverse (trans, dict);

		return dict;
	}
	
}
