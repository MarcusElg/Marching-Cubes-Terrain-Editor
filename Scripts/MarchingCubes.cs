using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes
{
    private Vector3[] _vertices;
    private int[] _triangles;
    private List<Color> _colours;
    private float _isoLevel;

    private int _vertexIndex;

    private Vector3[] _vertexList;
    private Point[] _initPoints;
    private Mesh _mesh;
    private int[,,] _cubeIndexes;

    private readonly Vector3 zero = Vector3.zero;

    public MarchingCubes(World world, Point[] points, float isoLevel, int seed)
    {
        _isoLevel = isoLevel;

        _mesh = new Mesh();

        _vertexIndex = 0;

        _vertexList = new Vector3[12];
        _initPoints = new Point[8];
        int amount = world.chunkSize + 1;
        _cubeIndexes = new int[amount, amount, amount];
    }

    private Vector3 VertexInterpolate(Vector3 p1, Vector3 p2, float v1, float v2)
    {
        if (Utils.Abs(_isoLevel - v1) < 0.000001f)
        {
            return p1;
        }
        if (Utils.Abs(_isoLevel - v2) < 0.000001f)
        {
            return p2;
        }
        if (Utils.Abs(v1 - v2) < 0.000001f)
        {
            return p1;
        }

        float mu = (_isoLevel - v1) / (v2 - v1);

        Vector3 p = p1 + mu * (p2 - p1);

        return p;
    }

    private List<int> March(Point[] points, int cubeIndex)
    {
        int edgeIndex = LookupTables.EdgeTable[cubeIndex];

        List<int> pointIndexes = new List<int>();
        _vertexList = GenerateVertexList(points, edgeIndex, ref pointIndexes);

        int[] row = LookupTables.TriangleTable[cubeIndex];

        for (int i = 0; i < row.Length; i += 3)
        {
            _vertices[_vertexIndex] = _vertexList[row[i + 0]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 1]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 2]];
            _triangles[_vertexIndex] = _vertexIndex;
            _vertexIndex++;
        }

        return pointIndexes;
    }

    private Vector3[] GenerateVertexList(Point[] points, int edgeIndex, ref List<int> pointIndexes)
    {
        for (int i = 0; i < 12; i++)
        {
            if ((edgeIndex & (1 << i)) != 0)
            {
                int[] edgePair = LookupTables.EdgeIndexTable[i];
                int edge1 = edgePair[0];
                int edge2 = edgePair[1];

                Point point1 = points[edge1];
                Point point2 = points[edge2];

                pointIndexes.Add(edge1);
                pointIndexes.Add(edge2);

                _vertexList[i] = VertexInterpolate(point1.localPosition, point2.localPosition, point1.density, point2.density);
            }
        }

        return _vertexList;
    }

    private int CalculateCubeIndex(Point[] points, float iso)
    {
        int cubeIndex = 0;

        for (int i = 0; i < 8; i++)
            if (points[i].density < iso)
                cubeIndex |= 1 << i;

        return cubeIndex;
    }

    public Mesh CreateMeshData(World world, Point[] points)
    {
        _cubeIndexes = GenerateCubeIndexes(world, points);
        int vertexCount = GenerateVertexCount(_cubeIndexes);

        if (vertexCount <= 0)
        {
            return new Mesh();
        }

        _vertices = new Vector3[vertexCount];
        _triangles = new int[vertexCount];
        _colours = new List<Color>();

        for (int x = 0; x < world.chunkSize; x++)
        {
            for (int y = 0; y < world.chunkSize; y++)
            {
                for (int z = 0; z < world.chunkSize; z++)
                {
                    int cubeIndex = _cubeIndexes[x, y, z];
                    if (cubeIndex == 0 || cubeIndex == 255) continue;

                    Point[] points2 = GetPoints(world, x, y, z, points);
                    List<int> pointIndexes = March(points2, cubeIndex);
                    for (int i = 0; i < pointIndexes.Count - 3; i+=4)
                    {
                        _colours.Add(points2[pointIndexes[i]].colour);
                        _colours.Add(points2[pointIndexes[i]].colour);
                        _colours.Add(points2[pointIndexes[i]].colour);
                    }
                }
            }
        }

        _vertexIndex = 0;

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.SetTriangles(_triangles, 0);
        _mesh.SetColors(_colours);
        _mesh.RecalculateNormals();

        return _mesh;
    }

    private Point[] GetPoints(World world, int x, int y, int z, Point[] points)
    {
        for (int i = 0; i < 8; i++)
        {
            Point p = points[x + CubePointsX[i] + (world.chunkSize + 1) * (y + CubePointsY[i] + (z + CubePointsZ[i]) * (world.chunkSize + 1))];
            _initPoints[i] = p;
        }

        return _initPoints;
    }

    private int[,,] GenerateCubeIndexes(World world, Point[] points)
    {
        for (int x = 0; x < world.chunkSize; x++)
        {
            for (int y = 0; y < world.chunkSize; y++)
            {
                for (int z = 0; z < world.chunkSize; z++)
                {
                    _initPoints = GetPoints(world, x, y, z, points);

                    _cubeIndexes[x, y, z] = CalculateCubeIndex(_initPoints, _isoLevel);
                }
            }
        }

        return _cubeIndexes;
    }


    private int GenerateVertexCount(int[,,] cubeIndexes)
    {
        int vertexCount = 0;

        for (int x = 0; x < cubeIndexes.GetLength(0); x++)
        {
            for (int y = 0; y < cubeIndexes.GetLength(1); y++)
            {
                for (int z = 0; z < cubeIndexes.GetLength(2); z++)
                {
                    int cubeIndex = cubeIndexes[x, y, z];
                    int[] row = LookupTables.TriangleTable[cubeIndex];
                    vertexCount += row.Length;
                }
            }
        }

        return vertexCount;
    }

    public static readonly Vector3Int[] CubePoints =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    public static readonly int[] CubePointsX =
    {
        0,
        1,
        1,
        0,
        0,
        1,
        1,
        0
    };

    public static readonly int[] CubePointsY =
    {
        0,
        0,
        0,
        0,
        1,
        1,
        1,
        1
    };

    public static readonly int[] CubePointsZ =
    {
        0,
        0,
        1,
        1,
        0,
        0,
        1,
        1
    };
}