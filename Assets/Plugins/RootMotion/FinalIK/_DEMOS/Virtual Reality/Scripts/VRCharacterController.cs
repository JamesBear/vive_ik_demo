using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {

	[RequireComponent(typeof(CharacterController))]
	public class VRCharacterController : MonoBehaviour {
	
		public float moveSpeed = 2f;
		public float rotationSpeed = 2f;
		[Range(0f, 180f)] public float rotationRatchet = 45f;
		public KeyCode ratchetRight = KeyCode.E;
		public KeyCode ratchetLeft = KeyCode.Q;
		public Transform forwardDirection;
	
		private CharacterController characterController;
	
		void Awake() {
			characterController = GetComponent<CharacterController>();
			if (forwardDirection == null) forwardDirection = transform;
		}
	
		void Update () {
			Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
			
			// Find the forward direction
			Vector3 f = forwardDirection.forward;
			f.y = 0f;
			
			characterController.SimpleMove(Quaternion.LookRotation(f) * Vector3.ClampMagnitude(input, 1f) * moveSpeed);
			
			if (Input.GetKeyDown(ratchetRight)) transform.rotation = Quaternion.Euler(0f, rotationRatchet, 0f) * transform.rotation;
			else if (Input.GetKeyDown(ratchetLeft)) transform.rotation = Quaternion.Euler(0f, -rotationRatchet, 0f) * transform.rotation;
			
			transform.rotation = Quaternion.Euler(0f, Input.GetAxis("Mouse X") * rotationSpeed, 0f) * transform.rotation;
		}
	}
}
