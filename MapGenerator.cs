using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*this script generate building blocks and items
remember to add "generatePoint" tag to door
*/
public class MapGenerator : MonoBehaviour
{
    //variables for player
    [Header("Player section")]
    public Transform player;

    //spawndistance must greater than spawnnumber
    private int spawnDistance = 300;

    //spawnnumber must smaller than spawndistance
    private int startSpawnNumber = 10;

    private float distance;

    //variables for backgrounds
    [Header("Background section")]
    public Transform startBackground1;

    public Transform startBackground2;

    public Transform startBackground3;

    public List<Transform> background1;

    public List<Transform> background2;

    public List<Transform> background3;

    private Vector3 endPosition1;

    private Vector3 endPosition2;

    private Vector3 endPosition3;

    //variables for grounds
    [Header("Ground section")]
    public Transform startGround1;

    public List<Transform> Ground1;

    public int groundTopLimit;

    public List<Transform> Ground1Top;

    private Vector3 endPosition4;

    private Vector3 addPosition1;

    //variables for undergrounds
    [Header("Underground section")]
    public Transform startUnderground1;

    public List<Transform> underground1;

    public int undergroundTopLimit;

    public List<Transform> underground1Top;

    private Vector3 endPosition5;

    private Vector3 addPosition2;

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
        if (undergroundTopLimit < 1)
        {
            Debug.Log("error, 'undergroundTopLimit' must greater than 1");
        }
    }

    public void Update()
    {
        // Assign distance values
        distance = Vector3.Distance(player.position, endPosition4);
        generateBG1();
        generateBG2();
        generateBG3();
        generateG1();
        generateUG1();
    }

    public void Awake()
    {
        // find endpositions for backgrounds
        endPosition1 = startBackground1.Find("EndPosition").position;
        endPosition2 = startBackground2.Find("EndPosition").position;
        endPosition3 = startBackground3.Find("EndPosition").position;

        // find endpositions and addpositions for grounds
        endPosition4 = startGround1.Find("EndPosition").position;
        addPosition1 = startGround1.Find("AddPosition").position;

        // find endpositions and addpositions for undergrounds
        endPosition5 = startUnderground1.Find("EndPosition").position;
        addPosition2 = startUnderground1.Find("AddPosition").position;

        for (int i = 0; i < startSpawnNumber; i++)
        {
            generateBG1();
            generateBG2();
            generateBG3();
            generateG1();
            generateUG1();
        }
        DontDestroyOnLoad(transform.gameObject);
    }

    // spawn method for background1
    public void generateBG1()
    {
        if (Vector3.Distance(player.position, endPosition1) < spawnDistance)
        {
            Transform chosenLevelPart =
                background1[Random.Range(0, background1.Count)];
            Transform lastLevelPartTransform =
                Instantiate(chosenLevelPart, endPosition1, Quaternion.identity);
            endPosition1 = lastLevelPartTransform.Find("EndPosition").position;
        }
    }

    // spawn method for background2
    public void generateBG2()
    {
        if (Vector3.Distance(player.position, endPosition2) < spawnDistance)
        {
            Transform chosenLevelPart =
                background2[Random.Range(0, background2.Count)];
            Transform lastLevelPartTransform =
                Instantiate(chosenLevelPart, endPosition2, Quaternion.identity);
            endPosition2 = lastLevelPartTransform.Find("EndPosition").position;
        }
    }

    // spawn method for background3
    public void generateBG3()
    {
        if (Vector3.Distance(player.position, endPosition3) < spawnDistance)
        {
            Transform chosenLevelPart =
                background3[Random.Range(0, background3.Count)];
            Transform lastLevelPartTransform =
                Instantiate(chosenLevelPart, endPosition3, Quaternion.identity);
            endPosition3 = lastLevelPartTransform.Find("EndPosition").position;
        }
    }

    // spawn method for ground1 and ground1Top
    public void generateG1()
    {
        if (Vector3.Distance(player.position, endPosition4) < spawnDistance)
        {
            // generate the base ground
            Transform chosenLevelPart1 =
                Ground1[Random.Range(0, Ground1.Count)];
            Transform lastLevelPartTransform1 =
                Instantiate(chosenLevelPart1,
                endPosition4,
                Quaternion.identity);
            endPosition4 = lastLevelPartTransform1.Find("EndPosition").position;
            addPosition1 = lastLevelPartTransform1.Find("AddPosition").position;

            // generate the ground tops
            for (int j = 0; j < Random.Range(0, groundTopLimit); j++)
            {
                Transform chosenLevelPart2 =
                    Ground1Top[Random.Range(0, Ground1Top.Count)];
                Transform lastLevelPartTransform2 =
                    Instantiate(chosenLevelPart2,
                    addPosition1,
                    Quaternion.identity);
                addPosition1 =
                    lastLevelPartTransform2.Find("AddPosition").position;
            }
        }
    }

    // spawn method for underground1
    public void generateUG1()
    {
        if (Vector3.Distance(player.position, endPosition5) < spawnDistance)
        {
            Transform chosenLevelPart =
                underground1[Random.Range(0, underground1.Count)];
            Transform lastLevelPartTransform =
                Instantiate(chosenLevelPart, endPosition5, Quaternion.identity);
            endPosition5 = lastLevelPartTransform.Find("EndPosition").position;
            addPosition2 = lastLevelPartTransform.Find("AddPosition").position;

            // generate the underground tops
            for (int j = 0; j < Random.Range(0, undergroundTopLimit); j++)
            {
                Transform chosenLevelPart2 =
                    underground1Top[Random.Range(0, underground1Top.Count)];
                Transform lastLevelPartTransform2 =
                    Instantiate(chosenLevelPart2,
                    addPosition2,
                    Quaternion.identity);
                addPosition2 =
                    lastLevelPartTransform2.Find("AddPosition").position;
            }
        }
    }
}
