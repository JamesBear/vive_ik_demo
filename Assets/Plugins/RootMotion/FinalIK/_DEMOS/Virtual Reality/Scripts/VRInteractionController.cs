using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos {
	
	/// <summary>
	/// Manages OVR interactions using the Interaction System.
	/// </summary>
	[RequireComponent(typeof(InteractionSystem))]
	public class VRInteractionController : MonoBehaviour {
	
		[Tooltip("How long do we need to stare at triggers?")]
		[Range(0f, 10f)] public float triggerTime = 1f;
		
		public InteractionSystem interactionSystem { get; private set; }
		
		// Normalized progress of how long we have been watching a trigger. Not used here, but might be useful for UI scripts
		public float triggerProgress { 
			get {
				if (triggerTime <= 0f) return 0f; // can't divide by 0
				return timer / triggerTime; 
			}
		}
		
		// currentTrigger is not used by this script, but we assign it here so that other scripts, like UI controllers, know what we are looking at
		public InteractionTrigger currentTrigger { get; private set; }
		
		private float timer;
		
		void Start() {
			interactionSystem = GetComponent<InteractionSystem>();
		}
	
		void LateUpdate () {
			// Find the closest InteractionTrigger that the character is in contact with
			int closestTriggerIndex = interactionSystem.GetClosestTriggerIndex();
			
			// Tick the timer if we are looking at the trigger...
			if (CanTrigger(closestTriggerIndex)) {
				timer += Time.deltaTime;
				
				currentTrigger = interactionSystem.triggersInRange[closestTriggerIndex]; // currentTrigger is not used by this script, but we assign it so that other scripts, like UI controllers, know what we are looking at
			} else {
				// ...reset if not
				timer = 0f;
				currentTrigger = null;
				return;
			}
			
			// Its OK now to start the trigger
			if (timer >= triggerTime) {
				interactionSystem.TriggerInteraction(closestTriggerIndex, false);
				timer = 0f;
			}
		}
		
		private bool CanTrigger(int index) {
			// ...if none found, do nothing
			if (index == -1) return false;
			
			// ...if the effectors associated with the trigger are in interaction, do nothing
			if (!interactionSystem.TriggerEffectorsReady(index)) return false;
			
			return true;
		}
	}
}
