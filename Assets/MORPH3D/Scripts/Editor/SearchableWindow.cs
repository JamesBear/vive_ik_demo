using UnityEngine;
using UnityEditor;

public class SearchableWindow : EditorWindow
{
	private static SearchableWindow windowInstance;
	private static Vector2 _scroll;
	public delegate void SetResult(string newResult);

	private SetResult action;
	private string[] options;

	private string searchString = "";
	
	public static void Init (SetResult action, string[] options)
	{
		if(SearchableWindow.windowInstance != null)
			SearchableWindow.windowInstance.Close();
		
		windowInstance = EditorWindow.GetWindow <SearchableWindow>(true, "Search Window", true);
		windowInstance.action = action;
		windowInstance.options = options;
	}
	
	void OnGUI ()
	{
		EditorGUILayout.Space();

		GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
		searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
		if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
		{
			searchString = "";
			GUI.FocusControl(null);
		}
		GUILayout.EndHorizontal();
		
		_scroll = EditorGUILayout.BeginScrollView(_scroll);		
		EditorGUILayout.Space();
		foreach(string option in options)
		{
			if(searchString != "" && option.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) < 0)
				continue;
			GUILayout.BeginHorizontal();

            string display = option;

            int pos = option.LastIndexOf("|");
            if (pos >= 0)
            {
                display = option.Substring(0, pos);
            }

			EditorGUILayout.LabelField(display);
			if(GUILayout.Button("Select", GUILayout.Width(75)))
			{
                UnityEngine.Debug.Log("Selected: " + option);
				action(option);
				windowInstance.Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}
	
	void OnDestroy()
	{
		windowInstance = null;
	}
	
	void OnEnable()
	{
		windowInstance = this;
	}
	
	void OnInspectorUpdate()
	{
		Repaint();
	}
}