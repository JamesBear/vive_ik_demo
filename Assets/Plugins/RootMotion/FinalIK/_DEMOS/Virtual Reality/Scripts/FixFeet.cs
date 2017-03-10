using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos {

	/// <summary>
	/// Fixes feet to where they were when last sampled.
	/// </summary>
	[RequireComponent(typeof(FullBodyBipedIK))]
	public class FixFeet : MonoBehaviour {
		
		[Range(0f, 1f)] public float weight = 1f;
	
		private FullBodyBipedIK ik;
		private Vector3 relativePosL, relativePosR;
		private Quaternion relativeRotL, relativeRotR;
	
		void Start () {
			ik = GetComponent<FullBodyBipedIK> ();
	
			Sample ();
		}
		
		// Remember the positions and rotations of the feet relative to the root of the character
		public void Sample() {
			relativePosL = transform.InverseTransformPoint (ik.solver.leftFootEffector.bone.position);
			relativePosR = transform.InverseTransformPoint (ik.solver.rightFootEffector.bone.position);
			
			relativeRotL = Quaternion.Inverse (transform.rotation) * ik.solver.leftFootEffector.bone.rotation;
			relativeRotR = Quaternion.Inverse (transform.rotation) * ik.solver.rightFootEffector.bone.rotation;
		}
		
		// Update feet effector offsets
		void LateUpdate() {
			if (weight <= 0f) return;
	
			ik.solver.leftFootEffector.positionOffset = (transform.TransformPoint (relativePosL) - ik.solver.leftFootEffector.bone.position) * weight;
			ik.solver.rightFootEffector.positionOffset = (transform.TransformPoint (relativePosR) - ik.solver.rightFootEffector.bone.position) * weight;
	
			ik.solver.leftFootEffector.bone.rotation = Quaternion.Lerp (ik.solver.leftFootEffector.bone.rotation, transform.rotation * relativeRotL, weight);
			ik.solver.rightFootEffector.bone.rotation = Quaternion.Lerp (ik.solver.rightFootEffector.bone.rotation, transform.rotation * relativeRotR, weight);
		}
	}
}
