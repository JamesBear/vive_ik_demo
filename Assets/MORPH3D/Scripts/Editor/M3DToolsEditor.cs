using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using MORPH3D;

namespace MORPH3D
{
    public class M3DToolsEditor : Editor
    {

        [MenuItem("MORPH 3D/Reimport all mon files from selection")]
        public static void MenuItemReimportAllMonFilesFromSelected()
        {
            string[] guids = Selection.assetGUIDs;

            List<string> monPaths = new List<string>();

            string projectPath = Application.dataPath.Replace("/Assets", "");

            for(int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FileAttributes attr = File.GetAttributes(path);
                if((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string[] paths = Directory.GetFiles(path, "*.mon", SearchOption.AllDirectories);
                    for(int j=0;j<paths.Length;j++)
                    {
                        string tmpPath = paths[j].Replace(@"\",@"/");
                        tmpPath = tmpPath.Replace(projectPath, "");
                        monPaths.Add(tmpPath);
                    }
                } else
                {
                    if (path.EndsWith(".mon"))
                    {
                        monPaths.Add(path);
                    }
                }
            }

            foreach(string monPath in monPaths)
            {
                UnityEngine.Debug.Log("Importing: " + monPath);
                AssetDatabase.ImportAsset(monPath);
            }
        }

    }

}