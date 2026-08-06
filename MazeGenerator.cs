// MazeGenerator.cs (新增终点相关)
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class MazeGenerator : MonoBehaviour
{
    public Vector2Int gridSize = new Vector2Int(21, 21);
    public int wallSize = 20;

    public GameObject wallPrefab;
    public GameObject exitPrefab; // 终点预制体
    public Transform wallParent;

    private bool[,] mazeGrid;
    private SnakeController snakeController;

    // 终点位置
    public Vector2Int exitPosition { get; private set; }

    void Start()
    {
        snakeController = GetComponent<SnakeController>();
        if (snakeController != null)
        {
            gridSize = new Vector2Int(
                (gridSize.x % 2 == 0) ? gridSize.x + 1 : gridSize.x,
                (gridSize.y % 2 == 0) ? gridSize.y + 1 : gridSize.y
            );
            snakeController.gridSize = gridSize;
        }

        GenerateMaze();
        BuildMazeWalls();
        PlaceExit(); // 放置终点
    }

    public void GenerateMaze()
    {
        mazeGrid = new bool[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                mazeGrid[x, y] = false;
            }
        }

        // 使用深度优先搜索生成迷宫
        DFSMaze(1, 1);

        // 确保入口是通道
        mazeGrid[1, 0] = true;
    }

    void DFSMaze(int x, int y)
    {
        mazeGrid[x, y] = true;

        List<Vector2Int> directions = new List<Vector2Int>()
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };

        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            if (nx > 0 && nx < gridSize.x - 1 && ny > 0 && ny < gridSize.y - 1 && !mazeGrid[nx, ny])
            {
                mazeGrid[x + dir.x / 2, y + dir.y / 2] = true;
                DFSMaze(nx, ny);
            }
        }
    }

    public void BuildMazeWalls()
    {
        if (wallParent != null)
        {
            foreach (Transform child in wallParent)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            wallParent = transform;
        }

        float halfWidth = gridSize.x * wallSize / 2f;
        float halfHeight = gridSize.y * wallSize / 2f;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (!mazeGrid[x, y])
                {
                    Vector3 worldPos = new Vector3(
                        x * wallSize - halfWidth + wallSize / 2f,
                        y * wallSize - halfHeight + wallSize / 2f,
                        0
                    );

                    GameObject wall = Instantiate(wallPrefab, worldPos, Quaternion.identity);
                    wall.transform.localScale = new Vector3(wallSize, wallSize, 1);
                    wall.transform.parent = wallParent;
                }
            }
        }
    }

    // 放置终点（在迷宫右下角找一个通道）
    public void PlaceExit()
    {
        if (exitPrefab == null) return;

        // 从右下角开始搜索
        for (int x = gridSize.x - 2; x >= gridSize.x / 2; x--)
        {
            for (int y = gridSize.y - 2; y >= gridSize.y / 2; y--)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (mazeGrid[x, y])
                {
                    exitPosition = pos;

                    // 在终点位置生成终点标志
                    float halfWidth = gridSize.x * wallSize / 2f;
                    float halfHeight = gridSize.y * wallSize / 2f;

                    Vector3 worldPos = new Vector3(
                        x * wallSize - halfWidth + wallSize / 2f,
                        y * wallSize - halfHeight + wallSize / 2f,
                        0
                    );

                    GameObject exitObj = Instantiate(exitPrefab, worldPos, Quaternion.identity);
                    exitObj.transform.localScale = new Vector3(wallSize, wallSize, 1);
                    exitObj.transform.parent = wallParent;

                    return;
                }
            }
        }

        // 如果找不到合适的终点，使用最后一个通道
        for (int x = gridSize.x - 1; x >= 0; x--)
        {
            for (int y = gridSize.y - 1; y >= 0; y--)
            {
                if (mazeGrid[x, y])
                {
                    exitPosition = new Vector2Int(x, y);
                    return;
                }
            }
        }
    }

    public bool IsPath(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridSize.x || gridPos.y < 0 || gridPos.y >= gridSize.y)
            return false;
        return mazeGrid[gridPos.x, gridPos.y];
    }

    public List<Vector2Int> GetPathPositions()
    {
        List<Vector2Int> paths = new List<Vector2Int>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (mazeGrid[x, y])
                    paths.Add(new Vector2Int(x, y));
            }
        }
        return paths;
    }

    public bool[,] GetMazeGrid()
    {
        return mazeGrid;
    }
}