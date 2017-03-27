using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MORPH3D;
using System.Collections;
using System.Text;
using System.Diagnostics;

namespace AssemblyCSharpEditor
{
	public class MorphDebugInfo : Editor
	{

		private static StreamWriter sr;
		private const string MORPH_LOG_PATH = "Assets/MORPH3D/~Morph Logs/";
		private const string DLL_PATH = "Assets/MORPH3D/scripts/Plugins/";

		[MenuItem("MORPH 3D/Generate Morph Debug Info")]
		public static void GenerateMorphDebugInfo()
		{
			string directory = string.Format (MORPH_LOG_PATH + "Morph Debug Log {0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
			string filename = "Log.txt";

			Directory.CreateDirectory (directory);
			sr = File.CreateText (directory + "/" + filename);

			UnityEngine.Debug.Log ("Generated Morph Log in /MORPH3D/~Morph Logs/. Delete after sending to Morph3D");

			Application.CaptureScreenshot(directory + "/Screenshot.png");

			//Check the platform it is on
			sr.WriteLine ("Application Platform: " + Application.platform);

			//Check Unity version
			sr.WriteLine("Unity Version: " + Application.unityVersion);

			//Check dll version of M3D_Dll and M3DIMPORT_DLL
			if (File.Exists (DLL_PATH + "M3D_DLL.dll")) {
				FileVersionInfo m3d = FileVersionInfo.GetVersionInfo(DLL_PATH + "M3D_DLL.dll");
				sr.WriteLine ("M3D_DLL.dll version: " + m3d.FileVersion.ToString ());
			}
			if (File.Exists (DLL_PATH + "M3DIMPORT_DLL.dll")) {
				FileVersionInfo m3dimport = FileVersionInfo.GetVersionInfo(DLL_PATH + "M3DIMPORT_DLL.dll");
				sr.WriteLine ("M3DIMPORT_DLL.dll version: " + m3dimport.FileVersion.ToString ());
			}

			//Check if it is playing
			sr.WriteLine ("Is Application Playing: " + Application.isPlaying);

			sr.WriteLine ("");

			//Write out number of gameobjects with a M3DCharacterManager attached
			M3DCharacterManager[] characterManagerObjects = FindObjectsOfType (typeof(M3DCharacterManager)) as M3DCharacterManager[];
			sr.WriteLine ("Gameobjects with M3dCharacterManager: " + characterManagerObjects.Length);

			foreach (M3DCharacterManager obj in characterManagerObjects) {

				//Check current lod level
				sr.WriteLine ( obj.gameObject.name + " current LOD level: " + obj.currentLODLevel);

				//Check blendshape levels
				foreach (MORPH3D.FOUNDATIONS.Morph morph in obj.coreMorphs.morphStateGroups["All"]) {
					if (morph.value > 0) {
						sr.WriteLine ( obj.gameObject.name + " blend shape " + morph.name + ": " + morph.value);
					}
				}

				//Check if it has an animation controller
				Animator anim = obj.gameObject.GetComponent<Animator> ();
				if (anim != null) {
					if (anim.runtimeAnimatorController != null) {
						sr.WriteLine ( obj.gameObject.name + " has Animator Controller: " + anim.runtimeAnimatorController.name);
					} else {
						sr.WriteLine ( obj.gameObject.name + " does NOT have an animation controller");
					}
				}

				//Check content packs
				foreach (ContentPack cp in obj.GetAllContentPacks()) {
					sr.WriteLine ( obj.gameObject.name + "content pack: " + cp.name);
				}

				//Get children info
				sr.WriteLine (obj.gameObject.name + " children:");
				FindChildren (obj.gameObject);
			}
			sr.Close ();

		}


		private static void FindChildren(GameObject obj){

			StringBuilder sb = new StringBuilder ();

			//check if gameobject is active
			sb.Append ("\t" + obj.name + " active: " + obj.activeSelf.ToString ());

			//check for a skinned mesh renderer
			SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer> ();
			if (smr != null) {

				//check if the skinned mesh renderer is enabled
				sb.Append (" SkinnedMeshRender Enabled: " + smr.enabled);

				Renderer rend = obj.GetComponent<Renderer> ();
				if (rend != null) {
					foreach(Material mat in rend.sharedMaterials){
						sb.Append (System.Environment.NewLine);

						//print out the material name and then the shader name
						sb.Append("\t\t" + mat.name + " " + mat.shader.name);
					}
				}
			}

			sr.WriteLine (sb.ToString());

			foreach (Transform child in obj.transform) {

				//stop at the hip object
				if (obj.name == "hip") {
					return;
				}
				FindChildren (child.gameObject);
			}
		}

	}
}

