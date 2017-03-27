using UnityEngine;
using UnityEditor;
using System.Collections;
using MCS_Utilities;

[CustomEditor(typeof(M3DResourceFileWrapper))]
public class M3DResourceEditor : Editor {

    /*
    public void Awake()
    {
        UnityEngine.Debug.Log("Awake");
    }
    public void OnEnable()
    {
        UnityEngine.Debug.Log("OnEnable");
    }
    */

    public override void OnInspectorGUI()
    {
        M3DResourceFileWrapper wrapper = (M3DResourceFileWrapper)target;

        GUILayout.Label(wrapper.fileName);

        try
        {
            M3DResource m3dresource = new M3DResource();
            m3dresource.Read(wrapper.fileName);
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("" + m3dresource.header.Keys.Length + " entries");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File",GUILayout.Width(250));
                EditorGUILayout.LabelField("Size",GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < m3dresource.header.Keys.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(m3dresource.header.Keys[i],GUILayout.Width(250));
                //EditorGUILayout.TextField(m3dresource.header.positions[i].ToString(),GUILayout.Width(100));
                float kB = ((float)m3dresource.header.Lengths[i]) / 1024f;
                EditorGUILayout.TextField(kB.ToString("F2") + "kB",GUILayout.Width(100));
                bool export = GUILayout.Button("Export",GUILayout.Width(100));
                if (export)
                {
                    string outputFile = m3dresource.header.DirectoryPath + "/" + m3dresource.header.Keys[i];
                    UnityEngine.Debug.Log("Exporting to: " + outputFile);
                    m3dresource.UnpackResource(m3dresource.header.Keys[i]);
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }

        } catch
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            GUILayout.Label("Can't parse resource file, it appears corrupted.");
        }
    }
}



[InitializeOnLoad]
public class M3DResourceFileGlobal
{
    private static M3DResourceFileWrapper wrapper = null;
    private static bool selectionChanged = false;

    static M3DResourceFileGlobal()
    {
        Selection.selectionChanged += SelectionChanged;
        EditorApplication.update += Update;
    }

    private static void SelectionChanged()
    {
        selectionChanged = true;
        // can't do the wrapper stuff here. it does not work 
        // when you Selection.activeObject = wrapper
        // so do it in Update
    }

    private static void Update()
    {
        if (selectionChanged == false) return;

        selectionChanged = false;
        if (Selection.activeObject != wrapper)
        {
            if (Selection.objects.Length > 1)
            {
                //they have multiple files selected
                return;
            }

            Object[] objects = Selection.objects;
            int[] instanceIds = new int[objects.Length + 1];
            Object[] newObjects = new Object[objects.Length + 1];

            for(int i = 0; i < objects.Length; i++)
            {
                newObjects[i] = objects[i];
                instanceIds[i] = objects[i].GetInstanceID();
            }

            string fn = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (fn.ToLower().EndsWith(".mr"))
            {
                if (wrapper == null)
                {
                    wrapper = ScriptableObject.CreateInstance<M3DResourceFileWrapper>();
                    wrapper.hideFlags = HideFlags.DontSave;
                }
                newObjects[objects.Length] = wrapper;
                instanceIds[objects.Length] = wrapper.GetInstanceID();
                //newObjects[0] = wrapper;

                wrapper.fileName = fn;
                Selection.activeObject = wrapper;
                //Selection.objects = newObjects;



                //Selection.activeObject = wrapper;
                //Selection.activeInstanceID = wrapper.GetInstanceID();
                //Selection.instanceIDs = instanceIds;

                //Editor[] ed = Resources.FindObjectsOfTypeAll<M3DResourceEditor>();
                //UnityEngine.Debug.Log("ED:" + ed.Length);
                //if (ed.Length > 0) ed[0].Repaint();

                //EditorUtility.SetDirty(wrapper);

                /*
                ed = Resources.FindObjectsOfTypeAll<Editor>();
                UnityEngine.Debug.Log("ED 2:" + ed.Length);
                if (ed.Length > 0) ed[0].Repaint();
                */
            }
        }
    }
}

// M3DResourceFileWrapper.cs 
public class M3DResourceFileWrapper : ScriptableObject
{
    [System.NonSerialized]
    public string fileName; // path is relative to Assets/
}