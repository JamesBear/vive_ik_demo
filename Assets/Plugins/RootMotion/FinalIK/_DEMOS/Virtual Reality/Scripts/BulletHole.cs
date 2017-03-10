using UnityEngine;
using System.Collections;

public class BulletHole : MonoBehaviour {

	public float lifeTime = 15f;

	void Start () {
		StartCoroutine(DestroyDelayed());
	}
	
	private IEnumerator DestroyDelayed() {
		yield return new WaitForSeconds(lifeTime);
		
		Destroy(gameObject);
	}
}
