using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos {
	
	/// <summary>
	/// Animates a character to match the animation of it's identical duplicate, the target.
	/// </summary>
	public class Mirror : MonoBehaviour {
	
		public Transform target;
	
		private Transform[] children = new Transform[0];
		private Transform[] targetChildren = new Transform[0];
		private FullBodyBipedIK ik;
	
		void Start() {
			if (!target.gameObject.activeInHierarchy)
							return;
			if (targetChildren.Length > 0)
							return;
	
	
			children = GetComponentsInChildren<Transform> ();
			targetChildren = target.GetComponentsInChildren<Transform> ();
			
			ik = target.GetComponent<FullBodyBipedIK> ();
			if (ik != null) ik.solver.OnPostUpdate += OnPostFBBIK;
		}
	
		// Called after FBBIK has solved
		private void OnPostFBBIK() {
			for (int i = 1; i < children.Length; i++) {
				for (int c = 1; c < targetChildren.Length; c++) {
					if (children[i].name == targetChildren[c].name) {
						children[i].localPosition = targetChildren[c].localPosition;
						children[i].localRotation = targetChildren[c].localRotation;
						break;
					}
				}
			}
		}
	
		void OnDestroy() {
			if (ik != null) ik.solver.OnPostUpdate -= OnPostFBBIK;
		}
	}
}
