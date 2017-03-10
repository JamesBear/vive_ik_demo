using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;
using UnityEngine.VR;

namespace RootMotion.Demos {

	/// <summary>
	/// Aiming weapons with the OVR headset.
	/// </summary>
	[RequireComponent(typeof(FullBodyBipedIK))]
	public class VRAimingController : MonoBehaviour {
	
		[System.Serializable]
		public struct Targets {
			public Transform leftHand, rightHand, bendGoalLeftArm, bendGoalRightArm;
			public BoneRotationOffset[] boneRotationOffsets;
		}
		
		[System.Serializable]
		public enum Handedness {
			Right,
			Left
		}
		
		[System.Serializable]
		public class BoneRotationOffset {
			public Transform transform;
			public Vector3 value;
		}
	
		[Header("Component References")]
		public VRAnimatorController animatorController;
		[Tooltip("Which weapon is the character holding at this time?")]
		public WeaponBase currentWeapon;
		
		private Transform cam { get { return animatorController.cam; }}
		private Transform characterController { get { return animatorController.characterController; }}
		
		[Header("Weights")]
		[Tooltip("The master weight of aiming.")]
		[Range(0f, 1f)] public float weight = 1f;
		[Tooltip("The weight of twisting the spine to better hold the weapons")]
		[Range(0f, 1f)] public float spineTwistWeight = 1f;
		
		[Header("Hands")]
		[Tooltip("Which hand holds the weapon?")]
		public Handedness handedness;
		[Tooltip("How far left/right to offset the weapons?")]
		public float sideOffset = 0.1f;
		[Tooltip("Various references and settings for left handed weapons.")]
		public Targets leftHandedTargets;
		[Tooltip("Various references and settings for right handed weapons.")]
		public Targets rightHandedTargets;
		
		[Header("Weapon Positioning")]
		[Tooltip("The Transform that rotates the weapon.")]
		public Transform weaponsPivot;
		[Tooltip("Child of weaponsPivot, parent of all weapons.")]
		public Transform weaponsAnchor;
		[Tooltip("Weapons will inherit motion from that Transform.")]
		public Transform pivotMotionTarget;
		[Tooltip("Speed of various position/rotation interpolations.")]
		public float lerpSpeed = 8f;
		[Tooltip("The smoothing speed of inheriting motion from the pivotMotionTarget.")]
		public float pivotMotionSmoothSpeed = 5f;
		[Tooltip("The weight of inheriting motion from the pivotMotionTarget,")]
		[Range(0f, 1f)] public float pivotMotionWeight = 0.5f;
		[Tooltip("The limit of up/down rotation for the weapons.")]
		[Range(0f, 90f)] public float aimVerticalLimit = 80f;
		[Tooltip("Local Z position of the weapons anchor when the weapon is locked to the camera (while holding RMB).")]
		public float aimZ = 0.05f;
		
		private FullBodyBipedIK ik;
		private float lastWeight;
		private Poser poserLeftHand, poserRightHand;
		private Vector3 pivotRelativePosition;
		private Vector3 weaponsPivotLocalPosition;
		private Vector3 defaultWeaponsAnchorLocalPosition;
		private Vector3 aimVel;
		private Vector3 aimRandom;
		private float x, y;
		private float aimWeight;
		private Vector3 cameraPosition;
		private Vector3 lastCharacterPosition;
		
		void Start() {
			// Find some components.
			ik = GetComponent<FullBodyBipedIK>();
			poserLeftHand = ik.references.leftHand.GetComponent<Poser>();
			poserRightHand = ik.references.rightHand.GetComponent<Poser>();
			
			ik.solver.OnPostUpdate += AfterFBBIK;
			lastWeight = weight;
			
			SetHandedness(handedness);
			
			// Remember some default positions
			defaultWeaponsAnchorLocalPosition = weaponsAnchor.localPosition;
			weaponsPivotLocalPosition = weaponsPivot.localPosition;
			pivotRelativePosition = pivotMotionTarget.InverseTransformPoint(weaponsPivot.position);
			
			cameraPosition = TargetsCameraPosition();
			lastCharacterPosition = characterController.position;
		}
		
		void LateUpdate() {
			// Smooth camera position
			cameraPosition += characterController.position - lastCharacterPosition;
			lastCharacterPosition = characterController.position;
			cameraPosition = Vector3.Lerp(cameraPosition, TargetsCameraPosition(), Time.deltaTime * lerpSpeed);
			
			if (weight <= 0f && lastWeight <= 0f) return;	
			lastWeight = weight;
			
			// Pivot motion
			float pW = animatorController.velocity.magnitude * pivotMotionWeight;
			weaponsPivot.position = Vector3.Lerp(weaponsPivot.position, Vector3.Lerp(weaponsPivot.parent.TransformPoint(weaponsPivotLocalPosition), pivotMotionTarget.TransformPoint(pivotRelativePosition), pW), Time.deltaTime * pivotMotionSmoothSpeed);
			
			// Switch handedness
			if (Input.GetKeyDown(KeyCode.H)) {
				SetHandedness(handedness == Handedness.Right? Handedness.Left: Handedness.Right);
			}
			
			// Keep lerping weapons anchor to its default local position
			// That means we can change it's position if necessary, but be sure that it always smoothly returns as if it was connected to the weaponsPivot with a soft spring	
			defaultWeaponsAnchorLocalPosition.x = handedness == Handedness.Right? sideOffset: -sideOffset;
			weaponsAnchor.localPosition = Vector3.Lerp(weaponsAnchor.localPosition, defaultWeaponsAnchorLocalPosition, Time.deltaTime * lerpSpeed);	
			
			// Recoil
			if (currentWeapon != null) {
				if (Input.GetMouseButtonDown(0)) {
					// Fire the weapon
					currentWeapon.Fire();
				
					// Kick back
					weaponsAnchor.localPosition += currentWeapon.recoilDirection + UnityEngine.Random.insideUnitSphere * currentWeapon.recoilDirection.magnitude * UnityEngine.Random.value * currentWeapon.recoilRandom;
					
					// Rotate up
					aimVel.x -= currentWeapon.recoilAngleVertical + currentWeapon.recoilAngleVertical * UnityEngine.Random.value * currentWeapon.recoilRandom;
					
					// Rotate horizontally
					float hor = currentWeapon.recoilAngleHorizontal * UnityEngine.Random.value;
					if (UnityEngine.Random.value > 0.5f) hor = -hor;
					aimVel.y += hor + hor * UnityEngine.Random.value * currentWeapon.recoilRandom;
				}
			}
			
			// Rotate bones
			foreach (BoneRotationOffset rotationOffset in targets.boneRotationOffsets) {
				rotationOffset.transform.localRotation = Quaternion.Euler(rotationOffset.value * weight) * rotationOffset.transform.localRotation;
			}
			
			// Locking weapon to camera
			bool aim = Input.GetMouseButton(1);
			
			// Weight of locking the weapon to the camera
			float aimWeightTarget = aim? 1f: 0f;
			aimWeight = Mathf.MoveTowards(aimWeight, aimWeightTarget, Time.deltaTime * 3f);
			
			// Rotate the weapon with the mouse (around the weaponsPivot)
			RotateWeapon(Input.GetAxis("Mouse X") * (1f - aimWeight), Input.GetAxis("Mouse Y") * (1f - aimWeight));
			
			// Locking weapon to camera
			if (aim) {
				weaponsPivot.position = Vector3.Lerp(weaponsPivot.position, cameraPosition, aimWeight);
				weaponsAnchor.localPosition = Vector3.Lerp(weaponsAnchor.localPosition, new Vector3(0f, weaponsAnchor.localPosition.y, aimZ), aimWeight);
			
				weaponsPivot.rotation = Quaternion.Lerp(weaponsPivot.rotation, Quaternion.LookRotation(cam.forward), Time.deltaTime * lerpSpeed);
			}
		
			// Push the weapons anchor forward if it's too close to the camera
			Vector3 offset = Vector3.Project(weaponsAnchor.position - TargetsCameraPosition(), cam.forward);
			if (Vector3.Dot(offset, cam.forward) < 0f) weaponsAnchor.position -= offset;
			
			// Effector positions
			ik.solver.leftHandEffector.position = targets.leftHand.position;
			ik.solver.rightHandEffector.position = targets.rightHand.position;
			
			ik.solver.leftHandEffector.positionWeight = weight;
			ik.solver.rightHandEffector.positionWeight = weight;
			
			// Bend goals
			ik.solver.leftArmChain.bendConstraint.bendGoal = targets.bendGoalLeftArm;
			ik.solver.rightArmChain.bendConstraint.bendGoal = targets.bendGoalRightArm;
			
			ik.solver.leftArmChain.bendConstraint.weight = weight;
			ik.solver.rightArmChain.bendConstraint.weight = weight;
			
			// Hand Poser
			poserLeftHand.weight = weight;
			poserRightHand.weight = weight;
			
			// Rotate the character along when the gun is turned too far left or right
			animatorController.RotateCharacter(weaponsAnchor.forward, animatorController.maxViewAngle, weaponsPivot);
			
			// Twist the spine a bit to better hold the weapon
			TwistSpine();
		}
		
		// Rotate the weapon with the mouse (around the weaponsPivot)
		private void RotateWeapon(float horAdd, float vertAdd) {
			Vector3 input = new Vector3(-vertAdd, horAdd, 0f);
			
			// Random sway on the gun
			aimRandom = Vector3.Lerp(aimRandom, UnityEngine.Random.onUnitSphere, Time.deltaTime);
			input += aimRandom * 0.25f;
			
			aimVel = Vector3.Lerp(aimVel, input, Time.deltaTime * 20f);
			
			Vector3 forwardFlat = weaponsPivot.forward;
			forwardFlat.y = 0f;
			
			// Compose the rotation from yaw and pitch so they could be limited individually
			Quaternion v = Quaternion.AngleAxis(aimVel.x, Quaternion.LookRotation(forwardFlat) * Vector3.right);
			Quaternion h = Quaternion.AngleAxis(aimVel.y, Vector3.up);
			
			// Limiting yaw
			Vector3 forward = Vector3.RotateTowards(forwardFlat, v * weaponsPivot.forward, aimVerticalLimit * Mathf.Deg2Rad, 1f);
			
			weaponsPivot.rotation = Quaternion.LookRotation(h * forward, Vector3.up);
		}
		
		// Use left- or right-handed targets?
		private Targets targets {
			get {
				if (handedness == Handedness.Right) return rightHandedTargets;
				return leftHandedTargets;
			}
		}
		
		// Returns the offset position of the camera
		private Vector3 TargetsCameraPosition() {
			float eyeDistance = (InputTracking.GetLocalPosition(VRNode.LeftEye) - InputTracking.GetLocalPosition(VRNode.RightEye)).magnitude * 0.5f;
			Vector3 offset = Vector3.right * eyeDistance;
			if (handedness == Handedness.Left) offset = -offset;
			
			return cam.position + cam.rotation * offset;
		}
		
		// Change from left- to right-handedness and back
		private void SetHandedness(Handedness h) {
			handedness = h;
			
			poserLeftHand.poseRoot = targets.leftHand;
			poserRightHand.poseRoot = targets.rightHand;
			
			poserLeftHand.AutoMapping();
			poserRightHand.AutoMapping();
		}
		
		// Twist the spine a bit to better hold the weapon
		private void TwistSpine() {
			if (spineTwistWeight <= 0f) return;
			
			Vector3 weaponsForward = weaponsAnchor.forward;
			weaponsForward.y = 0f;
			
			Quaternion spineRotation = Quaternion.FromToRotation(transform.forward, weaponsForward);
			
			foreach (BoneRotationOffset rotationOffset in targets.boneRotationOffsets) {
				rotationOffset.transform.rotation = Quaternion.Lerp(Quaternion.identity, spineRotation, (1f / (float)targets.boneRotationOffsets.Length) * spineTwistWeight) * rotationOffset.transform.rotation;
			}
		}
		
		// Rotate hand bones
		void AfterFBBIK() {
			if (weight <= 0f) return;
			
			ik.references.leftHand.rotation = Quaternion.Lerp(ik.references.leftHand.rotation, targets.leftHand.rotation, weight);
			ik.references.rightHand.rotation = Quaternion.Lerp(ik.references.rightHand.rotation, targets.rightHand.rotation, weight);
		}
		
		void OnDestroy() {
			if (ik != null) ik.solver.OnPostUpdate -= AfterFBBIK;
		}
		
		// Clamping Euler angles
		private float ClampAngle (float angle, float min, float max) {
			if (angle < -360) angle += 360;
			if (angle > 360) angle -= 360;
			return Mathf.Clamp (angle, min, max);
		}
	}
}
