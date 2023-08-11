using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorTest : MonoBehaviour
{
    //general variables
    private Vector3 endPosition1;

    private Vector3 endPosition2;

    private Vector3 endPosition3;

    private Vector3 addPosition1;

    private float distance;

    //variables for player
    [Header("Player section")]
    public Transform player;

    public int spawnDistance;

    public int startSpawnNumber;

    //variables for grounds
    [Header("Ground section")]
    public Transform StartGround;

    public List<Transform> Grounds;

    public int groundTopLimit;

    public List<Transform> GroundTops;

    public void Start()
    {
        // Debug information
        if (startSpawnNumber >= spawnDistance)
        {
            Debug
                .Log("error, 'startSpawnNumber' must smaller than spawnDistance");
        }
        if (groundTopLimit < 1)
        {
            Debug.Log("error, 'groundTopLimit' must greater than 1");
        }
    }

    public void Awake()
    {
        // assign the endPosition and addPosition values
        endPosition3 = StartGround.Find("EndPosition").position;
        addPosition1 = StartGround.Find("AddPosition").position;

        for (int i = 0; i < startSpawnNumber; i++)
        {
            //spawn1();
            //spawn2();
            spawn3();
        }

        DontDestroyOnLoad(transform.gameObject);
    }

    private void Update()
    {
        // Assign distance values
        distance = Vector3.Distance(player.position, endPosition3);

        // generate ground and ground top
        if (distance < spawnDistance)
        {
            spawn3();
        }
    }

    // spawn method for grounds and groundTops
    public void spawn3()
    {
        // generate the base ground
        Transform chosenLevelPart1 = Grounds[Random.Range(0, Grounds.Count)];
        Transform lastLevelPartTransform1 =
            Instantiate(chosenLevelPart1, endPosition3, Quaternion.identity);
        endPosition3 = lastLevelPartTransform1.Find("EndPosition").position;
        addPosition1 = lastLevelPartTransform1.Find("AddPosition").position;

        // generate the ground tops
        for (int j = 0; j < Random.Range(0, groundTopLimit); j++)
        {
            Transform chosenLevelPart2 =
                GroundTops[Random.Range(0, GroundTops.Count)];
            Transform lastLevelPartTransform2 =
                Instantiate(chosenLevelPart2,
                addPosition1,
                Quaternion.identity);
            addPosition1 = lastLevelPartTransform2.Find("AddPosition").position;
        }
    }

    /*
    generate base 
    add top building
    add item
    
    
    
    
    */
}
