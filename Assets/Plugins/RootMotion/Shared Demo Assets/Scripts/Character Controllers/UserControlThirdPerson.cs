using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {

	/// <summary>
	/// User input for a third person character controller.
	/// </summary>
	public class UserControlThirdPerson : MonoBehaviour {

		// Input state
		public struct State {
			public Vector3 move;
			public Vector3 lookPos;
			public bool crouch;
			public bool jump;
			public int actionIndex;
		}

		public bool walkByDefault;        // toggle for walking state
		public bool canCrouch = true;
		public bool canJump = true;

		public State state = new State();			// The current state of the user input

		protected Transform cam;                    // A reference to the main camera in the scenes transform

		void Start () {
			// get the transform of the main camera
			cam = Camera.main.transform;
		}

		protected virtual void Update () {
			// read inputs
			state.crouch = canCrouch && Input.GetKey(KeyCode.C);
			state.jump = canJump && Input.GetButton("Jump");

			float h = Input.GetAxisRaw("Horizontal");
			float v = Input.GetAxisRaw("Vertical");
			
			// calculate move direction
			state.move = Quaternion.LookRotation(new Vector3(cam.forward.x, 0f, cam.forward.z).normalized) * new Vector3(h, 0f, v).normalized;

			bool walkToggle = Input.GetKey(KeyCode.LeftShift);

			// We select appropriate speed based on whether we're walking by default, and whether the walk/run toggle button is pressed:
			float walkMultiplier = (walkByDefault ? walkToggle ? 1 : 0.5f : walkToggle ? 0.5f : 1);

			state.move *= walkMultiplier;
			
			// calculate the head look target position
			state.lookPos = transform.position + cam.forward * 100f;
		}
	}
}

