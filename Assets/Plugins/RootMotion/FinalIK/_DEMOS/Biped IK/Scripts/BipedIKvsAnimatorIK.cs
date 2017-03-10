using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos {

	/// <summary>
	/// Demo script that shows how BipedIK performs compared to the built-in Animator IK
	/// </summary>
	public class BipedIKvsAnimatorIK: MonoBehaviour {
		
		public Animator animator;
		public BipedIK bipedIK;

		// Look At
		public Transform lookAtTargetBiped, lookAtTargetAnimator;
		
		public float lookAtWeight = 1f;
		public float lookAtBodyWeight = 1f;
		public float lookAtHeadWeight = 1f;
		public float lookAtEyesWeight = 1f;
		public float lookAtClampWeight = 0.5f;
		public float lookAtClampWeightHead = 0.5f;
		public float lookAtClampWeightEyes = 0.5f;

		// Foot
		public Transform footTargetBiped, footTargetAnimator;
		public float footPositionWeight = 0f;
		public float footRotationWeight = 0f;

		// Hand
		public Transform handTargetBiped, handTargetAnimator;
		public float handPositionWeight = 0f;
		public float handRotationWeight = 0f;

		void OnAnimatorIK(int layer) {
			animator.transform.rotation = bipedIK.transform.rotation;
			Vector3 offset = animator.transform.position - bipedIK.transform.position;

			// Look At
			lookAtTargetAnimator.position = lookAtTargetBiped.position + offset;
			
			bipedIK.SetLookAtPosition(lookAtTargetBiped.position);
			bipedIK.SetLookAtWeight(lookAtWeight, lookAtBodyWeight, lookAtHeadWeight, lookAtEyesWeight, lookAtClampWeight, lookAtClampWeightHead, lookAtClampWeightEyes);
			
			animator.SetLookAtPosition(lookAtTargetAnimator.position);
			animator.SetLookAtWeight(lookAtWeight, lookAtBodyWeight, lookAtHeadWeight, lookAtEyesWeight, lookAtClampWeight);

			// Foot
			footTargetAnimator.position = footTargetBiped.position + offset;
			footTargetAnimator.rotation = footTargetBiped.rotation;

			bipedIK.SetIKPosition(AvatarIKGoal.LeftFoot, footTargetBiped.position);
			bipedIK.SetIKRotation(AvatarIKGoal.LeftFoot, footTargetBiped.rotation);
			bipedIK.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footPositionWeight);
			bipedIK.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footRotationWeight);

			animator.SetIKPosition(AvatarIKGoal.LeftFoot, footTargetAnimator.position);
			animator.SetIKRotation(AvatarIKGoal.LeftFoot, footTargetAnimator.rotation);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footPositionWeight);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footRotationWeight);

			// Hand
			handTargetAnimator.position = handTargetBiped.position + offset;
			handTargetAnimator.rotation = handTargetBiped.rotation;
			
			bipedIK.SetIKPosition(AvatarIKGoal.LeftHand, handTargetBiped.position);
			bipedIK.SetIKRotation(AvatarIKGoal.LeftHand, handTargetBiped.rotation);
			bipedIK.SetIKPositionWeight(AvatarIKGoal.LeftHand, handPositionWeight);
			bipedIK.SetIKRotationWeight(AvatarIKGoal.LeftHand, handRotationWeight);
			
			animator.SetIKPosition(AvatarIKGoal.LeftHand, handTargetAnimator.position);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, handTargetAnimator.rotation);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, handPositionWeight);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, handRotationWeight);
		}
	}
}
