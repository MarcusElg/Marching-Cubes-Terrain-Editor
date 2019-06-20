public class DensityGenerator
{
    public FastNoise noise;

    public DensityGenerator(int seed)
    {
        noise = new FastNoise(seed);
    }

    public float CalculateDensity(World world, float worldPosX, float worldPosY, float worldPosZ)
    {
        return TerrainDensity(world, worldPosX, worldPosY, worldPosZ, world.noiseStretch * 0.1f).Clamp01();
    }

    public float SphereDensity(int worldPosX, int worldPosY, int worldPosZ, int radius)
    {
        return worldPosX * worldPosX + worldPosY * worldPosY + worldPosZ * worldPosZ - radius * radius;
    }

    public float TerrainDensity(World world, float worldPosX, float worldPosY, float worldPosZ, float noiseScale)
    {
        if (world.generateNoise == true)
        {
            return worldPosY - noise.GetPerlin(worldPosX / noiseScale, worldPosZ / noiseScale).Map(-1, 1, 0, 1) * world.noiseScale * 10 - world.groundHeight;
        }
        else
        {
            return worldPosY - world.groundHeight;
        }
    }

    public float FlatPlane(int y, float height)
    {
        return y - height + 0.5f;
    }

    public float Union(float d1, float d2)
    {
        return Utils.Min(d1, d2);
    }

    public float Subtract(float d1, float d2)
    {
        return Utils.Max(-d1, d2);
    }

    public float Intersection(float d1, float d2)
    {
        return Utils.Max(d1, d2);
    }
}
