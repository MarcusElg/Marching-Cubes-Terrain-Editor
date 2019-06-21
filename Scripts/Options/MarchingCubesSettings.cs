using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MarchingCubesSettings : ScriptableObject
{
    public bool hideNonEditableChildren = true;

    public static MarchingCubesSettings GetOrCreateSettings()
    {
        MarchingCubesSettings settings = AssetDatabase.LoadAssetAtPath<MarchingCubesSettings>("Assets/Editor/MarchingCubesSettings.asset");
        if (settings == null)
        {
            if (Directory.Exists("Assets/Editor") == false)
            {
                Directory.CreateDirectory("Assets/Editor");
            }

            settings = ScriptableObject.CreateInstance<MarchingCubesSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Editor/MarchingCubesSettings.asset");
            AssetDatabase.SaveAssets();
        }

        return settings;
    }

    public static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }

}
