using UnityEngine;
using System.Collections;

public class ThreePointIK : MonoBehaviour {

	public Transform IKBone1;
	public Transform IKBone2;
	public Transform IKBone3;
	public Transform target;

    // whether UpdateIK() is called by Update() or called externally
	public bool manualUpdateIK = false;
	public BendNormalStrategy bendNormalStrategy = BendNormalStrategy.followTarget;
	//public float debugAngle = 0f;
	public Vector3 defaultBendNormal;

	public enum BendNormalStrategy
	{
		followTarget,
		rightArm,
		leftArm,
        head,
        //rightFoot,
        //leftFoot,
	}

    /// <summary>
    /// Gets the rotation that can be used to convert a rotation from one axis space to another.
    /// </summary>
    public static Quaternion RotationToLocalSpace(Quaternion space, Quaternion rotation)
    {
        return Quaternion.Inverse(Quaternion.Inverse(space) * rotation);
    }

    ///// <summary>
    ///// Gets the Quaternion from rotation "from" to rotation "to".
    ///// </summary>
    //public static Quaternion FromToRotation(Quaternion from, Quaternion to)
    //{
    //    if (to == from) return Quaternion.identity;

    //    return to * Quaternion.Inverse(from);
    //}

    public class Bone
	{

		public float length;
		public Transform trans;
		private Quaternion targetToLocalSpace;
		private Vector3 defaultLocalBendNormal;

		public void Initiate(Vector3 childPosition, Vector3 bendNormal) {
			// Get default target rotation that looks at child position with bendNormal as up
			Quaternion defaultTargetRotation = Quaternion.LookRotation(childPosition - trans.position, bendNormal);

			// Covert default target rotation to local space
			targetToLocalSpace = RotationToLocalSpace(trans.rotation, defaultTargetRotation);

			defaultLocalBendNormal = Quaternion.Inverse(trans.rotation) * bendNormal;
		}


		public Quaternion GetRotation(Vector3 direction, Vector3 bendNormal) {
			return Quaternion.LookRotation(direction, bendNormal) * targetToLocalSpace;
		}

		public Vector3 GetBendNormalFromCurrentRotation() {
			return trans.rotation * defaultLocalBendNormal;
		}

		public Vector3 GetBendNormalFromCurrentRotation(Vector3 defaultNormal) {
			return trans.rotation * defaultNormal;
		}
	}

	private Bone bone1;
	private Bone bone2;
	private Bone bone3;

	private bool initialized = false;

	// Use this for initialization
	void Start () {
		Init ();
	}
	
	// Update is called once per frame
	void Update () {
		if (!initialized) {
			return;
		}

		if (!manualUpdateIK) {
			UpdateIK ();
		}	
	}

	Vector3 GetBendDirection(Vector3 IKPosition, Vector3 bendNormal) {
		Vector3 direction = IKPosition - bone1.trans.position;
		if (direction == Vector3.zero) return Vector3.zero;

		float directionSqrMag = direction.sqrMagnitude;
		float directionMagnitude = (float)Mathf.Sqrt(directionSqrMag);

		float x = (directionSqrMag + bone1.length*bone1.length - bone2.length*bone2.length) / 2f / directionMagnitude;
		float y = (float)Mathf.Sqrt(Mathf.Clamp(bone1.length*bone1.length - x * x, 0, Mathf.Infinity));

		Vector3 yDirection = Vector3.Cross(direction, bendNormal);
		return Quaternion.LookRotation(direction, yDirection) * new Vector3(0f, y, x);
	}

	public void UpdateIK()
	{
		//clamp target if distance to target is longer than bones combined
		Vector3 actualTargetPos;
		float overallLength = Vector3.Distance (bone1.trans.position, target.position);
		if (overallLength > bone1.length + bone2.length) {
			actualTargetPos = bone1.trans.position + (target.position - bone1.trans.position).normalized * (bone1.length + bone2.length);
			overallLength = bone1.length + bone2.length;
		} else
			actualTargetPos = target.position;

		//calculate bend normal
        //you may need to change this based on the model you chose
		Vector3 bendNormal = Vector3.zero;
        switch (bendNormalStrategy)
        {
            case BendNormalStrategy.followTarget:
			    bendNormal = - Vector3.Cross (actualTargetPos - bone1.trans.position, target.forward);
                break;
            case BendNormalStrategy.rightArm:
		    	bendNormal = Vector3.down;
                break;
            case BendNormalStrategy.leftArm:
			    bendNormal = Vector3.up;
                break;
            case BendNormalStrategy.head:
                bendNormal = bone1.GetBendNormalFromCurrentRotation();
                break;
            //case BendNormalStrategy.rightFoot:
            //    bendNormal = -Vector3.Cross(actualTargetPos - bone1.trans.position, target.forward);
            //    break;
            //case BendNormalStrategy.leftFoot:
            //    bendNormal = -Vector3.Cross(actualTargetPos - bone1.trans.position, target.forward);
            //    break;
            default:
			    Debug.LogError ("Undefined bendnormal strategy: " + bendNormalStrategy);
                break;
        }

		//calculate bone1, bone2 rotation
		Vector3 bendDirection = GetBendDirection(actualTargetPos, bendNormal);

		// Rotating bone1
		bone1.trans.rotation = bone1.GetRotation(bendDirection, bendNormal);

		// Rotating bone 2
		bone2.trans.rotation = bone2.GetRotation(actualTargetPos - bone2.trans.position, bone2.GetBendNormalFromCurrentRotation(defaultBendNormal));
		//bone2.trans.rotation = bone2.GetRotation(actualTargetPos - bone2.trans.position, Quaternion.AngleAxis(debugAngle, target.forward)* target.up);

		bone3.trans.rotation = target.rotation;
	}

	void Init()
	{
		if (IKBone1 == null || IKBone2 == null || IKBone3 == null || target == null) {
			Debug.LogError ("bone or target empty, IK aborted");
			return;
		}

		bone1 = new Bone{ trans = IKBone1 };
		bone2 = new Bone{ trans = IKBone2 };
		bone3 = new Bone{ trans = IKBone3 };
		bone1.length = Vector3.Distance (bone1.trans.position, bone2.trans.position);
		bone2.length = Vector3.Distance (bone2.trans.position, bone3.trans.position);

		Vector3 bendNormal = defaultBendNormal;
		//if (bendNormal == Vector3.zero) bendNormal = Vector3.forward;
		bone1.Initiate(bone2.trans.position, bendNormal);
		bone2.Initiate(bone3.trans.position, bendNormal);

		initialized = true;
	}
}
