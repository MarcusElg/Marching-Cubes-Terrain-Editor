using System.Collections.Generic;
using UnityEngine;

public class TerrainEditor : MonoBehaviour
{

    public static void ModifyTerrain(World world, Vector3 point, bool addTerrain)
    {
        int buildModifier = addTerrain ? 1 : -1;

        int hitX = point.x.Round();
        int hitY = point.y.Round();
        int hitZ = point.z.Round();
        Vector3 hitPos = new Vector3(hitX, hitY, hitZ);

        int intRange = world.range.Ceil();

        // Dertermine what chunks to update
        Chunk centerChunk = world.GetChunk(point + new Vector3(0, 10, 0));
        Chunk topLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 10, world.range + 0.1f));
        Chunk topRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 10, world.range + 0.1f));
        Chunk bottomLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 10, -world.range - 0.1f));
        Chunk bottomRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 10, -world.range - 0.1f));

        for (int x = -intRange; x <= intRange; x++)
        {
            for (int y = -intRange; y <= intRange; y++)
            {
                for (int z = -intRange; z <= intRange; z++)
                {
                    int offsetedX = hitX - x;
                    int offsetedY = hitY - y;
                    int offsetedZ = hitZ - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    float modificationAmount = world.force / distance * world.forceOverDistance.Evaluate(1 - distance.Map(0, world.force, 0, 1)) * buildModifier;
                    Chunk chunk2 = world.GetChunk(offsetedX, offsetedY + 1, offsetedZ);

                    if (chunk2 != null)
                    {
                        Vector3 point2 = (new Vector3(offsetedX, offsetedY, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x;

                        float oldDensity = world.GetPoint(new Vector3Int(Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y), Mathf.RoundToInt(point2.z))).density;
                        float newDensity = oldDensity - modificationAmount;
                        newDensity = newDensity.Clamp01();

                        Point p = chunk2.GetPoint(world, new Vector3Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point2.y), Mathf.RoundToInt(point2.z)));

                        // Don't edit last points as they should be edited by the others first points
                        if (p.localPosition.x < world.chunkSize && p.localPosition.z < world.chunkSize)
                        {
                            if (Mathf.RoundToInt(point2.x) == 0)
                            {
                                Chunk borderChunk = world.GetChunk(offsetedX - 1, offsetedY + 1, offsetedZ);

                                if (borderChunk != null)
                                {
                                    borderChunk.SetDensity(world, newDensity, new Vector3(world.chunkSize, point2.y, point2.z));
                                }
                            }

                            if (Mathf.RoundToInt(point2.z) == 0)
                            {
                                Chunk borderChunk = world.GetChunk(offsetedX, offsetedY + 1, offsetedZ - 1);

                                if (borderChunk != null)
                                {
                                    borderChunk.SetDensity(world, newDensity, new Vector3(point2.x, point2.y, world.chunkSize));
                                }
                            }

                            chunk2.SetDensity(world, newDensity, point2);
                        }
                    }
                }
            }
        }

        // Update chunks
        centerChunk.Generate(world);

        if (topLeftChunk != null)
        {
            topLeftChunk.Generate(world);
        }

        if (topRightChunk != null)
        {
            topRightChunk.Generate(world);
        }

        if (bottomLeftChunk != null)
        {
            bottomLeftChunk.Generate(world);
        }

        if (bottomRightChunk != null)
        {
            bottomRightChunk.Generate(world);
        }
    }

    public static void SetTerrain(World world, Vector3 point)
    {
        int hitX = point.x.Round();
        int hitY = point.y.Round();
        int hitZ = point.z.Round();
        Vector3 hitPos = new Vector3(hitX, hitY, hitZ);

        int intRange = world.range.Ceil();
        List<Chunk> chunksToUpdate = new List<Chunk>();

        // Dertermine what chunks to update
        Chunk chunk = world.GetChunk(hitPos + new Vector3(world.range, 1, world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(world.range, 1, -world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(-world.range, 1, world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(-world.range, 1, -world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        for (int x = -intRange; x <= intRange; x++)
        {
            for (int z = -intRange; z <= intRange; z++)
            {
                int offsetedX = hitX - x;
                int offsetedZ = hitZ - z;

                float distance = Utils.Distance(offsetedX, point.y, offsetedZ, point);
                if (!(distance <= world.range))
                {
                    continue;
                }

                Chunk chunk2 = world.GetChunk(offsetedX, hitY + 1, offsetedZ);

                if (chunk2 != null)
                {
                    for (int i = world.targetHeight + 1; i < world.chunkSize; i++)
                    {
                        Vector3 position = (new Vector3(offsetedX, i, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x;
                        chunk2.SetDensity(world, 1, position);
                    }

                    for (int i = world.targetHeight; i >= 0; i--)
                    {
                        Vector3 position = (new Vector3(offsetedX, i, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x;
                        chunk2.SetDensity(world, 0, position);
                    }
                }
            }
        }

        // Update chunks
        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[i].Generate(world);
        }
    }

    public static void PaintTerrain(World world, Vector3 point)
    {
        int hitX = point.x.Round();
        int hitY = point.y.Round();
        int hitZ = point.z.Round();
        Vector3 hitPos = new Vector3(hitX, hitY, hitZ);
        int intRange = world.range.Ceil();

        // Dertermine what chunks to update
        Chunk centerChunk = world.GetChunk(point + new Vector3(0, 10, 0));
        Chunk topLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 10, world.range + 0.1f));
        Chunk topRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 10, world.range + 0.1f));
        Chunk bottomLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 10, -world.range - 0.1f));
        Chunk bottomRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 10, -world.range - 0.1f));

        for (int x = -intRange; x <= intRange; x++)
        {
            for (int y = -intRange; y <= intRange; y++)
            {
                for (int z = -intRange; z <= intRange; z++)
                {
                    int offsetedX = hitX - x;
                    int offsetedY = hitY - y;
                    int offsetedZ = hitZ - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    Chunk chunk2 = world.GetChunk(offsetedX, offsetedY, offsetedZ);

                    if (chunk2 != null)
                    {
                        chunk2.SetColor(world, world.colour, new Vector3(offsetedX, offsetedY, offsetedZ));
                    }
                }
            }
        }

        // Update chunks
        centerChunk.Generate(world);

        if (topLeftChunk != null)
        {
            topLeftChunk.Generate(world);
        }

        if (topRightChunk != null)
        {
            topRightChunk.Generate(world);
        }

        if (bottomLeftChunk != null)
        {
            bottomLeftChunk.Generate(world);
        }

        if (bottomRightChunk != null)
        {
            bottomRightChunk.Generate(world);
        }
    }

}

