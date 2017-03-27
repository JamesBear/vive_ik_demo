using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MORPH3D.CONSTANTS;
using MORPH3D.FOUNDATIONS;
using MORPH3D.COSTUMING;
using MORPH3D.UTILITIES;
using MORPH3D.CORESERVICES;
using MORPH3D;

using Morph3d.Utility.Schematic;
using M3D_DLL;

public class MaterialImporter : AssetPostprocessor
{


    #region MATERIALS
    Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        //UnityEngine.Debug.LogError("asset: " + assetPath + " renderer: " + renderer.name + " mat: " + material.name);
        string path = StripFileName(assetPath);

        if (!assetPath.Contains("Assets/MORPH3D/"))
        {
            //we don't control this asset, so use the default processing
            return null;
        }

        //TODO: support non figure stuff

        if(!assetPath.Contains("M3DFemale.fbx") && !assetPath.Contains("M3DMale.fbx"))
        {
            return null;
        }

        material = FindLoadAndAssignFigureMaterial(material, renderer.name, path);
        return material;



        //TODO: support this legacy piece
        /*

        if (assetPath.Contains("Assets/MORPH3D/"))
        {

            string geometry_name = renderer.name;
            if (geometry_name.Contains("."))
            {

                geometry_name = geometry_name.Substring(0, geometry_name.LastIndexOf("."));//all lods share material

                //exception for hair
                if (renderer.name.Contains("Shape_opaque"))
                {
                    geometry_name += "_opaque";
                }
                if (renderer.name.Contains("Shape_feathered"))
                {
                    geometry_name += "_feathered";
                }
            }

            string go_name = GetFilename(assetPath);
            //				Debug.Log(go_name);

            Material existing_material = LoadExistingMaterial(material.name, geometry_name, go_name);
            if (existing_material == null)
            {
                return CreatePersistentMaterial(geometry_name, path, material);
            }
            else
            {
                return existing_material;
            }


        }
        return material;
        */
    }
    #endregion

    Material FindLoadAndAssignFigureMaterial(Material fbxMaterial, string rendererName, string dirPath)
    {
        Material dstMaterial;
        bool useLods = true;
        string materialName = fbxMaterial.name;
        if (materialName == "EyeSheen" || materialName == "EyeAndLash")
        {
            materialName = "EyeAndLash";
            useLods = false;
        }
        string lodSuffix = "";
        if (useLods)
        {
            int lodPos = rendererName.LastIndexOf("_LOD");
            lodSuffix = rendererName.Substring(lodPos);
        }

        string dstMatBaseName = materialName + lodSuffix;

        string matPath = dirPath + "/Materials/" + dstMatBaseName + ".mat";

        //UnityEngine.Debug.Log("MatPath: " + matPath);

        dstMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(dstMaterial != null)
        {
            return dstMaterial;
        }

        string dirMon = dirPath + "/Materials";

        string monPath = dirPath + "/Materials/" + dstMatBaseName + ".mon";
        //AssetDatabase.ImportAsset(monPath, ImportAssetOptions.Default | ImportAssetOptions.ForceSynchronousImport);
        MonDeserializer monDes = new MonDeserializer();
        AssetSchematic[] schematics = monDes.DeserializeMonFile(monPath);
        AssetCreator assetCreator = new AssetCreator();
        foreach(AssetSchematic schematic in schematics)
        {
            if (schematic.type_and_function.primary_function != Morph3d.Utility.Schematic.Enumeration.PrimaryFunction.material)
            {
                continue;
            }
            Material newMaterial = assetCreator.CreateMorphMaterial(schematic, TextureLoader.GetTextures(schematic, dirMon));

            if (schematic.stream_and_path.generated_path == "" || schematic.stream_and_path.generated_path == null)
            {
                schematic.stream_and_path.generated_path = dirMon + "/" + schematic.origin_and_description.name + ".mat";
            }

            int pos = schematic.stream_and_path.generated_path.LastIndexOf('/');
            string directoryPath = schematic.stream_and_path.generated_path.Substring(0, pos);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            //does a material already exist, if so, replace over it
            string dstPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(dirMon, schematic.stream_and_path.generated_path);
            if (!File.Exists(dstPath))
            {
                AssetDatabase.CreateAsset(newMaterial, dstPath);
            }
            else
            {
                Material oldMat = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
                oldMat.CopyPropertiesFromMaterial(newMaterial);
            }
            dstMaterial = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
            return dstMaterial;
        }

        dstMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);


        return dstMaterial;
    }



    Material CreatePersistentMaterial(string geometry_name, string current_path, Material material)
    {
        //			Debug.Log ("CREATIN AMTERIAL");
        string mat_ext = ".mat";
        string material_folder = "/M3DMaterials";
        if (AssetDatabase.IsValidFolder(current_path + material_folder) == false)
            AssetDatabase.CreateFolder(current_path, "M3DMaterials");
        string material_path = current_path + material_folder + "/" + geometry_name + mat_ext;
        material.shader = Shader.Find("Standard");
        AssetDatabase.CreateAsset(material, material_path);
        return material;
    }

    Material LoadExistingMaterial(string mat_name, string geometry_name, string go_name)
    {
        Material loaded_material = null;
        string mat_ext = ".mat";
        string path = StripFileName(assetPath);
        string local_materials_folder = path + "/M3DMaterials/";
        string shared_materials_folder = "Assets/MORPH3D/Content/SharedMaterials/";
        //local materials mat name
        //			Debug.Log("LAODING MAT FROM LOCAL Mat");

        loaded_material = AssetDatabase.LoadAssetAtPath<Material>(local_materials_folder + mat_name + mat_ext);
        //shared materials mat name
        if (loaded_material == null)
        {
            //				Debug.Log("LOADING MAT FROM Shared MAT");
            loaded_material = AssetDatabase.LoadAssetAtPath<Material>(shared_materials_folder + mat_name + mat_ext);
        }
        //local materials geometry name
        if (loaded_material == null)
        {
            //				Debug.Log("LOADING MAT FROM LOCAL GEOMETRY");
            loaded_material = AssetDatabase.LoadAssetAtPath<Material>(local_materials_folder + geometry_name + mat_ext);
        }
        //shared materials geometry name
        if (loaded_material == null)
        {
            //				Debug.Log("LOADING MAT FROM SHARED GEOMETRY");
            loaded_material = AssetDatabase.LoadAssetAtPath<Material>(shared_materials_folder + geometry_name + mat_ext);
        }
        //local materials gameobject name
        if (loaded_material == null)
        {
            //				Debug.Log("LOADING MAT FROM LOCAL GAMEOBJECT");

            loaded_material = AssetDatabase.LoadAssetAtPath<Material>(local_materials_folder + go_name + mat_ext);
        }
        //shared materials gamobejct name
        if (loaded_material == null)
        {
            //				Debug.Log("LOADING MAT FROM SHARED GAMEOBJECT");
            loaded_material = AssetDatabase.LoadAssetAtPath<Material>(shared_materials_folder + go_name + mat_ext);
        }


        return loaded_material;
    }





    string StripFileName(string path)
    {
        return path.Substring(0, path.LastIndexOf("/"));
    }


    string GetFilename(string path)
    {
        string file_name = path.Substring(path.LastIndexOf("/") + 1);
        file_name = file_name.Substring(0, file_name.LastIndexOf("."));
        return file_name;
    }

}

