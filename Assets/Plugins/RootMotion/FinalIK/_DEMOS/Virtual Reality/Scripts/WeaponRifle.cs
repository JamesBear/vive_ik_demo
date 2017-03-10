using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {
	
	public class WeaponRifle : WeaponBase {
		
		[Header("Shooting")]
		public Transform shootFrom;
		public float range = 300f;
		public LayerMask hitLayers;
		
		[Header("FX")]
		public ParticleSystem muzzleFlash;
		public ParticleSystem muzzleSmoke;
		public Transform bulletHole;
		public ParticleSystem bulletHit;
		public float smokeFadeOutSpeed = 5f;
		
		private float smokeEmission;
		
		// Emit particles, bullets...
		public override void Fire() {
			muzzleFlash.Emit(1);
			smokeEmission = 10f;
			
			RaycastHit hit;
			if (!Physics.Raycast(shootFrom.position, shootFrom.forward, out hit, range, hitLayers)) return;
			
			Vector3 hitPoint = hit.point + hit.normal * 0.01f;
			
			GameObject.Instantiate(bulletHole, hitPoint, Quaternion.LookRotation(-hit.normal));
			
			bulletHit.transform.position = hitPoint;
			bulletHit.Emit(20);
		}
		
		void Update() {
			// Fade out the smoke emitter
			smokeEmission = Mathf.Max(smokeEmission - Time.deltaTime * smokeFadeOutSpeed, 0f);
			
			#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
			muzzleSmoke.enableEmission = smokeEmission > 0f;
			muzzleSmoke.emissionRate = smokeEmission;
			#else
			var emission = muzzleSmoke.emission;
			emission.enabled = smokeEmission > 0f;
			emission.rate = new ParticleSystem.MinMaxCurve(smokeEmission);
			#endif
		}
	}
}
