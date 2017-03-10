using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {

	/// <summary>
	/// Base class for all Weapons so they could be fired regardless of type
	/// </summary>
	public abstract class WeaponBase : MonoBehaviour {
	
		[Header("Recoil")]
		public Vector3 recoilDirection = -Vector3.forward;
		public float recoilAngleVertical = 1f;
		public float recoilAngleHorizontal = 1f;
		public float recoilRandom = 0.2f;
		
		public abstract void Fire();
	}
}
