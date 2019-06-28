using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class World : MonoBehaviour
{
    /// Modify
    public float range = 2;
    public float force = 2f;

    // Set
    public float targetHeight = 10f;

    // Line
    public Vector3 startPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    public Vector3 endPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    public bool addTerrain = false;
    public bool flatFloor = false;
    public bool clearAbove = false;

    // Paint
    public Color colour;
    public bool useColourMask = false;
    public Color colourMask;
    public float colourMaskTolerance = 0.01f;

    public int chunkSize = 8;
    public int maxHeightIndex = 1;
    public float groundHeight = 10;

    public bool randomizeSeed = true;
    public int seed;
    public GameObject chunkPrefab;
    public bool generateNoise = true;
    public float noiseScale = 1;
    public float noiseStretch = 1;

    public bool editMode = true;

    public DensityGenerator densityGenerator;
    public enum TerrainMode { Modify, Set, Line, Smooth, Paint, Options };
    public TerrainMode terrainMode = TerrainMode.Set;

    public Vector3Int chunkStartIndexToAdd;
    public Vector3Int chunkEndIndexToAdd;

    public SerializedObject settings;

    public void Generate()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Chunk>().UpdateAfterReload(this);
            transform.GetChild(i).GetComponent<Chunk>().Generate(this);
        }
    }

    public void AddChunks(Vector3Int startIndex, Vector3Int endIndex)
    {
        if (endIndex.y + 1 > maxHeightIndex)
        {
            maxHeightIndex = endIndex.y + 1;
        }

        for (int x = startIndex.x; x < endIndex.x + 1; x++)
        {
            for (int y = startIndex.y; y < endIndex.y + 1; y++)
            {
                for (int z = startIndex.z; z < endIndex.z + 1; z++)
                {
                    CreateChunk(x, y, z);
                }
            }
        }
    }

    public void RemoveChunks(Vector3Int startIndex, Vector3Int endIndex)
    {
        for (int x = startIndex.x; x < endIndex.x + 1; x++)
        {
            for (int y = startIndex.y; y < endIndex.y + 1; y++)
            {
                for (int z = startIndex.z; z < endIndex.z + 1; z++)
                {
                    Chunk chunk = GetChunk(transform.position + new Vector3(x * chunkSize * transform.lossyScale.x + chunkSize * transform.lossyScale.x * 0.5f, y * chunkSize * transform.lossyScale.y + chunkSize * transform.lossyScale.y * 0.5f, z * chunkSize * transform.lossyScale.z + chunkSize * transform.lossyScale.z * 0.5f));
                    if (chunk != null)
                    {
                        DestroyImmediate(chunk.gameObject);
                    }
                }
            }
        }

        // Fix new max height
        int currentMaxIndex = 1;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<Chunk>().chunkIndex.y + 1 > currentMaxIndex)
            {
                currentMaxIndex = transform.GetChild(i).GetComponent<Chunk>().chunkIndex.y + 1;
            }
        }

        maxHeightIndex = currentMaxIndex;

        if (transform.childCount == 0)
        {
            AddChunks(Vector3Int.zero, Vector3Int.zero);
        }
    }

    public void RemoveAllChunks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        maxHeightIndex = 1;
    }

    public Chunk GetChunk(Vector3 position)
    {
        return GetChunk(position.x, position.y, position.z);
    }

    public Chunk GetChunk(float x, float y, float z)
    {
        Collider[] colliders = Physics.OverlapBox(new Vector3(x, y, z), new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, LayerMask.GetMask("Chunk Bounds"));
        if (colliders.Length > 0 && colliders[0].transform.parent.GetComponent<Chunk>() != null)
        {
            return colliders[0].transform.parent.GetComponent<Chunk>();
        }

        return null;
    }

    public List<Chunk> GetChunks(float x, float y, float z)
    {
        return GetChunks(new Vector3(x, y, z));
    }

    public List<Chunk> GetChunks(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapBox(position, new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, LayerMask.GetMask("Chunk Bounds"));
        List<Chunk> chunks = new List<Chunk>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].transform.parent.GetComponent<Chunk>() != null)
            {
                chunks.Add(colliders[i].transform.parent.GetComponent<Chunk>());
            }
        }

        return chunks;
    }

    public CubePoint GetPoint(Vector3 pos)
    {
        return GetPoint(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    }

    public CubePoint GetPoint(Vector3Int pos)
    {
        return GetPoint(pos.x, pos.y, pos.z);
    }

    public CubePoint GetPoint(int x, int y, int z)
    {
        Chunk chunk = GetChunk(x, y, z);

        float newX = x / transform.lossyScale.x - transform.position.x;
        float newY = y / transform.lossyScale.x - transform.position.y;
        float newZ = z / transform.lossyScale.x - transform.position.z;

        if (chunk != null)
        {
            return chunk.GetPoint(this, newX.Mod(chunkSize),
                                     newY.Mod(chunkSize),
                                     newZ.Mod(chunkSize));
        }
        else
        {
            return new CubePoint(new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue), 0, Color.green);
        }
    }

    private void CreateChunk(int x, int y, int z)
    {
        bool spaceEmpty = true;

        if (GetChunk(transform.position + new Vector3(x * chunkSize * transform.lossyScale.x + chunkSize * transform.lossyScale.x * 0.5f, y * chunkSize * transform.lossyScale.y + chunkSize * transform.lossyScale.y * 0.5f, z * chunkSize * transform.lossyScale.z + chunkSize * transform.lossyScale.z * 0.5f)) != null)
        {
            spaceEmpty = false;
        }

        if (spaceEmpty == true)
        {
            Chunk chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
            chunk.transform.localPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
            chunk.Initialize(this, new Vector3Int(x, y, z));
            chunk.Generate(this);
        }
    }
}
