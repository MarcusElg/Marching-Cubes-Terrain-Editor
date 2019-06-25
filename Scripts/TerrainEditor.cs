using System.Collections.Generic;
using UnityEngine;

public static class TerrainEditor
{

    public static void ModifyTerrain(World world, Vector3 point, bool addTerrain)
    {
        int buildModifier = addTerrain ? 1 : -1;

        int hitX = point.x.Round();
        int hitY = point.y.Round();
        int hitZ = point.z.Round();
        Vector3 hitPos = new Vector3(hitX, hitY, hitZ);

        // Dertermine what chunks to update
        Chunk centerChunk = world.GetChunk(point);
        Chunk topLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 0, world.range + 0.1f));
        Chunk topRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 0, world.range + 0.1f));
        Chunk bottomLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 0, -world.range - 0.1f));
        Chunk bottomRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 0, -world.range - 0.1f));

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

                    float modificationAmount = world.force / distance * world.forceOverDistance.Evaluate(1 - distance.Map(0, world.force, 0, 1)) * buildModifier;
                    Chunk chunk2 = world.GetChunk(offsetedX, offsetedY, offsetedZ);

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
                                Chunk borderChunk = world.GetChunk(offsetedX - 1, offsetedY, offsetedZ);

                                if (borderChunk != null)
                                {
                                    borderChunk.SetDensity(world, newDensity, new Vector3(world.chunkSize, point2.y, point2.z));
                                }
                            }

                            if (Mathf.RoundToInt(point2.z) == 0)
                            {
                                Chunk borderChunk = world.GetChunk(offsetedX, offsetedY, offsetedZ - 1);

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

        List<Chunk> chunksToUpdate = new List<Chunk>();

        // Dertermine what chunks to update
        Chunk chunk = world.GetChunk(hitPos + new Vector3(world.range, 0, world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(world.range, 0, -world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(-world.range, 0, world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

        chunk = world.GetChunk(hitPos + new Vector3(-world.range, 0, -world.range));
        if (chunk != null && !chunksToUpdate.Contains(chunk))
        {
            chunksToUpdate.Add(chunk);
        }

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

                Chunk chunk2 = world.GetChunk(offsetedX, hitY, offsetedZ);

                if (chunk2 != null)
                {
                    for (int y = 0; y <= world.chunkSize * world.maxHeightIndex; y++)
                    {
                        if (y > world.chunkSize)
                        {
                            chunk2 = world.GetChunk(offsetedX, y * world.transform.lossyScale.x, offsetedZ);

                            if (!chunksToUpdate.Contains(chunk2))
                            {
                                chunksToUpdate.Add(chunk2);
                            }
                        }

                        if (y >= world.targetHeight)
                        {
                            Vector3 position = (new Vector3(offsetedX, 0, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x;
                            position.y = y - chunk2.transform.localPosition.y;
                            chunk2.SetDensity(world, 1, position);
                        }
                        else
                        {
                            Vector3 position = (new Vector3(offsetedX, 0, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x;
                            position.y = y - chunk2.transform.localPosition.y;
                            chunk2.SetDensity(world, 0, position);
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
            if (world.flatFloor == true)
            {
                startY = 0;
            }

            int endY = world.range;
            if (world.clearAbove == true)
            {
                endY = 0;
            }

            for (int x = -world.range; x <= world.range; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Vector3 offsetPosition = position + left * x + Vector3.up * y;
                    Chunk chunk = world.GetChunk(new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z));

                    if (chunk != null)
                    {
                        if (!chunksToUpdate.Contains(chunk))
                        {
                            chunksToUpdate.Add(chunk);
                        }

                        float distanceToCenter = Vector3.Distance(position + new Vector3(x, y, 0), position);
                        if (!(distanceToCenter <= world.range))
                        {
                            continue;
                        }

                        chunk.SetDensity(world, 1, (offsetPosition - chunk.transform.position) / world.transform.lossyScale.x);
                    }
                }

                if (world.clearAbove == true)
                {
                    Vector3 offsetPosition = position + left * x;
                    Chunk chunk = world.GetChunk(new Vector3(offsetPosition.x, offsetPosition.y, offsetPosition.z));

                    if (chunk != null)
                    {
                        for (int y = Mathf.RoundToInt((position.y - chunk.transform.position.y) / world.transform.lossyScale.x); y <= world.chunkSize; y++)
                        {
                            Vector3 localPosition = (offsetPosition - chunk.transform.position) / world.transform.lossyScale.x;
                            localPosition.y = y;
                            chunk.SetDensity(world, 1, localPosition);
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

    public static void PaintTerrain(World world, Vector3 point)
    {
        if (world.roundToNearestPoint == true)
        {
            point = (point - world.transform.position).RoundToNearestX(world.transform.lossyScale.x) + world.transform.position;
        }

        // Dertermine what chunks to update
        Chunk centerChunk = world.GetChunk(point);
        Chunk topLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 0, world.range + 0.1f));
        Chunk topRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 0, world.range + 0.1f));
        Chunk bottomLeftChunk = world.GetChunk(point + new Vector3(-world.range - 0.1f, 0, -world.range - 0.1f));
        Chunk bottomRightChunk = world.GetChunk(point + new Vector3(world.range + 0.1f, 0, -world.range - 0.1f));

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

                    Chunk chunk2 = world.GetChunk(offsetedX, offsetedY, offsetedZ);

                    if (chunk2 != null)
                    {
                        if (world.useColourMask == false)
                        {
                            chunk2.SetColor(world, world.colour, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x);
                        }
                        else
                        {
                            Color colour = chunk2.GetPoint(world, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x).colour;
                            float h, s, v;
                            float h2, s2, v2;
                            Color.RGBToHSV(colour, out h, out s, out v);
                            Color.RGBToHSV(world.colourMask, out h2, out s2, out v2);

                            if (Mathf.Abs(h - h2) < world.colourMaskTolerance && Mathf.Abs(s - s2) < world.colourMaskTolerance && Mathf.Abs(v - v2) < world.colourMaskTolerance)
                            {
                                chunk2.SetColor(world, world.colour, (new Vector3(offsetedX, offsetedY, offsetedZ) - chunk2.transform.position) / world.transform.lossyScale.x);
                            }
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
}

