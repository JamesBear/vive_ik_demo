using UnityEngine;
using System.Collections;

public class FollowLogically : MonoBehaviour {

	public Transform target;


	private Transform representative;

	// Use this for initialization
	void Start () {
		Initialize ();
	}

	public void Initialize()
	{
		if (target != null) {
			GameObject go = new GameObject (name + "(representative)");
			go.transform.parent = target;
			go.transform.rotation = transform.rotation;
			go.transform.position = transform.position;
			representative = go.transform;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (representative) {
			transform.position = representative.position;
			transform.rotation = representative.rotation;
		}
	}
}
