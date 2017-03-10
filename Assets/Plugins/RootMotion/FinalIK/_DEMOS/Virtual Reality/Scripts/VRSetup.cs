using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR;

namespace RootMotion.Demos {

	/// <summary>
	/// Setting up a 3D character for full body mapping to the Oculus headset.
	/// </summary>
	public class VRSetup : MonoBehaviour {
	
		public Text text;
		public GameObject model;
		public GameObject[] enableOnR;
		public VRCharacterController characterController;
		public bool disableMovement;
		public bool isFinished { get; private set; }
		
		private float moveSpeed;
	
		void Awake() {
#if UNITY_EDITOR
			if (!UnityEditor.PlayerSettings.virtualRealitySupported) Debug.LogWarning("This demo requires enabling 'Virtual Reality Supported' in the Player Settings.");
#endif

			// Deactivate the character and the mirror characters
			foreach (GameObject g in enableOnR) g.SetActive(false);
			
			Cursor.lockState = CursorLockMode.Locked;
			
			if (characterController != null) {
				moveSpeed = characterController.moveSpeed;
				characterController.moveSpeed = 0f;
			}
		}
	
		void LateUpdate() {
			if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
		
			// No rotating the character
			if (!isFinished && characterController != null) {
				characterController.transform.rotation = Quaternion.identity;
			}
			
			// On reset tracker....
			if (Input.GetKeyDown (KeyCode.R)) {
				// Activate the mirrors
				foreach (GameObject g in enableOnR) g.SetActive(true);
				
				InputTracking.Recenter();
				
				text.gameObject.SetActive(false);
				
				if (characterController != null) {
					if (!disableMovement) characterController.moveSpeed = moveSpeed;
					
					// Move the player slightly to provoke OnTriggerEnter with any interaction triggers it might already be in contact with
					characterController.transform.position += Vector3.up * 0.001f;
				}
				
				isFinished = true;
			}
		}
	}
}
