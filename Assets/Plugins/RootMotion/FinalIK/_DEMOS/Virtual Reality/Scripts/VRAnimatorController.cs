using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;
using UnityEngine.VR;

namespace RootMotion.Demos {

	/// <summary>
	/// Handles animation of a full body OVR character.
	/// </summary>
	[RequireComponent(typeof(Animator))]
	public class VRAnimatorController : MonoBehaviour {
	
		[Header("Component References")]
		public VRSetup oculusSetup;
		public Transform characterController;
		public Transform cam;
		
		[Header("Main Properties")]
		[Tooltip("Offset of the VR camera")]
		public Vector3 cameraOffset;
		[Tooltip("How long to accelerate to target velocity using SmoothDamp?")]
		public float smoothAccelerationTime = 0.2f;
		[Tooltip("How fast to accelerate liearily? If this is zero, will only use smooth acceleration.")]
		public float linearAcceleration = 2f;
		[Tooltip("Rotate the character along if camera is looking too far left/right.")]
		public float maxViewAngle = 60f;
		[Tooltip("The master speed of locomotion animations.")]
		public float locomotionSpeed = 1f;
		
		public Vector3 velocity { get; private set; }
		
		private Animator animator;
		private Vector3 velocityC;
		private bool rootCorrection;
		private Vector3 playerVelocity;
		private Vector3 playerLastPosition;
		private FixFeet fixFeet;
		private Transform cameraPivot;
		
		void Start() {
			animator = GetComponent<Animator> ();
			fixFeet = gameObject.AddComponent<FixFeet>();
			
			animator.SetBool ("IsStrafing", true);
			playerLastPosition = characterController.position;
			
			cameraPivot = new GameObject().transform;
			cameraPivot.name = "Camera Pivot";
			cameraPivot.position = characterController.position + characterController.rotation * cameraOffset;
			cameraPivot.rotation = characterController.rotation;
			cameraPivot.parent = characterController;
			cam.parent = cameraPivot;
		}
		
		void Update() {
			// While in setup
			if (!oculusSetup.isFinished) {
				if (fixFeet != null) fixFeet.weight = 1f;
				velocity = Vector3.zero;
				animator.SetFloat("Right", 0f);
				animator.SetFloat("Forward", 0f);
				return;
			}
			
			RotateCharacter(cam.forward, maxViewAngle, cameraPivot);
			
			Vector3 velocityTarget = GetVelocityTarget();
			
			// Interpolate locomotion velocity
			velocity = Vector3.MoveTowards (velocity, velocityTarget, Time.deltaTime * linearAcceleration);
			velocity = Vector3.SmoothDamp (velocity, velocityTarget, ref velocityC, smoothAccelerationTime);
			
			// Fix local position to player controller
			transform.position = new Vector3(characterController.position.x, transform.position.y, characterController.position.z);
			
			// Fixing feet while standing so they don't sway around because of keyframe reduction/compression or retargeting problems.
			if (fixFeet != null) {
				//float fixFeetWeightTarget = rootCorrectionEnabled || velocity != Vector3.zero? 0f: 1f;
				float fixFeetWeightTarget = velocity == Vector3.zero? 1f: 0f;
				fixFeet.weight = Mathf.MoveTowards(fixFeet.weight, fixFeetWeightTarget, Time.deltaTime * 3f);
			}
			
			// Update Animator
			animator.SetFloat ("Right", velocity.x);
			animator.SetFloat ("Forward", velocity.z);
		}
		
		// Which way to animate locomotion?
		private Vector3 GetVelocityTarget() {
			Vector3 v = Vector3.zero;
			
			// Towards player velocity
			playerVelocity = (characterController.position - playerLastPosition) / Time.deltaTime;
			playerLastPosition = characterController.position;
			v += Quaternion.Inverse(transform.rotation) * playerVelocity * locomotionSpeed;
			
			// We can continue adding modifiers here...
			
			return v;
		}
		
		// Rotating the root of the character if it is past maxAngle from the camera forward
		public void RotateCharacter(Vector3 forward, float maxAngle, Transform fix = null) {
			if (maxAngle >= 180f) return;
			
			Quaternion fixRotation = fix != null? fix.rotation: Quaternion.identity;
			
			// If no angular difference is allowed, just rotate the character to the flattened camera forward
			if (maxAngle <= 0f) {
				characterController.rotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.z));
				if (fix != null) fix.rotation = fixRotation;
				return;
			}
			
			// Get camera forward in the character's rotation space
			Vector3 camRelative = characterController.InverseTransformDirection(forward);
			
			// Get the angle of the camera forward relative to the character forward
			float angle = Mathf.Atan2(camRelative.x, camRelative.z) * Mathf.Rad2Deg;
			
			// Making sure the angle does not exceed maxangle
			if (Mathf.Abs(angle) > Mathf.Abs(maxAngle)) {
				float a = angle - maxAngle;
				if (angle < 0f) a = angle + maxAngle;
				characterController.rotation = Quaternion.AngleAxis(a, characterController.up) * characterController.rotation;
			}
			
			if (fix != null) {
				fix.rotation = fixRotation;
			}
		}
	}
}
