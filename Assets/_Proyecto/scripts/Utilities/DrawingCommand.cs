using UnityEngine;

[System.Serializable]
public class DrawingCommand
{
    public float startX;
    public float startY;
    public float endX;
    public float endY;
    public float colorR;
    public float colorG;
    public float colorB;
    public float colorA;
    public float thickness;

    public DrawingCommand(Vector2 start, Vector2 end, Color color, float thickness)
    {
        startX = start.x;
        startY = start.y;
        endX = end.x;
        endY = end.y;
        colorR = color.r;
        colorG = color.g;
        colorB = color.b;
        colorA = color.a;
        this.thickness = thickness;
    }
}
