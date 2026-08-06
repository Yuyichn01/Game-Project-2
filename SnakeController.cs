// SnakeController.cs (更新版 - 添加终点胜利)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[DefaultExecutionOrder(100)]
public class SnakeController : MonoBehaviour
{
    [Header("游戏设置")]
    public Vector2Int gridSize = new Vector2Int(21, 21);
    public float moveInterval = 0.3f;
    public GameObject segmentPrefab;
    public GameObject foodPrefab;

    [Header("UI")]
    public Text scoreText;
    public Text gameOverText;
    public Button restartButton;

    [Header("像素风格")]
    public int pixelSize = 20;

    [Header("迷宫")]
    public MazeGenerator mazeGenerator;

    private List<Vector2Int> snakePositions = new List<Vector2Int>();
    private List<GameObject> snakeSegments = new List<GameObject>();
    private Vector2Int foodPosition;
    private GameObject foodObject;
    private Vector2Int moveDirection = Vector2Int.right;
    private Vector2Int nextMoveDirection = Vector2Int.right;
    private bool isGameRunning = true;
    private int score = 0;
    private float moveTimer = 0f;
    private bool[,] mazeGrid;
    private Vector2Int exitPosition;

    void Start()
    {
        if (mazeGenerator != null)
        {
            mazeGrid = mazeGenerator.GetMazeGrid();
            gridSize = mazeGenerator.gridSize;
            exitPosition = mazeGenerator.exitPosition;
        }
        else
        {
            mazeGrid = new bool[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
                for (int y = 0; y < gridSize.y; y++)
                    mazeGrid[x, y] = true;
            exitPosition = new Vector2Int(gridSize.x - 2, gridSize.y - 2);
        }

        InitializeGame();
    }

    void Update()
    {
        if (!isGameRunning) return;
        HandleInput();

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            MoveSnake();
        }
    }

    void InitializeGame()
    {
        foreach (var seg in snakeSegments) Destroy(seg);
        snakeSegments.Clear();
        snakePositions.Clear();

        isGameRunning = true;
        score = 0;
        moveDirection = Vector2Int.right;
        nextMoveDirection = Vector2Int.right;
        moveTimer = 0f;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
            gameOverText.text = "";
        }
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        Vector2Int startPos = FindValidStartPosition();
        if (startPos == Vector2Int.zero)
        {
            Debug.LogError("找不到有效的起始位置！");
            return;
        }

        snakePositions.Add(startPos);
        snakePositions.Add(new Vector2Int(startPos.x - 1, startPos.y));
        snakePositions.Add(new Vector2Int(startPos.x - 2, startPos.y));

        foreach (var pos in snakePositions)
        {
            if (!IsWalkable(pos))
            {
                InitializeGame();
                return;
            }
        }

        foreach (var pos in snakePositions)
        {
            CreateSegment(pos);
        }

        SpawnFood();
        UpdateScoreUI();
    }

    Vector2Int FindValidStartPosition()
    {
        for (int x = 2; x < gridSize.x / 2; x++)
        {
            for (int y = gridSize.y - 2; y > gridSize.y / 2; y--)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsWalkable(pos) &&
                    IsWalkable(new Vector2Int(x - 1, y)) &&
                    IsWalkable(new Vector2Int(x - 2, y)))
                {
                    return pos;
                }
            }
        }
        return Vector2Int.zero;
    }

    bool IsWalkable(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
            return false;

        if (mazeGrid != null)
            return mazeGrid[pos.x, pos.y];

        return true;
    }

    void CreateSegment(Vector2Int position)
    {
        if (segmentPrefab == null) return;

        GameObject segment = Instantiate(segmentPrefab, GetWorldPosition(position), Quaternion.identity);
        segment.transform.localScale = new Vector3(pixelSize, pixelSize, 1);
        snakeSegments.Add(segment);
    }

    Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        float x = gridPos.x * pixelSize - (gridSize.x * pixelSize) / 2f + pixelSize / 2f;
        float y = gridPos.y * pixelSize - (gridSize.y * pixelSize) / 2f + pixelSize / 2f;
        return new Vector3(x, y, 0);
    }

    void HandleInput()
    {
        if (InputHelper.GetKeyDown(KeyCode.W) || InputHelper.GetKeyDown(KeyCode.UpArrow))
        {
            if (moveDirection != Vector2Int.down)
                nextMoveDirection = Vector2Int.up;
        }
        else if (InputHelper.GetKeyDown(KeyCode.S) || InputHelper.GetKeyDown(KeyCode.DownArrow))
        {
            if (moveDirection != Vector2Int.up)
                nextMoveDirection = Vector2Int.down;
        }
        else if (InputHelper.GetKeyDown(KeyCode.A) || InputHelper.GetKeyDown(KeyCode.LeftArrow))
        {
            if (moveDirection != Vector2Int.right)
                nextMoveDirection = Vector2Int.left;
        }
        else if (InputHelper.GetKeyDown(KeyCode.D) || InputHelper.GetKeyDown(KeyCode.RightArrow))
        {
            if (moveDirection != Vector2Int.left)
                nextMoveDirection = Vector2Int.right;
        }
    }

    void MoveSnake()
    {
        moveDirection = nextMoveDirection;

        Vector2Int head = snakePositions[0];
        Vector2Int newHead = head + moveDirection;

        // 检查是否吃到食物
        bool ateFood = (newHead == foodPosition);

        // 检查是否到达终点（胜利）
        if (newHead == exitPosition)
        {
            // 蛇头到达终点，胜利！
            snakePositions.Insert(0, newHead);

            // 创建终点头部（可以变金色）
            GameObject newHeadObj = Instantiate(segmentPrefab, GetWorldPosition(newHead), Quaternion.identity);
            newHeadObj.transform.localScale = new Vector3(pixelSize, pixelSize, 1);

            // 可以改变颜色表示胜利
            SpriteRenderer renderer = newHeadObj.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = Color.yellow;

            snakeSegments.Insert(0, newHeadObj);

            GameWin();
            return;
        }

        // 执行普通移动
        snakePositions.Insert(0, newHead);

        if (!ateFood)
        {
            Vector2Int tail = snakePositions[snakePositions.Count - 1];
            snakePositions.RemoveAt(snakePositions.Count - 1);

            GameObject tailObj = snakeSegments[snakeSegments.Count - 1];
            snakeSegments.RemoveAt(snakeSegments.Count - 1);
            Destroy(tailObj);
        }
        else
        {
            score += 10;
            UpdateScoreUI();
            SpawnFood();
        }

        // 创建新头部
        GameObject newHeadObj2 = Instantiate(segmentPrefab, GetWorldPosition(newHead), Quaternion.identity);
        newHeadObj2.transform.localScale = new Vector3(pixelSize, pixelSize, 1);
        snakeSegments.Insert(0, newHeadObj2);

        CheckCollision(newHead);
    }

    void CheckCollision(Vector2Int head)
    {
        if (!IsWalkable(head))
        {
            GameOver(LocalizationManager.Get("wall_hit"));
            return;
        }

        for (int i = 1; i < snakePositions.Count; i++)
        {
            if (snakePositions[i] == head)
            {
                GameOver(LocalizationManager.Get("self_bite"));
                return;
            }
        }
    }

    void SpawnFood()
    {
        if (foodObject != null) Destroy(foodObject);

        List<Vector2Int> availablePositions = new List<Vector2Int>();

        if (mazeGenerator != null)
        {
            List<Vector2Int> allPaths = mazeGenerator.GetPathPositions();
            foreach (var pos in allPaths)
            {
                // 不能生成在蛇身上，也不能生成在终点位置
                if (!snakePositions.Contains(pos) && pos != exitPosition)
                {
                    availablePositions.Add(pos);
                }
            }
        }
        else
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!snakePositions.Contains(pos) && IsWalkable(pos) && pos != exitPosition)
                    {
                        availablePositions.Add(pos);
                    }
                }
            }
        }

        if (availablePositions.Count == 0)
        {
            // 没有位置放食物，检查是否所有通道都被蛇占据了
            int totalPaths = mazeGenerator != null ? mazeGenerator.GetPathPositions().Count : gridSize.x * gridSize.y;
            if (snakePositions.Count >= totalPaths - 1) // -1 因为终点不算
            {
                GameWin(); // 蛇几乎占满所有通道，胜利！
            }
            return;
        }

        foodPosition = availablePositions[Random.Range(0, availablePositions.Count)];

        if (foodPrefab != null)
        {
            foodObject = Instantiate(foodPrefab, GetWorldPosition(foodPosition), Quaternion.identity);
            foodObject.transform.localScale = new Vector3(pixelSize, pixelSize, 1);
        }
    }

    // 胜利处理
    void GameWin()
    {
        isGameRunning = false;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = LocalizationManager.Format("win_message", score);
            gameOverText.color = Color.yellow;
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    void GameOver(string reason)
    {
        isGameRunning = false;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = LocalizationManager.Format("game_over", reason, score);
            gameOverText.color = Color.red;
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            string exitHint = LocalizationManager.Get("exit_hint");
            scoreText.text = LocalizationManager.Format("score_prefix", score) + exitHint;
        }
    }

    public void RestartGame()
    {
        // 重新获取迷宫数据（生成新迷宫）
        if (mazeGenerator != null)
        {
            mazeGenerator.GenerateMaze();
            mazeGenerator.BuildMazeWalls();
            mazeGenerator.PlaceExit();
            mazeGrid = mazeGenerator.GetMazeGrid();
            gridSize = mazeGenerator.gridSize;
            exitPosition = mazeGenerator.exitPosition;
        }

        InitializeGame();
    }
}