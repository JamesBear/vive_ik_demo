using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UniqueShadow : MonoBehaviour {
	[System.Serializable] public class FocusSetup {
		public bool autoFocus;
		public float autoFocusRadiusBias;
		public Transform target;
		public Vector3 offset;
		public float radius = 1f;
		public float depthBias = 0.0005f;
		public float sceneCaptureDistance = 50f;
	}
		
	[HideInInspector] public Shader uniqueShadowDepthShader;
	
	public enum Dimension {
		x256	= 256,
		x512	= 512,
		x1024	= 1024,
		x2048	= 2048,
		x4096	= 4096,
		x8192	= 8192,
	}

	public Dimension shadowMapSize = Dimension.x2048;
	public float cullingDistance = 15f;
	public LayerMask inclusionMask;
	public bool useSceneCapture = true;

	public float blockerSearchDistance = 24f;
	public float blockerDistanceScale = 1f;
	public float lightNearSize = 4f;
	public float lightFarSize = 22f;
	public float fallbackFilterWidth = 6f;

	public int startFocus;
	public FocusSetup[] shadowFoci;

	int m_downscale = 0;

	int m_activeFocus = -1;
	Light m_lightSource;
	List<Material> m_materialInstances;

	static Texture2D	ms_shadowTextureFakePoint;
	static int			ms_shadowMatrixID, ms_shadowTextureID;
	//static Plane[]		ms_cameraPlanes = new Plane[6]; 

	RenderTexture m_shadowTexture;
	Matrix4x4 m_shadowMatrix;
	Camera m_shadowCamera;
	Matrix4x4 m_shadowSpaceMatrix;

	public void SetDownscale(int downscale) {
		m_downscale = downscale;

		if(m_shadowTexture) {
			ReleaseTarget();
			AllocateTarget();
		}
	}

	void Awake() {
		if(!ms_shadowTextureFakePoint) {
			ms_shadowTextureFakePoint = new Texture2D(1, 1, TextureFormat.Alpha8, false, true);
			ms_shadowTextureFakePoint.filterMode = FilterMode.Point;
			ms_shadowTextureFakePoint.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
			ms_shadowTextureFakePoint.Apply(false, true);

			ms_shadowMatrixID = Shader.PropertyToID("u_UniqueShadowMatrix");
			ms_shadowTextureID = Shader.PropertyToID("u_UniqueShadowTexture");
		}

		EnsureLightSource();

		m_shadowMatrix = Matrix4x4.identity;
		var shadowCameraGO = new GameObject("#> _Shadow Camera < " + this.name);
		shadowCameraGO.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;
		m_shadowCamera = shadowCameraGO.AddComponent<Camera>();
		m_shadowCamera.renderingPath = RenderingPath.Forward;
		m_shadowCamera.clearFlags = CameraClearFlags.Depth;
		m_shadowCamera.depthTextureMode = DepthTextureMode.None;
		m_shadowCamera.useOcclusionCulling = false;
		m_shadowCamera.cullingMask = useSceneCapture ? (LayerMask)~0 : inclusionMask;
		m_shadowCamera.orthographic = true;
		m_shadowCamera.depth = -100;
		m_shadowCamera.aspect = 1f;
		m_shadowCamera.SetReplacementShader(uniqueShadowDepthShader, "RenderType");
		m_shadowCamera.enabled = false;
		
		SetFocus(startFocus);

		m_materialInstances = new List<Material>();
		var materialMap = new Dictionary<Material, Material>();
		foreach(var r in GetComponentsInChildren<Renderer>()) {
			if(!r.receiveShadows)
				continue;

			bool hadMaterials = false;
			var sharedMaterials = r.sharedMaterials;
			for(int i  = 0, n = sharedMaterials.Length; i < n; ++i) {
				var m = sharedMaterials[i];

				Material mi = null;
				if(!materialMap.TryGetValue(m, out mi)) {
					materialMap[m] = mi = new Material(m);
					mi.name = m.name + " (uniq)";
					mi.shaderKeywords = m.shaderKeywords;
					mi.renderQueue = m.renderQueue;
					SetStaticShaderUniforms(mi);
					m_materialInstances.Add(mi);
				}
				sharedMaterials[i] = mi;
				hadMaterials = true;
			}

			if(hadMaterials)
				r.sharedMaterials = sharedMaterials;
		}

		if(m_materialInstances.Count > 0) {
			var mesh = new Mesh();
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
			mesh.hideFlags = HideFlags.HideAndDontSave;
			var mf = gameObject.AddComponent<MeshFilter>();
			mf.sharedMesh = mesh;
			var mr = gameObject.AddComponent<MeshRenderer>();
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.useLightProbes = false;
		}
	}

	void OnEnable() {
		AllocateTarget();
		ToggleUniqueVariant();
	}

	void OnDisable() {
		ReleaseTarget();
		ToggleUniqueVariant();
	}

	void OnDestroy() {
		var mf = GetComponent<MeshFilter>();
		if(mf)
			Object.DestroyImmediate(mf.sharedMesh);
		
		if(m_shadowCamera)
			Object.DestroyImmediate(m_shadowCamera.gameObject);
	}

	void OnValidate() {
		if(!Application.isPlaying || !m_shadowCamera)
			return;

		ReleaseTarget();
		AllocateTarget();

		if(m_materialInstances != null)
			for(int i = 0, n = m_materialInstances.Count; i < n; ++i)
				SetStaticShaderUniforms(m_materialInstances[i]);

		m_shadowCamera.cullingMask = useSceneCapture ? (LayerMask)~0 : inclusionMask;

		SetFocus(m_activeFocus >= 0 ? m_activeFocus : startFocus);
		ToggleUniqueVariant();
	}

	void AllocateTarget() {
		m_shadowTexture = new RenderTexture((int)shadowMapSize >> m_downscale, (int)shadowMapSize >> m_downscale, 16, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear);
		m_shadowTexture.filterMode = FilterMode.Bilinear;
		m_shadowTexture.useMipMap = false;
		m_shadowTexture.generateMips = false;
		m_shadowCamera.targetTexture =  m_shadowTexture;
	}

	void ReleaseTarget() {
		m_shadowCamera.targetTexture = null;
		Object.DestroyImmediate(m_shadowTexture);
		m_shadowTexture = null;
	}

	void ToggleUniqueVariant() {
		bool isEnable = m_lightSource;

		for(int i = 0, n = m_materialInstances.Count; i < n; ++i) {
			var m = m_materialInstances[i];

			m.DisableKeyword("UNIQUE_SHADOW");
			m.DisableKeyword("UNIQUE_SHADOW_LIGHT_COOKIE");

			if(isEnable && m_shadowTexture) {
				if(m_lightSource.cookie)
					m.EnableKeyword("UNIQUE_SHADOW_LIGHT_COOKIE");
				else
					m.EnableKeyword("UNIQUE_SHADOW");
			}				
		}
	}

	void SetStaticShaderUniforms(Material m) {
		m.SetTexture("u_UniqueShadowTextureFakePoint", ms_shadowTextureFakePoint);

		// We want the same 'softness' regardless of texture resolution.
		var texelsInMap = (float)(int)shadowMapSize;
		var relativeTexelSize = texelsInMap / 2048f;

		m.SetVector("u_UniqueShadowFilterWidth", new Vector2(1f / (float)(int)shadowMapSize, 1f / (float)(int)shadowMapSize) * fallbackFilterWidth * relativeTexelSize);

		var uniqueShadowBlockerWidth = relativeTexelSize * blockerSearchDistance / texelsInMap;
		m.SetVector("u_UniqueShadowBlockerWidth", Vector4.one * uniqueShadowBlockerWidth);

		// This needs to run each frame if we start using multiple foci.
		var focus = shadowFoci[m_activeFocus];
		var uniqueShadowBlockerDistanceScale = blockerDistanceScale * focus.radius * 0.5f / 10f; // 10 samples in shader
		m.SetFloat("u_UniqueShadowBlockerDistanceScale", uniqueShadowBlockerDistanceScale);
		
		var uniqueShadowLightWidth = new Vector2(lightNearSize, lightFarSize) * relativeTexelSize / texelsInMap;
		m.SetVector("u_UniqueShadowLightWidth", uniqueShadowLightWidth);
	}

	bool EnsureLightSource() {
		bool hadValidLight = m_lightSource;
		bool hadCookie = m_lightSource && m_lightSource.cookie;
		m_lightSource = UniqueShadowSun.instance;

		// Only capture shadows from the light's culling mask.
		if(useSceneCapture && m_lightSource && m_shadowCamera)
			m_shadowCamera.cullingMask = m_lightSource.cullingMask;

		return hadValidLight != m_lightSource || hadCookie != (m_lightSource && m_lightSource.cookie);
	}

	void UpdateAutoFocus(FocusSetup focus) {
		if(!focus.autoFocus)
			return;

		var targetPos = focus.target.position + focus.target.right * focus.offset.x
			+ focus.target.up * focus.offset.y + focus.target.forward * focus.offset.z;

		var self = GetComponent<Renderer>();
		var bounds = new Bounds(targetPos, Vector3.one * 0.1f);
		foreach(var r in GetComponentsInChildren<Renderer>())
			if(r != self)
				bounds.Encapsulate(r.bounds);

		focus.offset = bounds.center - focus.target.position;
		focus.radius = focus.autoFocusRadiusBias + bounds.extents.magnitude;
	}

	void SetFocus(int idx) {
		if(idx < 0 || idx >= shadowFoci.Length) {
			Debug.LogError("Invalid active focus: " + m_activeFocus);
			return;
		}
		
		m_activeFocus = idx;
		
		var focus = shadowFoci[m_activeFocus];
		UpdateAutoFocus(focus);

		m_shadowCamera.orthographicSize = focus.radius;
		m_shadowCamera.nearClipPlane = useSceneCapture ? -focus.sceneCaptureDistance : 0f;
		m_shadowCamera.farClipPlane = focus.radius * 2f;
		m_shadowCamera.projectionMatrix
			= GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-focus.radius, focus.radius, -focus.radius, focus.radius, 0f, focus.radius * 2f), false);

		var isD3D9 = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D9;
		var isD3D = isD3D9 || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
		float to = isD3D9 ? 0.5f / (float)(int)shadowMapSize : 0f;
		float zs = isD3D ? 1f : 0.5f, zo = isD3D ? 0f : 0.5f;
		float db = -focus.depthBias;
		m_shadowSpaceMatrix.SetRow(0, new Vector4(0.5f, 0.0f, 0.0f, 0.5f + to));
		m_shadowSpaceMatrix.SetRow(1, new Vector4(0.0f, 0.5f, 0.0f, 0.5f + to));
		m_shadowSpaceMatrix.SetRow(2, new Vector4(0.0f, 0.0f,   zs,   zo + db));
		m_shadowSpaceMatrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
	}

	void UpdateFocus() {
		var focus = shadowFoci[m_activeFocus];
		
		var targetPos = focus.target.position + focus.target.right * focus.offset.x
			+ focus.target.up * focus.offset.y + focus.target.forward * focus.offset.z;
		var lightDir = m_lightSource.transform.forward;
		var lightOri = m_lightSource.transform.rotation;
				
		m_shadowCamera.transform.position = targetPos - lightDir * focus.radius;
		m_shadowCamera.transform.rotation = lightOri;

		//TODO: Texel snap? (probably doesn't matter too much since the targets are always animated)

		var shadowViewMat = m_shadowCamera.worldToCameraMatrix;
		var shadowProjMat = GL.GetGPUProjectionMatrix(m_shadowCamera.projectionMatrix, false);
		m_shadowMatrix = m_shadowSpaceMatrix * shadowProjMat * shadowViewMat;
	}

	bool CheckVisibility(Camera cam) {
		var focus = shadowFoci[m_activeFocus];
		UpdateAutoFocus(focus);

		var targetPos = focus.target.position + focus.target.right * focus.offset.x
			+ focus.target.up * focus.offset.y + focus.target.forward * focus.offset.z;
		var bounds = new Bounds(targetPos, Vector3.one * focus.radius * 2f);

		return (targetPos - cam.transform.position).sqrMagnitude < (cullingDistance * cullingDistance)
			&& GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(/*ms_cameraPlanes,*/ cam), bounds);
	}

	bool CheckCamera(Camera cam) {
		if(cam == Camera.main)
			return true;

#if UNITY_EDITOR
		if(UnityEditor.SceneView.currentDrawingSceneView)
			if(UnityEditor.SceneView.currentDrawingSceneView.camera == cam)
				return true;
#endif

		return false;
	}
	
	void OnWillRenderObject() {
		if(EnsureLightSource())
			ToggleUniqueVariant();

		if(!m_lightSource)
			return;

		var cam = Camera.current;
		if(!CheckCamera(cam))
			return;

		if(!CheckVisibility(cam))
			return;

		UpdateFocus();

		var shadowDistance = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = 0f;

		m_shadowCamera.Render();

		QualitySettings.shadowDistance = shadowDistance;

		for(int i = 0, n = m_materialInstances.Count; i < n; ++i) {
			var m = m_materialInstances[i];
			m.SetTexture(ms_shadowTextureID, m_shadowTexture);
			m.SetMatrix(ms_shadowMatrixID, m_shadowMatrix);
		}
	}
	
	void OnDrawGizmosSelected() {
		if(shadowFoci == null)
			return;

		foreach(var f in shadowFoci) {
			if(f.target == null)
				continue;

			Gizmos.color = f.autoFocus ? Color.cyan : Color.green;

			var p = f.target.position + f.target.right * f.offset.x	+ f.target.up * f.offset.y + f.target.forward * f.offset.z;
			Gizmos.DrawWireSphere(p, f.radius + (f.autoFocus ? f.autoFocusRadiusBias : 0f));
		}
	}
}