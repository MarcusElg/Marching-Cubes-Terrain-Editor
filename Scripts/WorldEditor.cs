using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(World))]
public class WorldEditor : Editor
{

    World world;
    Tool lastTool;

    void OnEnable()
    {
        lastTool = Tools.current;
        Tools.current = Tool.None;
        world = (World)target;

        world.densityGenerator = new DensityGenerator(world.seed);

        if (world.transform.childCount == 0)
        {
            world.AddChunks(Vector3Int.zero, Vector3Int.zero);
        }
    }

    private void OnDisable()
    {
        Tools.current = lastTool;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Modify"))
        {
            serializedObject.FindProperty("terrainMode").enumValueIndex = 0;
        }
        else if (GUILayout.Button("Set"))
        {
            serializedObject.FindProperty("terrainMode").enumValueIndex = 1;
        }
        if (GUILayout.Button("Smooth"))
        {
            serializedObject.FindProperty("terrainMode").enumValueIndex = 2;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Options"))
        {
            serializedObject.FindProperty("terrainMode").enumValueIndex = 3;
        }

        GUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        if (serializedObject.FindProperty("terrainMode").enumValueIndex == 0)
        {
            InspectorModify();
        }
        else if (serializedObject.FindProperty("terrainMode").enumValueIndex == 1)
        {
            InspectorSet();
        }
        else if (serializedObject.FindProperty("terrainMode").enumValueIndex == 3)
        {
            InspectorOptions();
        }

        // Shared
        DrawLine();

        if (GUILayout.Button("Generate Terrain"))
        {
            world.Generate();
        }

        if (GUILayout.Button("Reset Terrain"))
        {
            ResetTerrain();
        }

        if (GUILayout.Button("Remove All Chunks"))
        {
            world.RemoveChunks();
            world.AddChunks(Vector3Int.zero, Vector3Int.zero);
        }
    }

    private void InspectorModify()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("range").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Range", serializedObject.FindProperty("range").floatValue), 0.1f, serializedObject.FindProperty("chunkSize").intValue * 0.75f * world.transform.lossyScale.x);
        serializedObject.FindProperty("force").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Force", serializedObject.FindProperty("force").floatValue), 0.1f, 10f);
        serializedObject.FindProperty("forceOverDistance").animationCurveValue = EditorGUILayout.CurveField("Force Over Distance", serializedObject.FindProperty("forceOverDistance").animationCurveValue);

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private void InspectorSet()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("range").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Range", serializedObject.FindProperty("range").floatValue), 0.1f, serializedObject.FindProperty("chunkSize").intValue * 0.75f * world.transform.lossyScale.x);
        serializedObject.FindProperty("forceOverDistance").animationCurveValue = EditorGUILayout.CurveField("Force Over Distance", serializedObject.FindProperty("forceOverDistance").animationCurveValue);
        serializedObject.FindProperty("targetHeight").intValue = Mathf.Clamp(EditorGUILayout.IntField("Target Height", serializedObject.FindProperty("targetHeight").intValue), 0, serializedObject.FindProperty("chunkSize").intValue);

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private void InspectorOptions()
    {
        GUIStyle boldStyle = new GUIStyle();
        boldStyle.fontStyle = FontStyle.Bold;

        GUILayout.Label("Options", boldStyle);
        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("isoLevel").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Iso Level", serializedObject.FindProperty("isoLevel").floatValue), 0.01f, 0.99f);

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            world.Generate();
        }

        EditorGUI.BeginChangeCheck();

        GUILayout.Space(20);
        GUILayout.Label("Add Chunks", boldStyle);
        serializedObject.FindProperty("chunkStartIndexToAdd").vector3IntValue = ClampVector3(EditorGUILayout.Vector3IntField("Start Chunk Index", serializedObject.FindProperty("chunkStartIndexToAdd").vector3IntValue), Vector3.zero, new Vector3(100, 10, 100));
        serializedObject.FindProperty("chunkEndIndexToAdd").vector3IntValue = ClampVector3(EditorGUILayout.Vector3IntField("End Chunk Index", serializedObject.FindProperty("chunkEndIndexToAdd").vector3IntValue), serializedObject.FindProperty("chunkStartIndexToAdd").vector3IntValue, new Vector3(100, 10, 100));

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        if (GUILayout.Button("Add"))
        {
            world.AddChunks(serializedObject.FindProperty("chunkStartIndexToAdd").vector3IntValue, serializedObject.FindProperty("chunkEndIndexToAdd").vector3IntValue);
        }

        EditorGUI.BeginChangeCheck();
        GUILayout.Space(20);
        GUILayout.Label("Resetting options", boldStyle);
        GUILayout.Label("Warning: changing these options will reset your terrain without undo support");
        serializedObject.FindProperty("chunkSize").intValue = Mathf.Clamp(EditorGUILayout.IntField("Chunk Divisions", serializedObject.FindProperty("chunkSize").intValue), 1, 50);

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            world.ChangeChunkSizes();
        }

        EditorGUI.BeginChangeCheck();
        serializedObject.FindProperty("groundHeight").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Ground Height", serializedObject.FindProperty("groundHeight").floatValue), 0, serializedObject.FindProperty("chunkSize").intValue);
        serializedObject.FindProperty("generateNoise").boolValue = EditorGUILayout.Toggle("Generate Noise", serializedObject.FindProperty("generateNoise").boolValue);

        if (serializedObject.FindProperty("generateNoise").boolValue == true)
        {
            serializedObject.FindProperty("noiseScale").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Noise Scale", serializedObject.FindProperty("noiseScale").floatValue), 0.1f, 5f * world.transform.lossyScale.x);
            serializedObject.FindProperty("noiseStretch").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Noise Stretch", serializedObject.FindProperty("noiseStretch").floatValue), 0.1f, 5f * world.transform.lossyScale.x);

            serializedObject.FindProperty("randomizeSeed").boolValue = EditorGUILayout.Toggle("Randomize Seed", serializedObject.FindProperty("randomizeSeed").boolValue);
            if (serializedObject.FindProperty("randomizeSeed").boolValue == false)
            {
                serializedObject.FindProperty("seed").intValue = Mathf.Clamp(EditorGUILayout.IntField("Seed", serializedObject.FindProperty("seed").intValue), 0, 100000);
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkPrefab"), new GUIContent("Chunk Prefab"));

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            ResetTerrain();
        }
    }

    private void OnSceneGUI()
    {
        HandleUtility.nearestControl = GUIUtility.GetControlID(FocusType.Passive);

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit raycastHit;

        if (world.transform.hasChanged == true)
        {
            world.transform.localScale = new Vector3(world.transform.localScale.x, world.transform.localScale.x, world.transform.localScale.x);
            world.transform.localRotation = Quaternion.identity;
            world.Generate();
            world.transform.hasChanged = false;
        }

        // Brushes
        if (Event.current.shift == false)
        {
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.GetComponent<Chunk>() != null)
                {
                    World world = raycastHit.transform.parent.GetComponent<World>();

                    if (world.terrainMode == World.TerrainMode.Modify)
                    {
                        Handles.color = Color.white;
                        Handles.DrawWireDisc(raycastHit.point, Vector3.up, world.range);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            bool raise = true;

                            if (Event.current.button == 1)
                            {
                                raise = false;
                            }
                            else if (Event.current.button == 2)
                            {
                                return;
                            }

                            TerrainEditor.ModifyTerrain(world, raycastHit.point, raise);
                        }
                    }
                    else if (world.terrainMode == World.TerrainMode.Set)
                    {
                        Handles.color = Color.white;
                        Handles.DrawWireDisc(raycastHit.point, Vector3.up, world.range);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 2)
                            {
                                world.targetHeight = Mathf.RoundToInt(raycastHit.point.y - raycastHit.transform.position.y);
                                return;
                            }

                            if (Event.current.button == 1)
                            {
                                return;
                            }

                            TerrainEditor.SetTerrain(world, raycastHit.point);
                        }
                    }
                }

                SceneView.currentDrawingSceneView.Repaint();
            }
        }
    }

    private void ResetTerrain()
    {
        if (world == null)
        {
            world = (World)target;
        }

        if (world.randomizeSeed == true)
        {
            world.seed = Random.Range(0, 10000);
        }

        for (int i = 0; i < world.transform.childCount; i++)
        {
            world.transform.GetChild(i).GetComponent<Chunk>().ResetPoints(world);
        }

        world.Generate();
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.black;
        Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.width, rect.y));
        EditorGUILayout.EndHorizontal();
    }

    private Vector3Int ClampVector3(Vector3 inVector, Vector3 min, Vector3 max)
    {
        return new Vector3Int(Mathf.RoundToInt(Mathf.Clamp(inVector.x, min.x, max.x)), Mathf.RoundToInt(Mathf.Clamp(inVector.y, min.y, max.y)), Mathf.RoundToInt(Mathf.Clamp(inVector.z, min.z, max.z)));
    }

}
