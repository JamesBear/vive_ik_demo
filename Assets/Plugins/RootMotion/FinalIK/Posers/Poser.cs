using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK {

	/// <summary>
	/// The base abstract class for all class that are translating a hierarchy of bones to match the translation of bones in another hierarchy.
	/// </summary>
	public abstract class Poser: MonoBehaviour {

		/// <summary>
		/// Reference to the other Transform (should be identical to this one)
		/// </summary>
		public Transform poseRoot;
		/// <summary>
		/// The master weight.
		/// </summary>
		[Range(0f, 1f)] public float weight = 1f;
		/// <summary>
		/// Weight of localRotation matching
		/// </summary>
		[Range(0f, 1f)] public float localRotationWeight = 1f;
		/// <summary>
		/// Weight of localPosition matching
		/// </summary>
		[Range(0f, 1f)] public float localPositionWeight;
		/// <summary>
		/// If true, bones will be fixed to their default local positions and rotations in each Update. This is useful if you don't have animation overwriting the bone Transforms.
		/// </summary>
		public bool fixTransforms = true;

		/// <summary>
		/// Map this instance to the poseRoot.
		/// </summary>
		public abstract void AutoMapping();
		public abstract void StoreDefaultState();
		public abstract void FixTransforms();

		protected virtual void Start() {
			StoreDefaultState();
		}

		protected virtual void Update() {
			if (fixTransforms) FixTransforms();
		}

	}
}
