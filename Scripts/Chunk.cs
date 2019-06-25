using UnityEngine;

public class Chunk : MonoBehaviour
{

    [SerializeField]
    [HideInInspector]
    public Point[] points;
    public Vector3Int chunkIndex;

    public MarchingCubes _marchingCubes;
    public DensityGenerator _densityGenerator;

    public void Initialize(World world, Vector3Int chunkIndex)
    {
        transform.SetParent(world.transform, false);
        transform.localScale = Vector3.one;
        name = chunkIndex.ToString();
        gameObject.layer = LayerMask.NameToLayer("Chunk");

        if (world.settings.FindProperty("hideNonEditableChildren").boolValue == true)
        {
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            gameObject.hideFlags = HideFlags.NotEditable;
        }

        this.chunkIndex = chunkIndex;
        points = new Point[(int)Mathf.Pow(world.chunkSize + 1, 3)];

        // Collider
        GameObject boundsObject = new GameObject("Bounds");
        boundsObject.transform.SetParent(transform, false);
        boundsObject.layer = LayerMask.NameToLayer("Chunk Bounds");
        boundsObject.hideFlags = HideFlags.NotEditable;
        boundsObject.AddComponent<BoxCollider>();
        boundsObject.GetComponent<BoxCollider>().center = new Vector3(world.chunkSize * world.transform.lossyScale.x / 4, world.chunkSize * world.transform.lossyScale.x / 4, world.chunkSize * world.transform.lossyScale.x / 4);
        boundsObject.GetComponent<BoxCollider>().size = new Vector3(world.chunkSize * world.transform.lossyScale.x / 2, world.chunkSize * world.transform.lossyScale.x / 2, world.chunkSize * world.transform.lossyScale.x / 2);

        ResetPoints(world);
    }

    public void ResetPoints(World world)
    {
        UpdateAfterReload(world);

        for (int x = 0; x < world.chunkSize + 1; x++)
        {
            for (int y = 0; y < world.chunkSize + 1; y++)
            {
                for (int z = 0; z < world.chunkSize + 1; z++)
                {
                    points[x + (world.chunkSize + 1) * (y + z * (world.chunkSize + 1))] = new Point(new Vector3Int(x, y, z), _densityGenerator.CalculateDensity(world, world.transform.lossyScale.x * x + transform.position.x, world.transform.lossyScale.x * y + transform.position.y, world.transform.lossyScale.x * z + transform.position.z), Color.green);
                }
            }
        }
    }

    public void UpdateAfterReload(World world)
    {
        _densityGenerator = world.densityGenerator;
        _marchingCubes = new MarchingCubes(world, points, world.seed);
    }

    public void Generate(World world)
    {
        if (_marchingCubes == null)
        {
            UpdateAfterReload(transform.parent.GetComponent<World>());
        }

        Mesh mesh = _marchingCubes.CreateMeshData(world, points);

        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public Point GetPoint(World world, Vector3 pos)
    {
        return GetPoint(world, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    }

    public Point GetPoint(World world, Vector3Int pos)
    {
        return GetPoint(world, pos.x, pos.y, pos.z);
    }

    public Point GetPoint(World world, int x, int y, int z)
    {
        return points[x + (world.chunkSize + 1) * (y + z * (world.chunkSize + 1))];
    }

    public void SetDensity(World world, float density, int x, int y, int z)
    {
        x = Mathf.Clamp(x, 0, world.chunkSize);
        y = Mathf.Clamp(y, 0, world.chunkSize);
        z = Mathf.Clamp(z, 0, world.chunkSize);
        points[x + (world.chunkSize + 1) * (y + z * (world.chunkSize + 1))].density = density;
    }

    public void SetDensity(World world, float density, Vector3 position)
    {
        SetDensity(world, density, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
    }

    public void SetColor(World world, Color colour, Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(position.x), 0, world.chunkSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.y), 0, world.chunkSize);
        int z = Mathf.Clamp(Mathf.RoundToInt(position.z), 0, world.chunkSize);
        points[x + (world.chunkSize + 1) * (y + z * (world.chunkSize + 1))].colour = colour;
    }
}
