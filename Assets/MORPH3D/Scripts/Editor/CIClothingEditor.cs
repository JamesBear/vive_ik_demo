using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using MORPH3D;
using MORPH3D.COSTUMING;
using MORPH3D.CONSTANTS;
//using M3D_DLL;
//using Morph3d.Utility.Schematic;

namespace MORPH3D.EDITORS
{
	public struct MaterialOption {
		public string assetPath;
		public string displayName;
		public string materialName;
	}

	[CustomEditor(typeof(CIclothing))]
	public class CIClothingEditor : Editor
	{
		//private DateTime _lastCheckTime = new DateTime ();
		//private List<Material> _potentialMaterials = null;
		//private Material _currentMaterial = null;


		private int _materialOptionIndex = -1;



		public List<MaterialOption> GetPotentialMaterialsFromAssetId(string collectionName, string itemName){
			List<MaterialOption> options = new List<MaterialOption> ();

			string strippedName = itemName.Replace ("_Left", "").Replace ("_Right", "");
			bool isSplitItem = !strippedName.Equals(itemName);
			bool isLeft = (isSplitItem && itemName.Contains ("_Left"));

			string matDir = "Assets/MORPH3D/Content/" + collectionName + "/" + strippedName + "/Materials";
			if (!Directory.Exists (matDir)) {
				return null;
			}

			string[] paths = Directory.GetFiles (matDir, "*0.mat", SearchOption.AllDirectories);

			//MonDeserializer deserializer = new MonDeserializer ();

			for(int i=0;i<paths.Length;i++){
                string path = paths[i].Replace(@"\", @"/"); ;


				string monPath = path.Replace (".mat", ".mon");
				if (!File.Exists (monPath)) {
					continue;
				}

				if (isSplitItem) {
					if (isLeft) {
						if (!path.Contains ("Left")) {
							continue;
						}
					} else {
						if (!path.Contains ("Right")) {
							continue;
						}
					}
				}

				int pos = path.LastIndexOf ("/");
				string materialName = path.Substring(pos+1);
				string dirFull = path.Substring (0, pos);
				pos = dirFull.LastIndexOf ("/");
				string dirName = dirFull.Substring (pos + 1);

				MaterialOption mo = new MaterialOption ();
				mo.assetPath = path;
				mo.displayName = dirName;
				mo.materialName = materialName;
				options.Add (mo);
			}

			return options;
		}
		
		public override void OnInspectorGUI()
		{

			CIclothing comp = (CIclothing)target;
			CoreMeshMetaData cmmd = comp.gameObject.GetComponent<CoreMeshMetaData> ();

			comp.dazName = EditorGUILayout.TextField("Daz Name", comp.dazName);
			comp.ID = EditorGUILayout.TextField("ID", comp.ID);
			//comp.LODlist = EditorGUILayout.Field

			SerializedProperty lodList = serializedObject.FindProperty ("LODlist");
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(lodList, true);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();


			comp.currentLODlevel = EditorGUILayout.FloatField("Current LOD Level", comp.currentLODlevel);
			comp.meshType = (MESH_TYPE)EditorGUILayout.EnumPopup ("Mesh Type", comp.meshType);

			comp.isAttached = EditorGUILayout.Toggle ("Is Attached", comp.isAttached);
			bool isVisible = EditorGUILayout.Toggle ("Is Visible", comp.isVisible);
            comp.SetVisibility(isVisible);

			if (cmmd != null) {
				List<MaterialOption> mos = GetPotentialMaterialsFromAssetId (cmmd.collection_name, comp.dazName);
                if (mos != null)
                {
                    List<string> options = new List<string>();
                    foreach (MaterialOption mo in mos)
                    {
                        options.Add(mo.displayName);
                    }

                    if (mos.Count > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Preconfigured Materials");
                        _materialOptionIndex = EditorGUILayout.Popup(_materialOptionIndex, options.ToArray());
                        if (_materialOptionIndex >= 0)
                        {
                            Material m = AssetDatabase.LoadAssetAtPath<Material>(mos[_materialOptionIndex].assetPath);
                            if (m != null)
                            {
                                Renderer[] renderers = comp.gameObject.GetComponentsInChildren<Renderer>();
                                foreach (Renderer renderer in renderers)
                                {
                                    renderer.material = m;
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
			}

			//comp.alphaMask = (Texture2D) EditorGUILayout.ObjectField ("Alpha Mask (deprecated)", comp.alphaMask, typeof (Texture2D), false);

			foreach(MATERIAL_SLOT slot in Enum.GetValues(typeof(MATERIAL_SLOT))){



				if (slot != MATERIAL_SLOT.HEAD && slot != MATERIAL_SLOT.BODY) {
					continue;
				}

				Texture2D tex = comp.alphaMasks.ContainsKey (slot) ? comp.alphaMasks [slot] : null;



				//NOTE: let's NOT do this, this would be a legacy 1.0 -> 1.5, but I think we should have these explicitly separate
				// we'll only process the old texture slot 
				/*
				if (slot == MATERIAL_SLOT.BODY && tex == null) {
					tex = comp.alphaMask;
				}
				*/

				comp.alphaMasks [slot] = (Texture2D) EditorGUILayout.ObjectField ("Alpha Mask: " + slot.ToString(), tex, typeof (Texture2D), false);
			}





			//handle the dynamic list of elements

			//EditorGUILayout.LabelField("LOD Level", myTarget.Level.ToString());
		}

    }
}

