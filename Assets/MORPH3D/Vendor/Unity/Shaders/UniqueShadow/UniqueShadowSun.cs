using UnityEngine;
using System.Collections;

public class UniqueShadowSun : MonoBehaviour {
	public static Light instance;

	Light m_lightSource;

	void Awake() {
		m_lightSource = GetComponent<Light>();
		if(!m_lightSource)
			Debug.LogErrorFormat("No light component found in UniqueShadowSun '{0}!", name);
	}

	void OnEnable() {
		if(instance) {
			Debug.LogErrorFormat("Not setting 'UniqueShadowSun.instance' because '{0}' is already active!", instance.name);
			return;
		}

		instance = m_lightSource;
	}

	void OnDisable() {
		if(instance == null) {
			Debug.LogErrorFormat("'UniqueShadowSun.instance' is already null when disabling '{0}'!", this.name);
			return;
		}
	
		if(instance != m_lightSource) {
			Debug.LogErrorFormat("Not UNsetting 'UniqueShadowSun.instance' because it points to someone else '{0}'!", instance.name);
			return;
		}

		instance = null;
	}
}
