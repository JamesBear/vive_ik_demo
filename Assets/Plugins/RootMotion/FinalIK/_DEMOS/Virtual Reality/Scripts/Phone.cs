using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos {

	/// <summary>
	/// Managing the phone interaction object.
	/// </summary>
	public class Phone : MonoBehaviour {
	
		[Tooltip("The collider that is used for triggering the picking up interaction.")]
		public Collider pickUpCollider;
		[Tooltip("InteractionObject of the picking up interaction.")]
		public InteractionObject pickUpObject;
		[Tooltip("Root of the phone's display that has all the buttons parented to it.")]
		public GameObject display;
		
		private Transform parent;
	
		// Called by the Interaction System when the picking up interaction is paused at the point where the hand has reached the phone (see InteractionObject events).
		void OnPickUp() {
			// Disable the picking up collider as the phone is already picked up
			pickUpCollider.enabled = false;
			
			// Parent the phone to the character
			parent = transform.parent;
			transform.parent = pickUpObject.lastUsedInteractionSystem.transform;
			
			GetComponent<Rigidbody>().isKinematic = true;
			
			StartCoroutine(EnableDisplay());
		}
		
		// Enable the display and buttons after a short delay after picking up
		private IEnumerator EnableDisplay() {
			yield return new WaitForSeconds(1f);
			
			display.SetActive(true);
		}
		
		// Called by the Interaction System when button 1 has been triggered (see InteractionObject events).
		void OnButton1() {
			// Call your mom!
		}
		
		// Called by the Interaction System when button 2 has been triggered (see InteractionObject events).
		void OnButton2() {
			// When the phone is picked up, the picking up/dropping interaction is paused -> resume to drop the phone.
			pickUpObject.lastUsedInteractionSystem.ResumeAll();
			
			pickUpCollider.enabled = true;
			display.SetActive(false);
		}
		
		// Called by the Interaction System when the phone should be dropped (see InteractionObject events).
		void DropPhone() {
			// Parent the phone back to what it was parented to before picking up
			transform.parent = parent;
			
			GetComponent<Rigidbody>().isKinematic = false;
		}
	}
}
