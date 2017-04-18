using UnityEngine;
using System.Collections;

public class FollowLogically : MonoBehaviour {

	public Transform target;

    public bool test;

	private Transform representative;
    private Vector3 lastPos;

	// Use this for initialization
	void Start () {
		Initialize ();
	}

	public void Initialize()
	{
        //if (target != null) {
        //	GameObject go = new GameObject (name + "(representative)");
        //	go.transform.parent = target;
        //	go.transform.rotation = transform.rotation;
        //	go.transform.position = transform.position;
        //	representative = go.transform;
        //}
        //      test = false;
        if (target != null)
        {
            transform.parent = target;
            name = name + "(moved)";
        }
	}
	
	//// Update is called once per frame
	//void Update () {
	//	if (representative != null) {
	//		transform.position = representative.position;
	//		transform.rotation = representative.rotation;

 //           if (test)
 //           {
 //               if (lastPos != representative.position)
 //               {
 //                   Debug.Log("pos change, " + name);
 //               }
 //               lastPos = representative.position;
 //           }
	//	}
	//}
}
