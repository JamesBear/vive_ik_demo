using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Head effector for FBBIK.
	/// </summary>
	public class FBBIKHeadEffector : MonoBehaviour {

		[Tooltip("Reference to the FBBIK component.")]
		public FullBodyBipedIK ik;
		
		[Header("Position")]
		[Tooltip("Master weight for positioning the head.")]
		[Range(0f, 1f)] public float positionWeight = 1f;

		[Tooltip("The weight of moving the body along with the head")]
		[Range(0f, 1f)] public float bodyWeight = 0.8f;

		[Tooltip("The weight of moving the thighs along with the head")]
		[Range(0f, 1f)] public float thighWeight = 0.8f;
		
		[Header("Rotation")]
		[Tooltip("The weight of rotating the head bone after solving")]
		[Range(0f, 1f)] public float rotationWeight = 0f;
		
		[Tooltip("The master weight of bending/twisting the spine to the rotation of the head effector. This is similar to CCD, but uses the rotation of the head effector not the position.")]
		[Range(0f, 1f)] public float bendWeight = 1f;
		
		[Tooltip("The bones to use for bending.")]
		public BendBone[] bendBones = new BendBone[0];
		
		[System.Serializable]
		public class BendBone {

			[Tooltip("Assign spine and/or neck bones.")]
			public Transform transform;

			[Tooltip("The weight of rotating this bone.")]
			[Range(0f, 1f)] public float weight = 0.5f;

			private Quaternion defaultLocalRotation = Quaternion.identity;

			public BendBone() {}

			public BendBone(Transform transform, float weight) {
				this.transform = transform;
				this.weight = weight;
			}

			public void StoreDefaultLocalState() {
				defaultLocalRotation = transform.localRotation;
			}

			public void FixTransforms() {
				transform.localRotation = defaultLocalRotation;
			}
		}

		[Header("CCD")]
		[Tooltip("Optional. The master weight of the CCD (Cyclic Coordinate Descent) IK effect that bends the spine towards the head effector before FBBIK solves.")] 
		[Range(0f, 1f)] public float CCDWeight = 1f;

		[Tooltip("The weight of rolling the bones in towards the target")]
		[Range(0f, 1f)] public float roll = 0f;

		[Tooltip("Smoothing the CCD effect.")]
		[Range(0f, 1000f)] public float damper = 500f;

		[Tooltip("Bones to use for the CCD pass. Assign spine and/or neck bones.")]
		public Transform[] CCDBones = new Transform[0];

		[Header("Stretching")]
		[Tooltip("Stretching the spine/neck to help reach the target. This is useful for making sure the head stays locked relative to the VR headset. NB! Stretching is done after FBBIK has solved so if you have the hand effectors pinned and spine bones included in the 'Stretch Bones', the hands might become offset from their target positions.")]
		[Range(0f, 1f)] public float stretchWeight = 1f;
		[Tooltip("Stretch magnitude limit.")]
		public float maxStretch = 0.1f;
		[Tooltip("If > 0, dampers the stretching effect.")]
		public float stretchDamper = 0f;
		[Tooltip("If true, will fix head position to this Transform no matter what. Good for making sure the head will not budge away from the VR headset")]
		public bool fixHead;
		[Tooltip("Bones to use for stretching. The more bones you add, the less noticable the effect.")]
		public Transform[] stretchBones = new Transform[0];

		private Vector3 offset, headToBody, shoulderCenterToHead, headToLeftThigh, headToRightThigh, leftShoulderPos, rightShoulderPos;
		private float shoulderDist, leftShoulderDist, rightShoulderDist;
		private Quaternion chestRotation;
		private Quaternion headRotationRelativeToRoot;
		private Quaternion[] ccdDefaultLocalRotations = new Quaternion[0];
		private Vector3 headLocalPosition;
		private Quaternion headLocalRotation;
		private Vector3[] stretchLocalPositions = new Vector3[0];
		private Quaternion[] stretchLocalRotations = new Quaternion[0];
		private int bendBonesCount;
		private int ccdBonesCount;
		private int stretchBonesCount;

		// Register to get messages from the FBBIK
		void Start() {
			ik.solver.OnPreRead += OnPreRead;
			ik.solver.OnPreIteration += Iterate;
			ik.solver.OnPostUpdate += OnPostUpdate;
			ik.solver.OnStoreDefaultLocalState += OnStoreDefaultLocalState;
			ik.solver.OnFixTransforms += OnFixTransforms;

			headRotationRelativeToRoot = Quaternion.Inverse(ik.references.root.rotation) * ik.references.head.rotation;
		}

		// Store the default local positions and rotations of the bones used by this head effector.
		private void OnStoreDefaultLocalState() {
			foreach (BendBone bendBone in bendBones) {
				if (bendBone != null) bendBone.StoreDefaultLocalState();
			}

			ccdDefaultLocalRotations = new Quaternion[CCDBones.Length];
			for (int i = 0; i < CCDBones.Length; i++) {
				if (CCDBones[i] != null) ccdDefaultLocalRotations[i] = CCDBones[i].localRotation;
			}

			headLocalPosition = ik.references.head.localPosition;
			headLocalRotation = ik.references.head.localRotation;

			stretchLocalPositions = new Vector3[stretchBones.Length];
			stretchLocalRotations = new Quaternion[stretchBones.Length];
			for (int i = 0; i < stretchBones.Length; i++) {
				if (stretchBones[i] != null) {
					stretchLocalPositions[i] = stretchBones[i].localPosition;
					stretchLocalRotations[i] = stretchBones[i].localRotation;
				}
			}

			bendBonesCount = bendBones.Length;
			ccdBonesCount = CCDBones.Length;
			stretchBonesCount = stretchBones.Length;
		}

		// Fix the bones used by this head effector to their default local state
		private void OnFixTransforms() {
			foreach (BendBone bendBone in bendBones) {
				if (bendBone != null) bendBone.FixTransforms();
			}

			for (int i = 0; i < CCDBones.Length; i++) {
				if (CCDBones[i] != null) CCDBones[i].localRotation = ccdDefaultLocalRotations[i];
			}

			ik.references.head.localPosition = headLocalPosition;
			ik.references.head.localRotation = headLocalRotation;

			for (int i = 0; i < stretchBones.Length; i++) {
				if (stretchBones[i] != null) {
					stretchBones[i].localPosition = stretchLocalPositions[i];
					stretchBones[i].localRotation = stretchLocalRotations[i];
				}
			}
		}

		// Called by the FBBIK each time before it reads the pose
		private void OnPreRead() {
			if (!enabled) return;
			if (!gameObject.activeInHierarchy) return;

			if (ik.solver.iterations == 0) return;

			if (bendBonesCount != bendBones.Length || ccdBonesCount != CCDBones.Length || stretchBonesCount != stretchBones.Length) OnStoreDefaultLocalState();

			// Spine Bend
			SpineBend ();

			// CCD
			CCDPass();

			// Body
			offset = transform.position - ik.references.head.position;
			
			shoulderDist = Vector3.Distance(ik.references.leftUpperArm.position, ik.references.rightUpperArm.position);
			leftShoulderDist = Vector3.Distance(ik.references.head.position, ik.references.leftUpperArm.position);
			rightShoulderDist = Vector3.Distance(ik.references.head.position, ik.references.rightUpperArm.position);
			
			headToBody = ik.solver.rootNode.position - ik.references.head.position;
			headToLeftThigh = ik.references.leftThigh.position - ik.references.head.position;
			headToRightThigh = ik.references.rightThigh.position - ik.references.head.position;

			leftShoulderPos = ik.references.leftUpperArm.position + offset * bodyWeight;
			rightShoulderPos = ik.references.rightUpperArm.position + offset * bodyWeight;

			chestRotation = Quaternion.LookRotation(ik.references.head.position - ik.references.leftUpperArm.position, ik.references.rightUpperArm.position - ik.references.leftUpperArm.position);
		}

		// Bending the spine to the head effector
		private void SpineBend() {
			float w = bendWeight * ik.solver.IKPositionWeight;

			if (w <= 0f) return;
			if (bendBones.Length == 0) return;

			Quaternion rotation = transform.rotation * Quaternion.Inverse(ik.references.root.rotation * headRotationRelativeToRoot);
			
			float step = 1f / bendBones.Length;

			for (int i = 0; i < bendBones.Length; i++) {
				if (bendBones[i].transform != null) {
					bendBones[i].transform.rotation = Quaternion.Lerp(Quaternion.identity, rotation, step * bendBones[i].weight * w) * bendBones[i].transform.rotation;
				}
			}
		}

		// Single CCD pass to make the spine less stiff 
		private void CCDPass() {
			float w = CCDWeight * ik.solver.IKPositionWeight;

			if (w <= 0f) return;
			
			for (int i = CCDBones.Length - 1; i > -1; i--) {
				Quaternion r = Quaternion.FromToRotation(ik.references.head.position - CCDBones[i].position, transform.position - CCDBones[i].position) * CCDBones[i].rotation;
				float d = Mathf.Lerp((CCDBones.Length - i) / CCDBones.Length, 1f, roll);
				float a = Quaternion.Angle(Quaternion.identity, r);
				
				a = Mathf.Lerp(0f, a, (damper - a) / damper);
				
				CCDBones[i].rotation = Quaternion.RotateTowards(CCDBones[i].rotation, r, a * w * d);
			}
		}

		// Called by the FBBIK before each solver iteration
		private void Iterate(int iteration) {
			if (!enabled) return;
			if (!gameObject.activeInHierarchy) return;

			if (ik.solver.iterations == 0) return;

			// Shoulders
			leftShoulderPos = transform.position + (leftShoulderPos - transform.position).normalized * leftShoulderDist;
			rightShoulderPos = transform.position + (rightShoulderPos - transform.position).normalized * rightShoulderDist;

			Solve (ref leftShoulderPos, ref rightShoulderPos, shoulderDist);

			LerpSolverPosition(ik.solver.leftShoulderEffector, leftShoulderPos, positionWeight * ik.solver.IKPositionWeight);
			LerpSolverPosition(ik.solver.rightShoulderEffector, rightShoulderPos, positionWeight * ik.solver.IKPositionWeight);

			// Body
			Quaternion chestRotationSolved = Quaternion.LookRotation(transform.position - leftShoulderPos, rightShoulderPos - leftShoulderPos);
			Quaternion rBody = QuaTools.FromToRotation(chestRotation, chestRotationSolved);

			Vector3 headToBodySolved = rBody * headToBody;
			LerpSolverPosition(ik.solver.bodyEffector, transform.position + headToBodySolved, positionWeight * ik.solver.IKPositionWeight);

			// Thighs
			Quaternion rThighs = Quaternion.Lerp(Quaternion.identity, rBody, thighWeight);
			
			Vector3 headToLeftThighSolved = rThighs * headToLeftThigh;
			Vector3 headToRightThighSolved = rThighs * headToRightThigh;

			LerpSolverPosition(ik.solver.leftThighEffector, transform.position + headToLeftThighSolved, positionWeight * ik.solver.IKPositionWeight);
			LerpSolverPosition(ik.solver.rightThighEffector, transform.position + headToRightThighSolved, positionWeight * ik.solver.IKPositionWeight);
		}

		// Called by the FBBIK each time it is finished updating
		private void OnPostUpdate() {
			if (!enabled) return;
			if (!gameObject.activeInHierarchy) return;

			// Stretching the spine and neck
			Stretching ();

			// Rotate the head bone
			ik.references.head.rotation = Quaternion.Lerp(ik.references.head.rotation, transform.rotation, rotationWeight * ik.solver.IKPositionWeight);
		}

		// Stretching the spine/neck to help reach the target. This is most useful for making sure the head stays locked relative to the VR controller
		private void Stretching() {
			float w = stretchWeight * ik.solver.IKPositionWeight;
			if (w <= 0f) return;

			Vector3 stretch = Vector3.ClampMagnitude(transform.position - ik.references.head.position, maxStretch);
			stretch *= w;

			stretchDamper = Mathf.Max (stretchDamper, 0f);
			if (stretchDamper > 0f) stretch /= (1f + stretch.magnitude) * (1f + stretchDamper);
			
			for (int i = 0; i < stretchBones.Length; i++) {
				if (stretchBones[i] != null) stretchBones[i].position += stretch / stretchBones.Length;
			}
			
			if (fixHead && ik.solver.IKPositionWeight > 0f) ik.references.head.position = transform.position;
		}

		// Interpolate the solver position of the effector
		private void LerpSolverPosition(IKEffector effector, Vector3 position, float weight) {
			effector.GetNode(ik.solver).solverPosition = Vector3.Lerp(effector.GetNode(ik.solver).solverPosition, position, weight);
		}

		// Solve a simple linear constraint
		private void Solve(ref Vector3 pos1, ref Vector3 pos2, float nominalDistance) {
			Vector3 direction = pos2 - pos1;
			
			float distance = direction.magnitude;
			if (distance == nominalDistance) return;
			if (distance == 0f) return;
			
			float force = 1f;
			
			force *= 1f - nominalDistance / distance;
			
			Vector3 offset = direction * force * 0.5f;
			
			pos1 += offset;
			pos2 -= offset;
		}

		// Clean up the delegates
		void OnDestroy() {
			if (ik != null) {
				ik.solver.OnPreRead -= OnPreRead;
				ik.solver.OnPreIteration -= Iterate;
				ik.solver.OnPostUpdate -= OnPostUpdate;
				ik.solver.OnStoreDefaultLocalState -= OnStoreDefaultLocalState;
				ik.solver.OnFixTransforms -= OnFixTransforms;
			}
		}
	}
}
