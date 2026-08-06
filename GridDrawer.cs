// GridDrawer.cs
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GridDrawer : MonoBehaviour
{
    public SnakeController snakeController;
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public int pixelSize = 20;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        if (snakeController == null) return;

        Vector2Int gridSize = snakeController.gridSize;

        // 计算边界
        float halfWidth = gridSize.x * pixelSize / 2f;
        float halfHeight = gridSize.y * pixelSize / 2f;

        LineRenderer line = GetComponent<LineRenderer>();
        line.startColor = gridColor;
        line.endColor = gridColor;
        line.startWidth = 1;
        line.endWidth = 1;
        line.positionCount = 0;

        // 画垂直线
        for (int x = 0; x <= gridSize.x; x++)
        {
            float worldX = x * pixelSize - halfWidth;
            line.positionCount += 2;
            line.SetPosition(line.positionCount - 2, new Vector3(worldX, -halfHeight, 0));
            line.SetPosition(line.positionCount - 1, new Vector3(worldX, halfHeight, 0));
        }

        // 画水平线
        for (int y = 0; y <= gridSize.y; y++)
        {
            float worldY = y * pixelSize - halfHeight;
            line.positionCount += 2;
            line.SetPosition(line.positionCount - 2, new Vector3(-halfWidth, worldY, 0));
            line.SetPosition(line.positionCount - 1, new Vector3(halfWidth, worldY, 0));
        }
    }
}