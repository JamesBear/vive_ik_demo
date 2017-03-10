using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {
	
	/// <summary>
	/// User input for an AI controlled character controller.
	/// </summary>
	public class UserControlAI : UserControlThirdPerson {

		public Transform moveTarget;
		public float stoppingDistance = 0.5f;
		public float stoppingThreshold = 1.5f;

		protected override void Update () {
			float moveSpeed = walkByDefault? 0.5f: 1f;

			Vector3 direction = moveTarget.position - transform.position;
			direction.y = 0f;

			float sD = state.move != Vector3.zero? stoppingDistance: stoppingDistance * stoppingThreshold;

			state.move = direction.magnitude > sD? direction.normalized * moveSpeed: Vector3.zero;
		}
	}
}

