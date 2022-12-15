using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Player section")]
    public Transform player;

    public int PlayerSpawnDistance;

    [Header("Background section")]
    public int StartingSpawns;

    public Transform StartPrefabBackground;

    public List<Transform> levelPartList;

    private Vector3 lastEndPosition;

    [Header("Ground section")]
    public Transform StartPrefabGround;

    public void Awake()
    {
        // generate background
        lastEndPosition = StartPrefabBackground.Find("EndPosition").position;

        for (int i = 0; i < StartingSpawns; i++)
        {
            Spawn();
        }
        DontDestroyOnLoad(transform.gameObject);
    }

    private void Update()
    {
        // generate background
        if (
            Vector3.Distance(player.position, lastEndPosition) <
            PlayerSpawnDistance
        )
        {
            Spawn();
        }
    }

    // Start is called before the first frame update
    public void Spawn()
    {
        Transform chosenLevelPart =
            levelPartList[Random.Range(0, levelPartList.Count)];
        Transform lastLevelPartTransform =
            Spawn(chosenLevelPart, lastEndPosition);
        lastEndPosition = lastLevelPartTransform.Find("EndPosition").position;
    }

    private Transform Spawn(Transform levelPart, Vector3 spawnPosition)
    {
        Transform levelPartTransform =
            Instantiate(levelPart, spawnPosition, Quaternion.identity);
        return levelPartTransform;
    }
}
