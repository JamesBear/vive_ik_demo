using UnityEngine;
using System.Collections.Generic;

public class HairRenderer : MonoBehaviour {
	public enum DebugMode { DBG_NONE, DBG_OCCLUSION, DBG_GRAYMASK, DBG_MASKEDALBEDO, DBG_SPECULAR, DBG_LIGHTING, DBG_FLOW }

	public enum Mode { Original, StaticHeightBased, DynamicRadialDistance };
	
	public Renderer		sourceRenderer;
	public Mode			mode = Mode.StaticHeightBased;
	public bool			useOpaquePass = true;
	public float		opaqueAlphaRef = 0.80f;
	public bool			frontWriteDepth = false;
	public float		frontBackAlphaRef = 0.01f;
	public DebugMode	debugMode = DebugMode.DBG_NONE;

	// DynamicRadialDistance not really supported these days.
	[HideInInspector] public Transform[]headSpheres;
	[HideInInspector] public Transform	headShell;
	[HideInInspector] public float		sortDistanceScale = 1f;
	
	Mesh		m_sourceMesh;
	Mesh		m_sortedMesh;
	int[]		m_sortedIndices;
	MeshFilter	m_meshFilter;
	MeshSorter	m_meshSorter;
	Material	m_materialOpaque;
	Material	m_materialBack;
	Material	m_materialFront;

	void Awake () {
		debugMode = DebugMode.DBG_NONE;

		gameObject.layer = sourceRenderer.gameObject.layer;
		if(sourceRenderer is MeshRenderer)
			m_sourceMesh = ((MeshRenderer)sourceRenderer).GetComponent<MeshFilter>().sharedMesh;
		else if(sourceRenderer is SkinnedMeshRenderer)
			m_sourceMesh = ((SkinnedMeshRenderer)sourceRenderer).sharedMesh;
		else
			Debug.LogError("Invalid source renderer type");

		CreateMeshData();
		CreateMaterials();

		UpdateMaterials();
		UpdateKeywords();
		SelectMesh();
	}

	void CreateMeshData() {
		var vertices = m_sourceMesh.vertices;
		var uvs = m_sourceMesh.uv;
		var colors = m_sourceMesh.colors;
		var indices = m_sourceMesh.triangles;
		m_sortedIndices = new int[indices.Length];

		m_meshSorter = new MeshSorter(vertices, uvs, colors, indices, sourceRenderer.transform, headSpheres);
		m_meshSorter.BuildNormalizedPatches();
		m_meshSorter.SortIndices(Vector3.zero, m_sortedIndices, 0f);

		m_sortedMesh = new Mesh();
		m_sortedMesh.vertices = vertices;
		m_sortedMesh.uv = uvs;
		m_sortedMesh.normals = m_sourceMesh.normals;
		m_sortedMesh.tangents = m_sourceMesh.tangents;
		m_sortedMesh.colors = m_meshSorter.staticPatchColors;
		m_sortedMesh.triangles = m_sortedIndices;
		m_sortedMesh.bindposes = m_sourceMesh.bindposes;
		m_sortedMesh.boneWeights = m_sourceMesh.boneWeights;
		m_sourceMesh.RecalculateBounds();
	}

	void CreateMaterials() {
		// We sort by material render queue instead of renderer sorting order 
		// because we don't want to manually have to sort between multiple hair instances.

		var sourceMat = sourceRenderer.sharedMaterial;

		m_materialOpaque = new Material(sourceMat);
		m_materialOpaque.name = "HairOpaque";
		m_materialOpaque.renderQueue = 2450;
		m_materialOpaque.shaderKeywords = sourceMat.shaderKeywords;
		m_materialOpaque.DisableKeyword("_ALPHABLEND_ON");
		m_materialOpaque.EnableKeyword("_ALPHATEST_ON");

        //check to see if we're using 5.4, b/c we need to change the shader if we are
#if UNITY_5_4
        m_materialOpaque.EnableKeyword("_UNITY_5_4");
#endif
        m_materialOpaque.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
		m_materialOpaque.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
		m_materialOpaque.SetInt("_ZWrite", 1);
		m_materialOpaque.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
		m_materialOpaque.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

		m_materialBack = new Material(sourceMat);
		m_materialBack.name = "HairBack";
		m_materialBack.renderQueue = 2501;
		m_materialBack.shaderKeywords = sourceMat.shaderKeywords;
		m_materialBack.EnableKeyword("_ALPHABLEND_ON");
		m_materialBack.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		m_materialBack.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		m_materialBack.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Less);
		m_materialBack.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
		
		m_materialFront = new Material(sourceMat);
		m_materialFront.name = "HairFront";
		m_materialFront.renderQueue = 2502;
		m_materialFront.shaderKeywords = sourceMat.shaderKeywords;
		m_materialFront.EnableKeyword("_ALPHABLEND_ON");
		m_materialFront.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		m_materialFront.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		m_materialFront.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Less);
		m_materialFront.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
	}

	void UpdateMaterials() {
		m_materialOpaque.SetFloat("_Cutoff", opaqueAlphaRef);

		m_materialFront.SetInt("_ZWrite", frontWriteDepth ? 1 : 0);
		m_materialBack.SetInt("_ZWrite", 0);

		m_materialFront.SetFloat("_Cutoff", frontBackAlphaRef);
		m_materialBack.SetFloat("_Cutoff", frontBackAlphaRef);

		if(frontBackAlphaRef > 0f) {
			m_materialFront.EnableKeyword("_ALPHATEST_ON");
			m_materialBack.EnableKeyword("_ALPHATEST_ON");
		} else {
			m_materialFront.DisableKeyword("_ALPHATEST_ON");
			m_materialBack.DisableKeyword("_ALPHATEST_ON");
		}
	}

	void UpdateKeywords() {
		foreach(var m in new[]{m_materialOpaque, m_materialBack, m_materialFront}) {
			foreach(var n in System.Enum.GetNames(typeof(DebugMode)))
				m.DisableKeyword(n);

			m.EnableKeyword(debugMode.ToString());
		}
	}
	
	Material AddChild(Material m) {
		var go = new GameObject(m.name);
		go.layer = gameObject.layer;
		go.transform.parent = transform;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;

		Material instanceMat = m;

		if(sourceRenderer is MeshRenderer) {
			go.AddComponent<MeshFilter>().sharedMesh = m_sortedMesh;
			var mr = go.AddComponent<MeshRenderer>();
			mr.material = m;
			instanceMat = mr.material;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = true;
		} else {
			var smr = go.AddComponent<SkinnedMeshRenderer>();
			smr.rootBone = (sourceRenderer as SkinnedMeshRenderer).rootBone;
			smr.bones = (sourceRenderer as SkinnedMeshRenderer).bones;
			smr.material = m;
			instanceMat = smr.material;
			smr.sharedMesh = m_sortedMesh;
		}

		return instanceMat;
	}

	void SelectMesh() {
		/*if(!sourceRenderer.enabled && mode == Mode.Original) {
			foreach(var c in transform)
				Destroy((c as Transform).gameObject);

			sourceRenderer.enabled = true;
		} else if(sourceRenderer.enabled || transform.childCount != (useOpaquePass ? 3 : 2)) {
		*/
			foreach(var c in transform)
				Destroy((c as Transform).gameObject);

			sourceRenderer.enabled = false;

			if(useOpaquePass)
				m_materialOpaque = AddChild(m_materialOpaque);
			m_materialBack = AddChild(m_materialBack);
			m_materialFront = AddChild(m_materialFront);
		//}
	}

	void OnValidate() {
		if(mode == Mode.DynamicRadialDistance) {
			Debug.LogWarning("DynamicRadialDistance is not really supported. Falling back to StaticHeightBased. (change code if you really want to try this)");
			mode = Mode.StaticHeightBased;
		}

		if(m_sourceMesh) {
			sortDistanceScale = Mathf.Clamp01(sortDistanceScale);
			opaqueAlphaRef = Mathf.Clamp(opaqueAlphaRef, 0f, 1.001f);
			frontBackAlphaRef = Mathf.Clamp(frontBackAlphaRef, 0f, 1.001f);

			UpdateMaterials();
			UpdateKeywords();
			SelectMesh();
		}
	}

	void LateUpdate() {
		if(Camera.current == null || mode != Mode.DynamicRadialDistance)
			return;

		var eyePos = Camera.current.transform.position;
		var eyeHeadVec = headShell.transform.position - eyePos;
		var eyeRay = new Ray(eyePos, eyeHeadVec);
		RaycastHit rhi;
		if(headShell.GetComponent<SphereCollider>().Raycast(eyeRay, out rhi, eyeHeadVec.magnitude)) {
			m_meshSorter.SortIndices(transform.InverseTransformPoint(rhi.point), m_sortedIndices, sortDistanceScale);
			Debug.DrawLine(eyePos, rhi.point, Color.red, 3f);
			m_sortedMesh.triangles = m_sortedIndices;
			m_sortedMesh.colors = m_meshSorter.staticPatchColors;
		} else {
			Debug.LogWarning("Failed to find head ray.. inside shell?");
		}
	}

	#region MeshSorter
	class MeshSorter {
		Vector3[]		vertices;
		Vector2[]		uvs;
		Color[]			colors;
		SortablePatch[]	sortablePatches;
		uint[]			sortingList;
		
		public Color[]		staticPatchColors;
		public int[]		staticPatchIndices;
		
		class Triangle {
			public int i0, i1, i2;
			
			public Triangle(int i0, int i1, int i2) {
				this.i0 = i0;
				this.i1 = i1;
				this.i2 = i2;
			}
		}
		
		class Patch {
			public MeshSorter			owner;
			public HashSet<int>			indices;
			public HashSet<Triangle>	triangles;
			
			public Patch(MeshSorter owner, Triangle t) {
				this.owner = owner;
				indices = new HashSet<int>();
				triangles = new HashSet<Triangle>();
				
				if(t != null)
					AddTriangle(t);
			}
			
			public bool AddTriangle(Triangle t) {
				if(triangles.Contains(t))
					return false;
				
				triangles.Add(t);
				indices.Add(t.i0);
				indices.Add(t.i1);
				indices.Add(t.i2);
				return true;
			}
			
			public bool Merge(Patch p) {
				int commonCount = 0;
				foreach(var pi in p.indices) {
					if(indices.Contains(pi)) {
						if(++commonCount >= 1) 
							goto outside;
					} else {
						// This is dog slow, but good enough for now (and can also be baked if we're too lazy to opt it).
						const float threshold = 0.001f * 0.001f;
						var piv = owner.vertices[pi];
						foreach(var si in indices) {
							var siv = owner.vertices[si];
							if(Vector3.SqrMagnitude(piv - siv) <= threshold)
								if(++commonCount >= 1)
									goto outside;
						}
					}
				}
			outside:
					
				if(commonCount >= 1) {
					foreach(var t in p.triangles)
						AddTriangle(t);
					
					return true;
				}
				
				return false;
			}
		}
		
		struct SortablePatch {
			public Vector3	centroid;
			public float	layer;
			public int[]	indices;
			
			public SortablePatch(int[] i, Vector3 c, float l) {
				indices = i;
				centroid = c;
				layer = l;
			}
		}
		
		public MeshSorter(Vector3[] vertices, Vector2[] uvs, Color[] colors, int[] indices, Transform space, Transform[] spheres) {
			this.vertices = vertices;
			this.uvs = uvs;
			this.colors = colors;
			
			var patches = new List<Patch>();
			patches.Add(new Patch(this, new Triangle(indices[0], indices[1], indices[2])));
			Patch activePatch = patches[0];
			for(int i = 3, n = indices.Length; i < n; i += 3) {
				var newPatch = new Patch(this, new Triangle(indices[i], indices[i+1], indices[i+2]));
				if(!activePatch.Merge(newPatch)) {
					patches.Add(newPatch);
					activePatch = newPatch;
				}
			}
			//Debug.Log(string.Format("{0} patches after initial add.", patches.Count));
			
			//int mergeIterations = 0, mergeTests = 0;
			// Usually don't need to search for more patches
			/*for(;;) {
				bool didMerge = false;

				for(int i = 0, n = patches.Count - 1; i < n; ++i) {
					var p0 = patches[i];
					for(int j = i + 1; j <= n; ++j) {
						var p1 = patches[j];

						if(p0.Merge(p1)) {
							patches[j] = patches[n];
							patches.RemoveAt(n);
							didMerge = true;
							--n;
						}
						++mergeTests;
					}
					++i;
				}

				++mergeIterations;
				if(!didMerge)
					break;
			}*/
			
			staticPatchColors = new Color[vertices.Length];
			var staticIndices = new List<int>();
			int patchIdx = 0;
			foreach(var p in patches) {
				foreach(var t in p.triangles) {
					staticIndices.Add(t.i0);
					staticIndices.Add(t.i1);
					staticIndices.Add(t.i2);
					
					//SetDebugColor(patches.Count, patchIdx, t.i0, null);
					//SetDebugColor(patches.Count, patchIdx, t.i1, null);
					//SetDebugColor(patches.Count, patchIdx, t.i2, null);
				}
				//Debug.Log(string.Format("Indices in patch {0}: {1}", patchIdx, p.triangles.Count * 3));
				++patchIdx;
			}
			staticPatchIndices = staticIndices.ToArray();
			
			//Debug.Log(string.Format("Merged {2} triangles to {0} patches in {1} iterations (tried {3} - out {4} indices).", patches.Count, mergeIterations, indices.Length/3, mergeTests, staticIndices.Count));
			
			
			sortingList = new uint[patches.Count];
			sortablePatches = new SortablePatch[patches.Count];
			for(int i = 0, n = patches.Count; i < n; ++i) {
				var p = patches[i];
				
				var c = Vector3.zero;
				var l = float.MaxValue;
				foreach(var idx in p.indices) {
					var v = vertices[idx];
					c += v;
					#if _DISABLED
					foreach(var s in spheres) {
						l = Mathf.Min(l, Vector3.Distance(space.TransformPoint(v), s.position) - s.localScale.x);
					}
					#else
					l = Mathf.Min(l, space.TransformPoint(v).y - space.position.y);
					#endif
				}
				c /= (float)p.indices.Count;
				
				var patchIndices = new int[p.triangles.Count * 3];
				var pIdx = 0;
				foreach(var t in p.triangles) {
					patchIndices[pIdx++] = t.i0;
					patchIndices[pIdx++] = t.i1;
					patchIndices[pIdx++] = t.i2;
				}
				
				//Debug.Log(string.Format("Patch {0}:  Layer: {1}  Centroid: {2}", patchIdx, l, c));
				sortablePatches[i] = new SortablePatch(patchIndices, c, l);
			}
		}
		
		void SetDebugColor(int patchCount, int patchIdx, int offset, int[] indices) {
			float r = Mathf.Ceil((float)patchCount / 3f);
			float r2 = 2f * r;
			float fpi = (float)patchIdx;
			
			Color c = new Color();
			if(fpi <= r) c.r = Mathf.Clamp01(fpi / r);
			else if((fpi - r) <= r) c.g = Mathf.Clamp01((fpi - r) / r);
			else c.b = Mathf.Clamp01((fpi - r2) / r);
			
			if(indices != null)
				for(int i = 0, n = indices.Length; i < n; ++i)
					staticPatchColors[indices[i]] = c;
			else
				staticPatchColors[offset] = c;
		}
		
		public void BuildNormalizedPatches() {
			for(int i = 0, n = sortablePatches.Length; i < n; ++i) {
				var patch = sortablePatches[i];
				var idc = patch.indices;
				float vMin = 1f, vMax = 0f;
				for(int j = 0, m = idc.Length; j < m; ++j) {
					var uv = uvs[idc[j]];
					vMin = Mathf.Min(vMin, uv.y);
					vMax = Mathf.Max(vMax, uv.y);
				}
				
				var vScale = 1f / (vMax - vMin);
				var vOffset = vMin;
				
				for(int j = 0, m = idc.Length; j < m; ++j) {
					var idx = idc[j];
					var uvn = Mathf.Clamp01((uvs[idx].y - vOffset) * vScale);
					var uvn2 = uvn * uvn;
					var uvn3 = uvn2 * uvn;
					var alpha = colors.Length > 0 ? colors[idx].a : 1f;
					staticPatchColors[idx] = new Color(1f - uvn, 1f - uvn2, 1f - uvn3, alpha);
				}
			}
		}
		
		public void SortIndices(Vector3 eye, int[] indices, float distScale) {
			for(int i = 0, n = sortingList.Length; i < n; ++i) {
				var sp = sortablePatches[i];
				//Debug.Log(string.Format("SP: {0}  L: {1}  D2: {2}  D: {3}  C: {4}", i, sp.layer, Vector3.SqrMagnitude(eye - sp.centroid), Vector3.Distance(eye, sp.centroid), sp.centroid));
				var w = sp.layer + Vector3.SqrMagnitude(eye - sp.centroid) * distScale;
				var iw = Mathf.RoundToInt(Mathf.Clamp(w * 100f, 0f, 1048575f)); //TODO: use shell size as scale
				sortingList[i] = (uint)(((iw&0xFFFFF) << 20) | (i&0xFFF));
			}
			
			System.Array.Sort(sortingList);
			
			for(int i = 0, n = sortingList.Length, off = 0; i < n; ++i) {
				#if _DISABLED
				var si = sortingList[n - i - 1] & 0xFFF;
				#else
				var si = sortingList[i] & 0xFFF;
				#endif
				var spi = sortablePatches[si].indices;
				System.Array.Copy(spi, 0, indices, off, spi.Length);
				//SetDebugColor(n, i, 0, spi);
				off += spi.Length;
			}
			
		}
	}
	#endregion
}