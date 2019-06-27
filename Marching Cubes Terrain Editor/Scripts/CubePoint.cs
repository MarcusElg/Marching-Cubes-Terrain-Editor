using UnityEngine;

[System.Serializable]
public struct CubePoint
{
    public Vector3Int localPosition;
    public float density;
    public Color colour;

    public CubePoint(Vector3Int localPosition, float density, Color colour)
    {
        this.localPosition = localPosition;
        this.density = density;
        this.colour = colour;
    }
}