using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using MORPH3D.COSTUMING;
using MORPH3D;
using System;

namespace MORPH3D.EDITORS
{

    /// <summary>
    /// Used internally to check for updates triggered by a user in the editor for a blendshape
    /// </summary>
    public struct EditorMorphState
    {
        public float value;
        public bool attached;
        public bool dirty;

        public bool dirtyValue;
        public bool dirtyAttached;
    }

    [CustomEditor (typeof(M3DCharacterManager))]
	public class M3DCharacterManagerEditor : Editor
	{
		protected M3DCharacterManager charMan = null;

        protected bool showMorphs = false; //internally used for the twirl down of "Morphs" in the inspector panel

		protected bool showContentPacks = false;

		protected bool showAllClothing = false;
		protected bool showAllClothingGroups = false;
		protected bool[] showClothingGroups = null;
		protected int selectedClothingName = 0;

        protected string selectedBlendShape = "";
        protected bool showMorphTypeGroups = false;

        protected bool showAttachmentPoints = false;
		protected bool[] showAttachmentPointsGroups = null;
		protected string[] selectedPropsNames = null;

		protected string selectedNewAttachmentPointName = "";

		protected bool showHair = false;

        public override void OnInspectorGUI()
		{
			#region just_stuff
			serializedObject.Update ();
			if(charMan == null)
				charMan = (M3DCharacterManager)target;


            if(charMan == null || !charMan.isAwake)
            {
                GUILayout.Label("Your figure must be in the scene to edit settings");
                return;
            }

            GUIStyle m3dDefaultButtonStyle = new GUIStyle(GUI.skin.button);
            m3dDefaultButtonStyle.margin = new RectOffset(10,10,5,5);
            m3dDefaultButtonStyle.padding = new RectOffset(5, 5, 5, 5);

			#endregion just_stuff

			
			#region LOD
			float lod;
			lod = EditorGUILayout.Slider("LOD", charMan.currentLODLevel, 0, 1);
			if(lod != charMan.currentLODLevel)
			{
				Undo.RecordObject(charMan, "Change LOD");
				charMan.SetLODLevel(lod);
				EditorUtility.SetDirty(charMan);
			}
			EditorGUILayout.Space();
            #endregion LOD

            #region morphs
            showMorphs = EditorGUILayout.Foldout(showMorphs, "Morphs");
            if (showMorphs)
            {
                charMan.coreMorphs.SortIfNeeded();
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal();
                selectedBlendShape = GUILayout.TextField(selectedBlendShape, GUI.skin.FindStyle("ToolbarSeachTextField"));
                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
                {
                    selectedBlendShape = "";
                    GUI.FocusControl(null);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                if (GUILayout.Button("Reset All", m3dDefaultButtonStyle))
                {
                    Undo.RecordObject(charMan, "Change Morph");
                    for (int i = 0; i < charMan.coreMorphs.morphs.Count; i++)
                    {
                        if (charMan.coreMorphs.morphs[i].attached)
                        {
                            charMan.SetBlendshapeValue(charMan.coreMorphs.morphs[i].name, 0);
                        }
                    }
                    EditorUtility.SetDirty(charMan);
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Detach All", m3dDefaultButtonStyle))
                {
                    Undo.RecordObject(charMan, "Detach All Morphs");
                    charMan.RemoveAllMorphs();
                    EditorUtility.SetDirty(charMan);
                }
                EditorGUILayout.Space();

                List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphValues = new List<MORPH3D.FOUNDATIONS.Morph>();
                List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphAttachments = new List<MORPH3D.FOUNDATIONS.Morph>();
                List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphDettachments = new List<MORPH3D.FOUNDATIONS.Morph>();

                EditorMorphs(charMan.coreMorphs.morphs, dirtyMorphValues, dirtyMorphAttachments, dirtyMorphDettachments);

                if (dirtyMorphDettachments.Count > 0 || dirtyMorphAttachments.Count > 0 || dirtyMorphValues.Count > 0)
                {
                    charMan.coreMorphs.DettachMorphs(dirtyMorphDettachments.ToArray());
                    charMan.coreMorphs.AttachMorphs(dirtyMorphAttachments.ToArray());
                    charMan.coreMorphs.SyncMorphValues(dirtyMorphValues.ToArray());
                    charMan.SyncAllBlendShapes();
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
            #endregion

            #region Morph Groups

            var morphTypeGroup = FOUNDATIONS.MorphTypeGroupService.GetMorphTypeGroups(charMan.gameObject.name);
            if (morphTypeGroup != null)
            {
                showMorphTypeGroups = EditorGUILayout.Foldout(showMorphTypeGroups, "Morph Groups");
                if (showMorphTypeGroups)
                {
                    charMan.coreMorphs.SortIfNeeded();
                    EditorGUI.indentLevel++;
                    foreach (var group in morphTypeGroup.SubGroups.Values)
                    {
                        ShowMorphTypeGroup(group);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            #endregion

            #region contentPacks
            showContentPacks = EditorGUILayout.Foldout (showContentPacks, "Content Packs");

			if(showContentPacks)
			{



				EditorGUI.indentLevel++;
				List<ContentPack> allPacks = charMan.GetAllContentPacks();
				for(int i = 0; i < allPacks.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(allPacks[i].name);
					if(GUILayout.Button("X"))
					{
						Undo.RecordObject(charMan, "Remove Bundle");
                        charMan.RemoveContentPack(allPacks[i]);
						EditorUtility.SetDirty(charMan);
					}
					EditorGUILayout.EndHorizontal();
				}
                /*
                CostumeItem tempPack = null;
				tempPack = (CostumeItem)EditorGUILayout.ObjectField("New", tempPack, typeof(CostumeItem), true);
				if(tempPack != null)
				{
					ContentPack packScript = new ContentPack(tempPack.gameObject);
					Undo.RecordObject(charMan, "Add Bundle");
					charMan.AddContentPack(packScript);
					EditorUtility.SetDirty(charMan);
				}
                */
                GameObject tempPack = null;
				tempPack = (GameObject)EditorGUILayout.ObjectField("New", tempPack, typeof(GameObject), true);
				if(tempPack != null)
				{
                    string tPPath = AssetDatabase.GetAssetPath(tempPack);
                    if (tPPath.EndsWith(".fbx"))
                    {
                        //if we dropped an fbx, try replacing it with a matching .prefab instead
                        tPPath = tPPath.Replace(".fbx", ".prefab");
                        GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(tPPath);
                        if(prefabObj != null)
                        {
                            tempPack = prefabObj;
                        }
                    }
					ContentPack packScript = new ContentPack(tempPack);
					Undo.RecordObject(charMan, "Add Bundle");
					charMan.AddContentPack(packScript);
					EditorUtility.SetDirty(charMan);
				}


                if(GUILayout.Button("Import Selected From Project Pane", m3dDefaultButtonStyle))
                {
                    string[] guids = Selection.assetGUIDs;
                    List<string> paths = new List<string>();
                    foreach(string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);

                        if (System.IO.Directory.Exists(path))
                        {
                            string[] prefabPaths = System.IO.Directory.GetFiles(path, "*.prefab", System.IO.SearchOption.AllDirectories);
                            for(int i = 0; i < prefabPaths.Length; i++)
                            {
                                string dirtyPath = prefabPaths[i];
                                dirtyPath = dirtyPath.Replace(@"\", "/");
                                paths.Add(dirtyPath);
                            }
                        }

                    }

                    foreach (string path in paths)
                    {
                        if (path.EndsWith(".prefab"))
                        {
                            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            CostumeItem ci = go.GetComponent<CostumeItem>();
                            if (ci != null)
                            {
                                ContentPack packScript = new ContentPack(go);
                                Undo.RecordObject(charMan, "Add Bundle");
                                charMan.AddContentPack(packScript);
                                EditorUtility.SetDirty(charMan);
                            }
                        }
                    }
                }

                if(GUILayout.Button("Clear Lingering Content Packs", m3dDefaultButtonStyle))
                {
                    charMan.RemoveRogueContent();
                }

				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			#endregion contentPacks
			
			#region hair
			showHair = EditorGUILayout.Foldout (showHair, "Hair");
			if(showHair)
			{
				EditorGUI.indentLevel++;
				List<MORPH3D.COSTUMING.CIhair> allHair = charMan.GetAllHair();
				foreach(MORPH3D.COSTUMING.CIhair mesh in allHair)
				{
					if(DisplayHair(mesh))
					{
						Undo.RecordObject(charMan, "Toggle Hair");
                        mesh.SetVisibility(!mesh.isVisible);
						//						data.SetVisibilityOnHairItem(mesh.ID, !mesh.isVisible);
						EditorUtility.SetDirty(charMan);
					}
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			#endregion hair

			#region clothing
			showAllClothing = EditorGUILayout.Foldout (showAllClothing, "Clothing");
			if(showAllClothing)
			{
				EditorGUI.indentLevel++;

				List<CIclothing> allClothing = null;
				allClothing = charMan.GetAllClothing();
				foreach(CIclothing mesh in allClothing)
				{
//					bool tempLock;
					bool temp = DisplayClothingMesh(mesh);

//					if(tempLock != mesh.isLocked)
//					{
//						Undo.RecordObject(data, "Lock Clothing");
//						if(tempLock)
					//							data.LockClothingItem(mesh.ID);
//						else
					//							data.UnlockClothingItem(mesh.ID);
//						EditorUtility.SetDirty(data);
//					}

					if(temp)
					{
						Undo.RecordObject(charMan, "Toggle Clothing");
						charMan.SetClothingVisibility(mesh.ID, !mesh.isVisible);
						EditorUtility.SetDirty(charMan);
					}
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			#endregion clothing

			#region props
			CIattachmentPoint[] attachmentPoints = charMan.GetAllAttachmentPoints();
//			Debug.Log("AP LENGTH:"+attachmentPoints.Length);
			if(showAttachmentPointsGroups == null || showAttachmentPointsGroups.Length != attachmentPoints.Length)
				showAttachmentPointsGroups = new bool[attachmentPoints.Length];
			/*
			if(selectedProps == null || selectedProps.Length != attachmentPoints.Length)
				selectedProps = new int[attachmentPoints.Length];
			*/	
			if(selectedPropsNames == null || selectedPropsNames.Length != attachmentPoints.Length)
				selectedPropsNames = new string[attachmentPoints.Length];
			List<CIprop> props = charMan.GetAllLoadedProps();
            Dictionary<string, string> idToName = new Dictionary<string, string>();
			string[] propsNames = new string[]{};
			if(props != null){
				propsNames = new string[props.Count];

			}
            for (int i = 0; i < propsNames.Length; i++)
            {
                propsNames[i] = props[i].dazName + "|" + props[i].ID;
                idToName[props[i].ID] = props[i].dazName;
            }

			showAttachmentPoints = EditorGUILayout.Foldout (showAttachmentPoints, "Attachment Points");
			if(showAttachmentPoints)
			{
				int deleteAttachment = -1;
				EditorGUI.indentLevel++;
				for(int i = 0; i < attachmentPoints.Length; i++)
				{ 
					EditorGUILayout.BeginHorizontal();
					showAttachmentPointsGroups[i] = EditorGUILayout.Foldout (showAttachmentPointsGroups[i], attachmentPoints[i].attachmentPointName);
					GUILayout.FlexibleSpace();
					if(GUILayout.Button("X", GUILayout.Width(45)))
						deleteAttachment = i;
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
					if(showAttachmentPointsGroups[i])
					{
						EditorGUI.indentLevel++;
						CIprop[] activeProps = attachmentPoints[i].getAttachmentArray();
						int destroyProp = -1;
						for(int x = 0; x < activeProps.Length; x++)
						{
							if(DisplayProp(activeProps[x]))
								destroyProp = x;
						}
						if(destroyProp >= 0)
						{
							Undo.RecordObject(charMan, "Destroy Prop");
							charMan.DetachPropFromAttachmentPoint(activeProps[destroyProp].ID, attachmentPoints[i].attachmentPointName);
							EditorUtility.SetDirty(charMan);
						}
//						Debug.Log("GF");
						if(propsNames.Length > 0)
						{
//							Debug.Log("FDFG");
							EditorGUILayout.Space();
							EditorGUILayout.BeginHorizontal();

                            string propDisplay = !string.IsNullOrEmpty(selectedPropsNames[i]) ? idToName[selectedPropsNames[i]] : null;

                            EditorGUILayout.LabelField("Add Prop:", GUILayout.Width(150));
							EditorGUILayout.LabelField(propDisplay, GUILayout.Width(150));
							if(selectedPropsNames[i] != "" && selectedPropsNames[i] != null && charMan.GetLoadedPropByName(selectedPropsNames[i]) == null)
								selectedPropsNames[i] = "";
							if(GUILayout.Button("Search"))
							{
								int num = i;
								SearchableWindow.Init(delegate(string newName) {

                                    string id = newName;
                                    int pos = newName.LastIndexOf('|');
                                    if (pos >= 0)
                                    {
                                        id = newName.Substring(pos + 1);
                                    }

                                    UnityEngine.Debug.Log("ID: " + id);

                                    selectedPropsNames[num] = id;
                                }, propsNames);
							}
							if(selectedPropsNames[i] != "" && selectedPropsNames[i] != null && GUILayout.Button("Add"))
							{
								Undo.RecordObject(charMan, "Attach Prop");
                                UnityEngine.Debug.Log("Prop:" + selectedPropsNames[i]);
								charMan.AttachPropToAttachmentPoint(selectedPropsNames[i], attachmentPoints[i].attachmentPointName);
								EditorUtility.SetDirty(charMan);
								selectedPropsNames[i] = "";
							}
							GUILayout.FlexibleSpace();
							EditorGUILayout.EndHorizontal();
							/*
							EditorGUILayout.Space();
							EditorGUILayout.BeginHorizontal();
							selectedProps[i] = EditorGUILayout.Popup (selectedProps[i], propsNames, GUILayout.Width(150));
							if(GUILayout.Button("Add"))
							{
								Undo.RecordObject(data, "Attach Prop");
								data.AttachPropToAttachmentPoint(propsNames[selectedProps[i]], attachmentPoints[i].attachmentPointName);
								EditorUtility.SetDirty(data);
								selectedProps[i] = 0;
							}
							GUILayout.FlexibleSpace();
							EditorGUILayout.EndHorizontal();
							*/
						}
						EditorGUILayout.Space();
						EditorGUI.indentLevel--;
					}
				}

				if(deleteAttachment >= 0)
				{
					Undo.RecordObject(attachmentPoints[deleteAttachment], "Delete Attachment Point");
					charMan.DeleteAttachmentPoint(attachmentPoints[deleteAttachment].attachmentPointName);
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("New Point:", GUILayout.Width(150));
				EditorGUILayout.LabelField(selectedNewAttachmentPointName, GUILayout.Width(150));
				Transform tempBone = charMan.GetBoneByName (selectedNewAttachmentPointName);
				if(selectedNewAttachmentPointName != "" && selectedNewAttachmentPointName != null && tempBone == null)
					selectedNewAttachmentPointName = "";
				if(GUILayout.Button("Search"))
				{
					SearchableWindow.Init(delegate(string newName) { selectedNewAttachmentPointName = newName; }, charMan.GetAllBonesNames());
				}
				if(selectedNewAttachmentPointName != "" && selectedNewAttachmentPointName != null && tempBone != null && GUILayout.Button("Add"))
				{
					Undo.RecordObject(tempBone.gameObject, "New Attachment Point");
					charMan.CreateAttachmentPointOnBone(selectedNewAttachmentPointName);
					selectedNewAttachmentPointName = "";
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				/*
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				selectedNewAttachmentPointName = EditorGUILayout.TextField("New Point Bone Name", selectedNewAttachmentPointName);
				if(GUILayout.Button("Add") && selectedNewAttachmentPointName != "")
				{
					Transform bone = data.boneService.getBoneByName (selectedNewAttachmentPointName);
					if(bone != null)
					{
						Undo.RecordObject(bone.gameObject, "New Attachment Point");
						data.CreateAttachmentPointOnBone(selectedNewAttachmentPointName);
					}
					selectedNewAttachmentPointName = "";
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				selectedNewAttachmentPoint = (GameObject)EditorGUILayout.ObjectField("New Attachemnt Point", selectedNewAttachmentPoint, typeof(GameObject), true);
				if(selectedNewAttachmentPoint != null && !selectedNewAttachmentPoint.activeInHierarchy)
					selectedNewAttachmentPoint = null;
				if(GUILayout.Button("Add") && selectedNewAttachmentPoint != null)
				{
					if(selectedNewAttachmentPoint.GetComponent<CIattachmentPoint>() == null)
					{
						Undo.RecordObject(selectedNewAttachmentPoint, "New Attachment Point");
						data.CreateAttachmentPointFromGameObject(selectedNewAttachmentPoint);
					}
					selectedNewAttachmentPoint = null;
				}
				EditorGUILayout.EndHorizontal();
				*/

				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			#endregion props
		}

        #region morphs_display

        public void ShowMorphTypeGroup(FOUNDATIONS.MorphTypeGroup group)
        {
            group.IsOpenInEditor = EditorGUILayout.Foldout(group.IsOpenInEditor, group.Key);
            if (group.IsOpenInEditor)
            {
                EditorGUI.indentLevel++;
                foreach (var key in group.SubGroups.Keys)
                {
                    ShowMorphTypeGroup(group.SubGroups[key]);
                }
                Predicate<FOUNDATIONS.Morph> morphIsInThisGroup = (morph) => group.Morphs.ContainsKey(morph.name);
                var morphs = charMan.coreMorphs.morphs.FindAll(morphIsInThisGroup);
                EditorMorphs(morphs);
                EditorGUI.indentLevel--;
            }
        }

        protected void EditorMorphs(List<FOUNDATIONS.Morph> morphs)
        {
            List<FOUNDATIONS.Morph> dirtyMorphValues = new List<FOUNDATIONS.Morph>();
            List<FOUNDATIONS.Morph> dirtyMorphAttachments = new List<FOUNDATIONS.Morph>();
            List<FOUNDATIONS.Morph> dirtyMorphDettachments = new List<FOUNDATIONS.Morph>();

            EditorMorphs(morphs, dirtyMorphValues, dirtyMorphAttachments, dirtyMorphDettachments);

            if (dirtyMorphDettachments.Count > 0 || dirtyMorphAttachments.Count > 0 || dirtyMorphValues.Count > 0)
            {
                charMan.coreMorphs.DettachMorphs(dirtyMorphDettachments.ToArray());
                charMan.coreMorphs.AttachMorphs(dirtyMorphAttachments.ToArray());
                charMan.coreMorphs.SyncMorphValues(dirtyMorphValues.ToArray());
                charMan.SyncAllBlendShapes();
            }
        }

        protected void EditorMorphs(List<MORPH3D.FOUNDATIONS.Morph> morphs, List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphValues, List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphAttachments,
            List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphDettachments)
        {
            string searchKey = null;
            if (!String.IsNullOrEmpty(selectedBlendShape))
            {
                searchKey = selectedBlendShape.ToLower();
            }

            for (int i = 0; i < morphs.Count; i++)
            {
                MORPH3D.FOUNDATIONS.Morph morph = morphs[i];

                if (!String.IsNullOrEmpty(selectedBlendShape))
                {
                    if(!morph.name.ToLower().Contains(searchKey) && !morph.displayName.ToLower().Contains(searchKey) && !morph.localName.ToLower().Contains(searchKey))
                    {
                        continue;
                    }
                }

                EditorMorphState ems = HandleEditorMorphState(morph);

                if (ems.dirty)
                {
                    //we need to update this morph
                    if (ems.dirtyAttached)
                    {
                        if (ems.attached)
                        {
                            morph.attached = true;
                            dirtyMorphAttachments.Add(morph);
                        }
                        else
                        {
                            morph.attached = false;
                            dirtyMorphDettachments.Add(morph);
                        }
                    }

                    if (ems.dirtyValue)
                    {
                        morph.value = ems.value;

                        if (!morph.attached && ems.value > 0f)
                        {
                            //if it wasn't attached and the slider is not 0 attach it
                            morph.attached = true;
                            dirtyMorphAttachments.Add(morph);
                        }
                        dirtyMorphValues.Add(morph);
                    }

                    //replace the object with our modified one, why doesn't c# support references for local vars.... this is stupid
                    morphs[i] = morph;

                    //Debug.Log("Morph: " + morph.name + " | " + morph.value + " | " + data.coreMorphs.morphs[i].value + " | " + (ems.dirtyValue ? " DIRTY VALUE " : "") + (ems.dirtyAttached ? " DIRTY ATTACH " : "") );
                }
            }
        }

        ///// <summary>
        ///// Renders the Morph Panel
        ///// </summary>
        //protected void HandleMorphsPane()
        //{

        //    List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphValues = new List<MORPH3D.FOUNDATIONS.Morph>();
        //    List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphAttachments = new List<MORPH3D.FOUNDATIONS.Morph>();
        //    List<MORPH3D.FOUNDATIONS.Morph> dirtyMorphDettachments = new List<MORPH3D.FOUNDATIONS.Morph>();

        //    for (int i = 0; i < charMan.coreMorphs.morphs.Count; i++) 
        //    {
        //        if (selectedBlendShape != "" &&
        //            charMan.coreMorphs.morphs[i].displayName.IndexOf(selectedBlendShape, StringComparison.OrdinalIgnoreCase) < 0)
        //        {
        //            continue;
        //        }

        //        MORPH3D.FOUNDATIONS.Morph morph = charMan.coreMorphs.morphs[i];
        //        EditorMorphState ems = HandleEditorMorphState(morph);

        //        if (ems.dirty)
        //        {
        //            //we need to update this morph

        //            if (ems.dirtyAttached)
        //            {
        //                if (ems.attached)
        //                {
        //                    morph.attached = true;
        //                    dirtyMorphAttachments.Add(morph);
        //                }
        //                else
        //                {
        //                    morph.attached = false;
        //                    dirtyMorphDettachments.Add(morph);
        //                }
        //            }

        //            if (ems.dirtyValue)
        //            {
        //                morph.value = ems.value;

        //                if(!morph.attached && ems.value > 0f)
        //                {
        //                    //if it wasn't attached and the slider is not 0 attach it
        //                    morph.attached = true;
        //                    dirtyMorphAttachments.Add(morph);
        //                }
        //                dirtyMorphValues.Add(morph);
        //            }

        //            //replace the object with our modified one, why doesn't c# support references for local vars.... this is stupid
        //            charMan.coreMorphs.morphs[i] = morph;

        //            //Debug.Log("Morph: " + morph.name + " | " + morph.value + " | " + data.coreMorphs.morphs[i].value + " | " + (ems.dirtyValue ? " DIRTY VALUE " : "") + (ems.dirtyAttached ? " DIRTY ATTACH " : "") );
        //        }
        //    }

        //    if (dirtyMorphDettachments.Count > 0 || dirtyMorphAttachments.Count > 0 || dirtyMorphValues.Count > 0)
        //    {
        //        charMan.coreMorphs.DettachMorphs(dirtyMorphDettachments.ToArray());
        //        charMan.coreMorphs.AttachMorphs(dirtyMorphAttachments.ToArray());
        //        charMan.coreMorphs.SyncMorphValues(dirtyMorphValues.ToArray());
        //        charMan.SyncAllBlendShapes();
        //    }
            
        //}

        
        protected EditorMorphState HandleEditorMorphState(MORPH3D.FOUNDATIONS.Morph morph)
        {
            EditorMorphState ems = new EditorMorphState();
            ems.dirty = false;

            GUILayoutOption[] optionsLabel = new GUILayoutOption[] { GUILayout.MaxWidth(200.0f), GUILayout.MinWidth(25.0f) };
            GUILayoutOption[] optionsSlider = new GUILayoutOption[] { GUILayout.MaxWidth(200.0f), GUILayout.MinWidth(50.0f) };
            GUILayoutOption[] optionsKey = new GUILayoutOption[] { GUILayout.MaxWidth(200.0f), GUILayout.MinWidth(0.0f), GUILayout.Height(EditorGUIUtility.singleLineHeight) };
            GUILayoutOption[] optionsToggle = new GUILayoutOption[] { GUILayout.Width(20f) };

            EditorGUILayout.BeginHorizontal();
            //Show the slider between 0% and 100% for the morph

            EditorGUILayout.LabelField(morph.displayName, optionsLabel);
            ems.value = EditorGUILayout.Slider(morph.value, 0f, 100f, optionsSlider);
            //Show the checkbox for if this morph should be installed to the figure/mesh
            ems.attached = EditorGUILayout.Toggle(morph.attached, optionsToggle); //most efficient way, but not necessarily the most accurate way
            //ems.attached = EditorGUILayout.Toggle(charMan.coreMorphs.morphGroups["Attached"].Contains(morph)); //most accurate way but not O(1);
            //has a property changed?
            if (ems.attached != morph.attached)
            {
                ems.dirtyAttached = true;
                ems.dirty = true;
            }
            if (Mathf.Abs(ems.value - morph.value) > 0.001f)
            {
                ems.dirtyValue = true;
                ems.dirty = true;
            }

            EditorGUILayout.SelectableLabel(morph.localName, EditorStyles.textField, optionsKey);

            EditorGUILayout.EndHorizontal();

            return ems;
        }
        #endregion

        #region clothing_display
        protected bool DisplayClothingMesh(MORPH3D.COSTUMING.CIclothing mesh)
		{
			bool result;
			EditorGUILayout.BeginHorizontal();

            string labelStr = String.IsNullOrEmpty(mesh.name) ? mesh.ID : mesh.name;
			EditorGUILayout.LabelField (labelStr, GUILayout.Width(150));
			if(mesh.isVisible)
				GUILayout.Space (60);
			result = GUILayout.Button ((mesh.isVisible) ? "Disable" : "Enable", GUILayout.Width(60));
			if(!mesh.isVisible)
				GUILayout.Space (60);
//			if (mesh.isVisible)
//				lockItem = EditorGUILayout.Toggle (mesh.isLocked);
//			else
//				lockItem = mesh.isLocked;

			EditorGUILayout.EndHorizontal();

			return result;
		}

		#endregion clothing_display

		#region props_display
		protected bool DisplayProp(MORPH3D.COSTUMING.CIprop prop)
		{
			bool result;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField (prop.dazName, GUILayout.Width(180));
			GUILayout.Space (60);
			result = GUILayout.Button ("Disable", GUILayout.Width(60));
			EditorGUILayout.EndHorizontal();
			
			return result;
		}
		#endregion props_display
		
		#region hair_display
		protected bool DisplayHair(MORPH3D.COSTUMING.CIhair mesh)
		{
			bool result;
			EditorGUILayout.BeginHorizontal();
            string labelStr = String.IsNullOrEmpty(mesh.name) ? mesh.ID : mesh.name;
			EditorGUILayout.LabelField (labelStr, GUILayout.Width(150));
			if(mesh.isVisible)
				GUILayout.Space (60);
			result = GUILayout.Button ((mesh.isVisible) ? "Disable" : "Enable", GUILayout.Width(60));
			if(!mesh.isVisible)
				GUILayout.Space (60);
			EditorGUILayout.EndHorizontal();
			
			return result;
		}
        #endregion hair_display

    }

}
