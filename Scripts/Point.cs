using UnityEngine;

[System.Serializable]
public struct Point
{
    public Vector3Int localPosition;
    public float density;
    public Color colour;

    public Point(Vector3Int localPosition, float density, Color colour)
    {
        this.localPosition = localPosition;
        this.density = density;
        this.colour = colour;
    }
}