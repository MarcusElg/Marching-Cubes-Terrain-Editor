using System.Collections.Generic;
using UnityEngine;

public static class TerrainEditor
{

    public static void ModifyTerrain(World world, Vector3 point, bool addTerrain)
    {
        int buildModifier = addTerrain ? -1 : 1;
        int hitX = point.x.Round();
        int hitY = point.y.Round();
        int hitZ = point.z.Round();
        Vector3 hitPos = new Vector3(hitX, hitY, hitZ);

        List<Chunk> chunksToUpdate = new List<Chunk>();

        for (int x = -world.range; x <= world.range; x++)
        {
            for (int y = -world.range; y <= world.range; y++)
            {
                for (int z = -world.range; z <= world.range; z++)
                {
                    int offsetedX = hitX - x;
                    int offsetedY = hitY - y;
                    int offsetedZ = hitZ - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    float modificationAmount = distance * world.forceOverDistance.Evaluate(1 - distance.Map(0, world.force, 0, 1)) * buildModifier;
                    List<Chunk> chunks = world.GetChunks(offsetedX, offsetedY, offsetedZ);

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (!chunksToUpdate.Contains(chunks[i]))
                        {
                            chunksToUpdate.Add(chunks[i]);
                        }

                        Vector3 point2 = (new Vector3(offsetedX, offsetedY, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x;
                        float oldDensity = world.GetPoint(new Vector3Int(Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y), Mathf.RoundToInt(point2.z))).density;
                        float newDensity = oldDensity + modificationAmount;
                        newDensity = newDensity.Clamp01();
                        chunks[i].SetDensity(world, newDensity, point2);
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

    public static void SetTerrain(World world, Vector3 point)
    {
        int hitX = point.x.Round();
        int hitZ = point.z.Round();

        List<Chunk> chunksToUpdate = new List<Chunk>();

        for (int x = -world.range; x <= world.range; x++)
        {
            for (int z = -world.range; z <= world.range; z++)
            {
                int offsetedX = hitX - x;
                int offsetedZ = hitZ - z;

                float distance = Utils.Distance(offsetedX, point.y, offsetedZ, point);
                if (!(distance <= world.range))
                {
                    continue;
                }

                for (int y = 0; y <= world.chunkSize * world.maxHeightIndex; y++)
                {
                    List<Chunk> chunks = world.GetChunks(offsetedX, y * world.transform.lossyScale.x + world.transform.position.y, offsetedZ);

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (!chunksToUpdate.Contains(chunks[i]))
                        {
                            chunksToUpdate.Add(chunks[i]);
                        }

                        if (y >= world.targetHeight)
                        {
                            Vector3 position = (new Vector3(offsetedX, 0, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x;
                            position.y = y - chunks[i].transform.localPosition.y;
                            chunks[i].SetDensity(world, 1, position);
                        }
                        else
                        {
                            Vector3 position = (new Vector3(offsetedX, 0, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x;
                            position.y = y - chunks[i].transform.localPosition.y;
                            chunks[i].SetDensity(world, 0, position);
                        }
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

    public static void LineTerrain(World world)
    {
        float distance = Vector3.Distance(world.startPosition, world.endPosition);
        float distancePerLerp = 1f / distance;

        Vector3 forward = world.startPosition - world.endPosition;
        Vector3 left = new Vector3(-forward.z, 0, forward.x).normalized;

        List<Chunk> chunksToUpdate = new List<Chunk>();

        for (float f = 0; f < 1; f += distancePerLerp)
        {
            Vector3 position = Vector3.Lerp(world.startPosition, world.endPosition, f);

            int startY = -world.range;
            if (world.flatFloor == true && world.addTerrain == false)
            {
                startY = 0;
            }

            int endY = world.range;
            if (world.clearAbove == true && world.addTerrain == false)
            {
                endY = 0;
            }

            for (int x = -world.range; x <= world.range; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Vector3 offsetPosition = position + left * x + Vector3.up * y;
                    List<Chunk> chunks = world.GetChunks(new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z));

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (!chunksToUpdate.Contains(chunks[i]))
                        {
                            chunksToUpdate.Add(chunks[i]);
                        }

                        float distanceToCenter = Vector3.Distance(position + new Vector3(x, y, 0), position);
                        if (!(distanceToCenter <= world.range))
                        {
                            continue;
                        }

                        if (world.addTerrain == true)
                        {
                            chunks[i].SetDensity(world, 0, (offsetPosition - chunks[i].transform.position) / world.transform.lossyScale.x);
                        }
                        else
                        {
                            chunks[i].SetDensity(world, 1, (offsetPosition - chunks[i].transform.position) / world.transform.lossyScale.x);
                        }
                    }
                }

                if (world.clearAbove == true && world.addTerrain == false)
                {
                    Vector3 offsetPosition = position + left * x;
                    List<Chunk> chunks = world.GetChunks(new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z));

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (!chunksToUpdate.Contains(chunks[i]))
                        {
                            chunksToUpdate.Add(chunks[i]);
                        }

                        for (int y = Mathf.RoundToInt((position.y - chunks[i].transform.position.y) / world.transform.lossyScale.x); y <= world.chunkSize; y++)
                        {
                            Vector3 localPosition = (offsetPosition - chunks[i].transform.position) / world.transform.lossyScale.x;
                            localPosition.y = y;
                            chunks[i].SetDensity(world, 1, localPosition);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[i].Generate(world);
        }
    }

    public static void SmoothTerrain(World world, Vector3 point, Vector3 up)
    {
        List<Chunk> chunksToUpdate = new List<Chunk>();
        float[,,] densities = new float[world.range * 2 + 2, 3, world.range * 2 + 2];
        Chunk[,,] chunks = new Chunk[world.range * 2 + 2, 3, world.range * 2 + 2];
        Point[,,] points = new Point[world.range * 2 + 2, 3, world.range * 2 + 2];

        Vector3 left = new Vector3(-up.z, up.y, up.x).normalized;
        Vector3 forward = new Vector3(left.x, left.y, -left.z).normalized;

        // Add densities
        for (int x = -world.range - 1; x <= world.range; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -world.range - 1; z <= world.range; z++)
                {
                    Chunk chunk = world.GetChunk(new Vector3(point.x, point.y, point.z) + x * left + y * up + z * forward);

                    if (chunk != null)
                    {
                        if (!chunksToUpdate.Contains(chunk))
                        {
                            chunksToUpdate.Add(chunk);
                        }

                        Vector3 localPosition = (new Vector3(point.x, point.y, point.z) + x * left + y * up + z * forward - chunk.transform.position) / world.transform.lossyScale.x;
                        densities[x + world.range + 1, y + 1, z + world.range + 1] = chunk.GetPoint(world, localPosition).density;
                        chunks[x + world.range + 1, y + 1, z + world.range + 1] = chunk;
                        points[x + world.range + 1, y + 1, z + world.range + 1] = chunk.GetPoint(world, localPosition);
                    }
                    else
                    {
                        densities[x + world.range + 1, y + 1, z + world.range + 1] = float.MaxValue;
                    }
                }
            }
        }

        // Calculate smoothed value
        for (int x = 1; x < densities.GetLength(0) - 1; x++)
        {
            for (int z = 1; z < densities.GetLength(1) - 1; z++)
            {
                if (chunks[x, 1, z] != null)
                {
                    float totalDensity = 0;
                    int amountOfDensities = 0;
                    AddNeighboringPoints(ref totalDensity, ref amountOfDensities, densities, x, 0, z);
                    AddNeighboringPoints(ref totalDensity, ref amountOfDensities, densities, x, 1, z);
                    AddNeighboringPoints(ref totalDensity, ref amountOfDensities, densities, x, 2, z);

                    float averageDensity = totalDensity / (amountOfDensities / 3);
                    chunks[x, 1, z].SetDensity(world, Mathf.Lerp(points[x, 1, z].density, averageDensity, world.force), points[x, 1, z].localPosition);
                }
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[i].Generate(world);
        }
    }

    private static void AddNeighboringPoints(ref float totalDensity, ref int amountOfDensities, float[,,] densities, int x, int y, int z)
    {
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x + 1, y, z]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x, y, z + 1]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x - 1, y, z]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x, y, z - 1]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x + 1, y, z + 1]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x - 1, y, z - 1]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x - 1, y, z + 1]);
        AddNeighboringPoint(ref totalDensity, ref amountOfDensities, densities[x + 1, y, z - 1]);
    }

    private static void AddNeighboringPoint(ref float totalDensity, ref int amountOfDensities, float density)
    {
        if (density != float.MaxValue)
        {
            totalDensity += density;
            amountOfDensities += 1;
        }
    }

    public static void PaintTerrain(World world, Vector3 point)
    {
        if (world.roundToNearestPoint == true)
        {
            point = (point - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position;
        }

        List<Chunk> chunksToUpdate = new List<Chunk>();

        for (int x = -world.range; x <= world.range; x++)
        {
            for (int y = -world.range; y <= world.range; y++)
            {
                for (int z = -world.range; z <= world.range; z++)
                {
                    float offsetedX = point.x - x;
                    float offsetedY = point.y - y;
                    float offsetedZ = point.z - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    List<Chunk> chunks = world.GetChunks(offsetedX, offsetedY, offsetedZ);

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (!chunksToUpdate.Contains(chunks[i]))
                        {
                            chunksToUpdate.Add(chunks[i]);
                        }

                        if (world.useColourMask == false)
                        {
                            chunks[i].SetColor(world, world.colour, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x);
                        }
                        else
                        {
                            Color colour = chunks[i].GetPoint(world, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x).colour;
                            float h, s, v;
                            float h2, s2, v2;
                            Color.RGBToHSV(colour, out h, out s, out v);
                            Color.RGBToHSV(world.colourMask, out h2, out s2, out v2);

                            if (Mathf.Abs(h - h2) < world.colourMaskTolerance && Mathf.Abs(s - s2) < world.colourMaskTolerance && Mathf.Abs(v - v2) < world.colourMaskTolerance)
                            {
                                chunks[i].SetColor(world, world.colour, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunks[i].transform.position) / world.transform.lossyScale.x);
                            }
                        }
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
}

