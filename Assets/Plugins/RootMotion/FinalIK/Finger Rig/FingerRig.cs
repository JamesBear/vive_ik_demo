using UnityEngine;
using System.Collections;
using RootMotion;
using System;

namespace RootMotion.FinalIK {

	/// <summary>
	/// Contains a LimbIK solver and some additional logic to handle the finger.
	/// </summary>
	[System.Serializable]
	public class Finger {

		/// <summary>
		/// Master Weight for the finger.
		/// </summary>
		[Tooltip("Master Weight for the finger.")]
		[Range(0f, 1f)] public float weight = 1f;
		/// <summary>
		/// The first bone of the finger.
		/// </summary>
		[Tooltip("The first bone of the finger.")]
		public Transform bone1;
		/// <summary>
		/// The second bone of the finger.
		/// </summary>
		[Tooltip("The second bone of the finger.")]
		public Transform bone2;
		/// <summary>
		/// The (optional) third bone of the finger. This can be ignored for thumbs.
		/// </summary>
		[Tooltip("The (optional) third bone of the finger. This can be ignored for thumbs.")]
		public Transform bone3;
		/// <summary>
		/// The fingertip object. If your character doesn't have tip bones, you can create an empty GameObject and parent it to the last bone in the finger. Place it to the tip of the finger.
		/// </summary>
		[Tooltip("The fingertip object. If your character doesn't have tip bones, you can create an empty GameObject and parent it to the last bone in the finger. Place it to the tip of the finger.")]
		public Transform tip;
		/// <summary>
		/// The IK target (optional, can use IKPosition and IKRotation directly).
		/// </summary>
		[Tooltip("The IK target (optional, can use IKPosition and IKRotation directly).")]
		public Transform target;

		/// <summary>
		/// Has the finger properly initiated (in play mode only)?
		/// </summary>
		public bool initiated { get; private set; }

		/// <summary>
		/// Gets or sets the IK target position if target is not used.
		/// </summary>
		public Vector3 IKPosition { 
			get {
				return solver.IKPosition;
			}
			set {
				solver.IKPosition = value;
			}
		}

		/// <summary>
		/// Gets or sets the IK target rotation if target is not used.
		/// </summary>
		public Quaternion IKRotation {
			get {
				return solver.IKRotation;
			}
			set {
				solver.IKRotation = value;
			}
		}

		/// <summary>
		/// Is this finger setup valid?
		/// </summary>
		public bool IsValid(ref string errorMessage) {
			if (bone1 == null || bone2 == null || tip == null) {
				errorMessage = "One of the bones in the Finger Rig is null, can not initiate solvers.";
				return false;
			}

			return true;
		}

		private IKSolverLimb solver;
		private Quaternion bone3RelativeToTarget;
		private Vector3 bone3DefaultLocalPosition;
		private Quaternion bone3DefaultLocalRotation;

		// Initiates the LimbIK solver
		public void Initiate(Transform hand, int index) {
			initiated = false;

			string errorMessage = string.Empty;
			if (!IsValid(ref errorMessage)) {
				Warning.Log(errorMessage, hand, false);
				return;
			}

			solver = new IKSolverLimb();

			solver.IKPositionWeight = weight;
			solver.bendModifier = IKSolverLimb.BendModifier.Target;
			solver.bendModifierWeight = 1f;

			IKPosition = tip.position;
			IKRotation = tip.rotation;

			if (bone3 != null) {
				bone3RelativeToTarget = Quaternion.Inverse(IKRotation) * bone3.rotation;
				bone3DefaultLocalPosition = bone3.localPosition;
				bone3DefaultLocalRotation = bone3.localRotation;
			}

			solver.SetChain(bone1, bone2, tip, hand);
			solver.Initiate(hand);

			initiated = true;
		}

		// Fix bones to their initial local position and rotation
		public void FixTransforms() {
			if (!initiated) return;

			solver.FixTransforms();

			if (bone3 != null) {
				bone3.localPosition = bone3DefaultLocalPosition;
				bone3.localRotation = bone3DefaultLocalRotation;
			}
		}

		// Update the LimbIK solver and rotate the optional 3rd bone
		public void Update(float masterWeight) {
			if (!initiated) return;

			float w = weight * masterWeight;
			if (w <= 0f) return;

			solver.target = target;
			if (target != null) {
				IKPosition = target.position;
				IKRotation = target.rotation;
			}

			// Rotate the 3rd bone
			if (bone3 != null) {
				if (w >= 1f) {
					bone3.rotation = IKRotation * bone3RelativeToTarget;
				} else {
					bone3.rotation = Quaternion.Lerp(bone3.rotation, IKRotation * bone3RelativeToTarget, w);
				}
			}

			// Update the LimbIK solver
			solver.IKPositionWeight = w;
			solver.Update();
		}
	}

	/// <summary>
	/// Handles IK for a number of Fingers with 3-4 joints.
	/// </summary>
	public class FingerRig : SolverManager {

		/// <summary>
		/// The master weight for all fingers.
		/// </summary>
		[Tooltip("The master weight for all fingers.")]
		[Range(0f, 1f)]
		public float weight = 1f;

		/// <summary>
		/// The array of Fingers.
		/// </summary>
		public Finger[] fingers = new Finger[0];

		/// <summary>
		/// Has the rig properly initiated (in play mode only)?
		/// </summary>
		public bool initiated { get; private set; }

		/// <summary>
		/// Is this rig valid?
		/// </summary>
		public bool IsValid(ref string errorMessage) {
			foreach (Finger finger in fingers) {
				if (!finger.IsValid(ref errorMessage)) return false;
			}
			return true;
		}

		/// <summary>
		/// Attempts to automatically fill in the Finger bones.
		/// </summary>
		[ContextMenu("Auto-detect")]
		public void AutoDetect() {
			fingers = new Finger[0];

			for (int i = 0; i < transform.childCount; i++) {
				Transform[] potentialFinger = new Transform[0];
				AddChildrenRecursive(transform.GetChild(i), ref potentialFinger);

				if (potentialFinger.Length == 3 || potentialFinger.Length == 4) {
					Finger finger = new Finger();
					finger.bone1 = potentialFinger[0];
					finger.bone2 = potentialFinger[1];
					if (potentialFinger.Length == 3) {
						finger.tip = potentialFinger[2];
					} else {
						finger.bone3 = potentialFinger[2];
						finger.tip = potentialFinger[3];
					}

					finger.weight = 1f;

					Array.Resize(ref fingers, fingers.Length + 1);
					fingers[fingers.Length - 1] = finger;
				}
			}
		}

		/// <summary>
		/// Adds a finger in run-time.
		/// </summary>
		public void AddFinger(Transform bone1, Transform bone2, Transform bone3, Transform tip, Transform target = null) {
			Finger finger = new Finger();
			finger.bone1 = bone1;
			finger.bone2 = bone2;
			finger.bone3 = bone3;
			finger.tip = tip;
			finger.target = target;

			Array.Resize(ref fingers, fingers.Length + 1);
			fingers[fingers.Length - 1] = finger;

			initiated = false;
			finger.Initiate(transform, fingers.Length - 1);
			if (fingers[fingers.Length - 1].initiated) initiated = true;
		}

		/// <summary>
		/// Removes a finger in runtime.
		/// </summary>
		public void RemoveFinger(int index) {
			if (index < 0f || index >= fingers.Length) {
				Warning.Log("RemoveFinger index out of bounds.", transform);
				return;
			}

			if (fingers.Length == 1) {
				fingers = new Finger[0];
				return;
			}

			Finger[] newFingers = new Finger[fingers.Length - 1];
			int added = 0;

			for (int i = 0; i < fingers.Length; i++) {
				if (i != index) {
					newFingers[added] = fingers[i];
					added ++;
				}
			}

			fingers = newFingers;
		}

		// Adds child Transforms of the 'parent' to the 'array' recursively only if each Transform has a single child.
		private void AddChildrenRecursive(Transform parent, ref Transform[] array) {
			Array.Resize(ref array, array.Length + 1);
			array[array.Length - 1] = parent;

			if (parent.childCount != 1) return;

			AddChildrenRecursive(parent.GetChild(0), ref array);
		}

		protected override void InitiateSolver() {
			initiated = true;

			for (int i = 0; i < fingers.Length; i++) {
				fingers[i].Initiate(transform, i);
				if (!fingers[i].initiated) initiated = false;
			}
		}

		public void UpdateFingerSolvers() {
			if (weight <= 0f) return;

			foreach (Finger finger in fingers) {
				finger.Update(weight);
			}
		}

		public void FixFingerTransforms() {
			foreach (Finger finger in fingers) {
				finger.FixTransforms();
			}
		}

		protected override void UpdateSolver() {
			UpdateFingerSolvers();
		}

		protected override void FixTransforms() {
			FixFingerTransforms();
		}
	}
}
