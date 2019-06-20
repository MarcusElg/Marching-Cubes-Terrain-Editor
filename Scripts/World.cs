using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public float range = 2f;
    public float force = 2f;
    public AnimationCurve forceOverDistance = AnimationCurve.Constant(0, 1, 1);

    public int targetHeight = 10;

    public int chunkSize = 8;
    public float groundHeight = 10;

    public float isoLevel;

    public bool randomizeSeed = true;
    public int seed;
    public GameObject chunkPrefab;
    public bool generateNoise = true;
    public float noiseScale = 1;
    public float noiseStretch = 1;

    public DensityGenerator densityGenerator;
    public enum TerrainMode { Modify, Set, Smooth, Paint, Options };
    public TerrainMode terrainMode = TerrainMode.Set;

    public Vector3Int chunkStartIndexToAdd;
    public Vector3Int chunkEndIndexToAdd;

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

    public void RemoveChunks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    public Chunk GetChunk(Vector3 position)
    {
        return GetChunk(position.x, position.y, position.z);
    }

    public Chunk GetChunk(float x, float y, float z)
    {
        RaycastHit raycastHit;

        if (Physics.Raycast(new Vector3(x, y, z), Vector3.down, out raycastHit, LayerMask.GetMask("Chunk")))
        {
            if (raycastHit.transform.GetComponent<Chunk>() != null)
            {
                return raycastHit.transform.GetComponent<Chunk>();
            }
        }

        return null;
    }

    public Point GetPoint(Vector3Int pos)
    {
        return GetPoint(pos.x, pos.y, pos.z);
    }

    public Point GetPoint(int x, int y, int z)
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
            Point p = new Point(new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue), 0);
            return p;
        }
    }

    private void CreateChunk(int x, int y, int z)
    {
        bool spaceEmpty = true;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<Chunk>().chunkIndex == new Vector3(x, y, z))
            {
                spaceEmpty = false;
            }
        }

        if (spaceEmpty == true)
        {
            Chunk chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
            chunk.transform.localPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
            chunk.Initialize(this, new Vector3Int(x, y, z));
            chunk.Generate(this);
        }
    }

    public void ChangeChunkSizes()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Chunk>().transform.position = transform.position + Vector3.Scale(transform.GetChild(i).GetComponent<Chunk>().chunkIndex, new Vector3(chunkSize * transform.lossyScale.x, chunkSize * transform.lossyScale.x, chunkSize * transform.lossyScale.x));
            transform.GetChild(i).GetComponent<Chunk>().points = new Point[(int)Mathf.Pow(chunkSize + 1, 3)];
            transform.GetChild(i).GetComponent<Chunk>().UpdateAfterReload(this);
            transform.GetChild(i).GetComponent<Chunk>().Generate(this);
        }
    }
}
