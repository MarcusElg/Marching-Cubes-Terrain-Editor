using System.Collections.Generic;
using UnityEngine;

public static class TerrainEditor
{

    public static void ModifyTerrain(World world, Vector3 point, bool addTerrain)
    {
        int buildModifier = addTerrain ? -1 : 1;

        List<Chunk> chunksToUpdate = new List<Chunk>();
        List<Vector3> alreadyModifiedPoints = new List<Vector3>();

        float start = -world.range;
        if (world.range < 1)
        {
            start = 0;
        }

        for (float x = start; x <= world.range; x++)
        {
            for (float y = start; y <= world.range; y++)
            {
                for (float z = start; z <= world.range; z++)
                {
                    float offsetedX = point.x - x;
                    float offsetedY = point.y - y;
                    float offsetedZ = point.z - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    if (!alreadyModifiedPoints.Contains(new Vector3(offsetedX, offsetedY, offsetedZ)))
                    {
                        float modificationAmount = world.force * buildModifier;
                        List<Chunk> chunks = null;

                        Vector3 position = (new Vector3(offsetedX, offsetedY - 1, offsetedZ) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position;
                        if (addTerrain == true)
                        {
                            position.y = (offsetedY - world.transform.position.y).CeilToNearestX(world.transform.lossyScale.x) + world.transform.position.y;
                        }
                        else
                        {
                            position.y = (offsetedY - world.transform.position.y).FloorToNearestX(world.transform.lossyScale.x) + world.transform.position.y;
                        }

                        chunks = world.GetChunks(position);

                        if (chunks != null && chunks.Count > 0)
                        {
                            Vector3 point2 = (position - chunks[0].transform.position) / world.transform.lossyScale.x;
                            float oldDensity = world.GetPoint(new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z))).density;
                            float newDensity = (oldDensity + modificationAmount).Clamp01();

                            for (int i = 0; i < chunks.Count; i++)
                            {
                                if (!chunksToUpdate.Contains(chunks[i]))
                                {
                                    chunksToUpdate.Add(chunks[i]);
                                }

                                point2 = (position - chunks[i].transform.position) / world.transform.lossyScale.x;

                                chunks[i].SetDensity(world, newDensity, point2);
                            }

                            if (chunks.Count > 1)
                            {
                                alreadyModifiedPoints.Add(new Vector3(offsetedX, offsetedY, offsetedZ));
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

    public static void SetTerrain(World world, Vector3 point)
    {
        List<Chunk> chunksToUpdate = new List<Chunk>();

        float start = -world.range;
        if (world.range < 1)
        {
            start = 0;
        }

        for (float x = start; x <= world.range; x++)
        {
            for (float z = start; z <= world.range; z++)
            {
                float offsetedX = point.x - x;
                float offsetedZ = point.z - z;

                float distance = Utils.Distance(offsetedX, point.y, offsetedZ, point);
                if (!(distance <= world.range))
                {
                    continue;
                }

                for (int y = 0; y <= world.chunkSize * world.maxHeightIndex; y++)
                {
                    List<Chunk> chunks = world.GetChunks((new Vector3(offsetedX, y * world.transform.lossyScale.x + world.transform.position.y, offsetedZ) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position);

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

        float start = -world.range;
        if (world.range < 1)
        {
            start = 0;
        }

        for (float f = 0; f < 1; f += distancePerLerp)
        {
            Vector3 position = Vector3.Lerp(world.startPosition, world.endPosition, f);

            float startY = -world.range;
            if ((world.flatFloor == true && world.addTerrain == false) || world.range < 1)
            {
                startY = 0;
            }

            float endY = world.range;
            if (world.clearAbove == true && world.addTerrain == false)
            {
                endY = 0;
            }

            for (float x = start; x <= world.range; x++)
            {
                for (float y = startY; y <= endY; y++)
                {
                    Vector3 offsetPosition = position + left * x + Vector3.up * y;
                    List<Chunk> chunks = world.GetChunks((new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position);

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
                    List<Chunk> chunks = world.GetChunks((new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position);

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
        float[,,] densities = new float[Mathf.CeilToInt(world.range) * 2 + 2, 3, Mathf.CeilToInt(world.range) * 2 + 2];
        List<Chunk>[,,] chunks = new List<Chunk>[Mathf.CeilToInt(world.range) * 2 + 2, 3, Mathf.CeilToInt(world.range) * 2 + 2];
        Point[,,] points = new Point[Mathf.CeilToInt(world.range) * 2 + 2, 3, Mathf.CeilToInt(world.range) * 2 + 2];

        Vector3 left = new Vector3(-up.z, up.y, up.x).normalized;
        Vector3 forward = new Vector3(left.x, left.y, -left.z).normalized;

        int start = Mathf.FloorToInt(-world.range);
        if (world.range < 1)
        {
            start = 0;
        }

        // Add densities
        for (int x = start; x <= world.range; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = start; z <= world.range; z++)
                {
                    List<Chunk> worldChunks = world.GetChunks(((new Vector3(point.x, point.y, point.z) + x * left + y * up + z * forward) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position);

                    if (worldChunks.Count != 0)
                    {
                        Vector3 localPosition = (new Vector3(point.x, point.y, point.z) + x * left + y * up + z * forward - worldChunks[0].transform.position) / world.transform.lossyScale.x;
                        densities[x + Mathf.CeilToInt(world.range) + 1, y + 1, z + Mathf.CeilToInt(world.range) + 1] = worldChunks[0].GetPoint(world, localPosition).density;
                        chunks[x + Mathf.CeilToInt(world.range) + 1, y + 1, z + Mathf.CeilToInt(world.range) + 1] = new List<Chunk>();
                        points[x + Mathf.CeilToInt(world.range) + 1, y + 1, z + Mathf.CeilToInt(world.range) + 1] = worldChunks[0].GetPoint(world, localPosition);

                        for (int i = 0; i < worldChunks.Count; i++)
                        {
                            if (!chunksToUpdate.Contains(worldChunks[i]))
                            {
                                chunksToUpdate.Add(worldChunks[i]);
                            }

                            chunks[x + Mathf.CeilToInt(world.range) + 1, y + 1, z + Mathf.CeilToInt(world.range) + 1].Add(worldChunks[i]);
                        }
                    }
                    else
                    {
                        densities[x + Mathf.CeilToInt(world.range) + 1, y + 1, z + Mathf.CeilToInt(world.range) + 1] = float.MaxValue;
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
                    for (int i = 0; i < chunks[x, 1, z].Count; i++)
                    {
                        chunks[x, 1, z][i].SetDensity(world, Mathf.Lerp(points[x, 1, z].density, averageDensity, world.force), points[x, 1, z].localPosition);
                    }
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
        List<Chunk> chunksToUpdate = new List<Chunk>();
        float start = -world.range;
        if (world.range < 1)
        {
            start = 0;
        }

        for (float x = start; x <= world.range; x++)
        {
            for (float y = start; y <= world.range; y++)
            {
                for (float z = start; z <= world.range; z++)
                {
                    float offsetedX = point.x - x;
                    float offsetedY = point.y - y;
                    float offsetedZ = point.z - z;

                    float distance = Utils.Distance(offsetedX, offsetedY, offsetedZ, point);
                    if (!(distance <= world.range))
                    {
                        continue;
                    }

                    List<Chunk> chunks = world.GetChunks((new Vector3(offsetedX, offsetedY, offsetedZ) - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position);

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

