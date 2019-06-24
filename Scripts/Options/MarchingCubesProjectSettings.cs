using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MarchingCubesProjectSettings
{

    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        SettingsProvider settingsProvider = new SettingsProvider("Project/MarchingCubes", SettingsScope.Project)
        {
            label = "Marching Cubes",

            guiHandler = (searchContext) =>
            {
                SerializedObject settings = MarchingCubesSettings.GetSerializedSettings();
                EditorGUI.BeginChangeCheck();
                settings.FindProperty("hideNonEditableChildren").boolValue = EditorGUILayout.Toggle("Hide Non-editable Children", settings.FindProperty("hideNonEditableChildren").boolValue);
                settings.FindProperty("linePreviewColour").colorValue = EditorGUILayout.ColorField("Line Preview Colour", settings.FindProperty("linePreviewColour").colorValue);

                if (EditorGUI.EndChangeCheck() == true)
                {
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    World[] worlds = GameObject.FindObjectsOfType<World>();

                    for (int i = 0; i < worlds.Length; i++)
                    {
                        worlds[i].settings = MarchingCubesSettings.GetSerializedSettings();
                    }
                }
            }
        };

        return settingsProvider;
    }
}
