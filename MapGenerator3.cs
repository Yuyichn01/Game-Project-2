using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*this script generate building blocks and items
remember to add "generatePoint" tag to door
*/
public class MapGenerator3 : MonoBehaviour
{
    //variables for player
    [Header("Player section")]
    public Transform playerPosition;

    //spawndistance must greater than spawnnumber
    public int spawnDistance = 300;

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
    public int groundTopLimit = 0;

    public int groundTopMinimum = 0;

    public Transform startGround;

    public List<Transform> Ground1;

    public List<Transform> Ground1Top;

    public List<Transform> Ground1Ceilings;

    public List<Transform> Ground2;

    public List<Transform> Ground2Top;

    public List<Transform> Ground2Ceilings;

    private List<Transform> Ground;

    private List<Transform> GroundTop;

    private List<Transform> GroundCeilings;

    private Vector3 endPosition4;

    private Vector3 addPosition1;

    private Vector3 endPosition6;

    private Vector3 addPosition3;

    //variables for door windows and decorator
    [Header("Ground object section")]
    //the ground index to choose
    public List<Transform> Doors1;

    public List<Transform> Windows1;

    public List<Transform> Doors2;

    public List<Transform> Windows2;

    public List<Transform> FloorDecorator;

    public List<Transform> WallDecorator;

    private List<Transform> Doors;

    private List<Transform> Windows;

    public void Start()
    {
        //check for error
        if (groundTopLimit < 1)
        {
            Debug.Log("error, 'groundTopLimit' must greater than 1");
        }
    }

    public void Update()
    {
        // Assign distance values
        generateBG1();
        generateBG2();
        generateBG3();
        generateG();
    }

    public void Awake()
    {
        // find endpositions for backgrounds
        endPosition1 = startBackground1.Find("EndPosition").position;
        endPosition2 = startBackground2.Find("EndPosition").position;
        endPosition3 = startBackground3.Find("EndPosition").position;

        // find endpositions and addpositions for grounds
        endPosition4 = startGround.Find("EndPosition").position;
        addPosition1 = startGround.Find("AddPosition").position;
    }

    // spawn method for backN
    public void generateBG1()
    {
        if (
            Vector3.Distance(playerPosition.position, endPosition1) <
            spawnDistance
        )
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
        if (
            Vector3.Distance(playerPosition.position, endPosition2) <
            spawnDistance
        )
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
        if (
            Vector3.Distance(playerPosition.position, endPosition3) <
            spawnDistance
        )
        {
            Transform chosenLevelPart =
                background3[Random.Range(0, background3.Count)];
            Transform lastLevelPartTransform =
                Instantiate(chosenLevelPart, endPosition3, Quaternion.identity);
            endPosition3 = lastLevelPartTransform.Find("EndPosition").position;
        }
    }

    public void generateG()
    {
        switch (Random.Range(0, 2))
        {
            case 0:
                Ground = Ground1;
                GroundTop = Ground1Top;
                GroundCeilings = Ground1Ceilings;
                break;
            case 1:
                Ground = Ground2;
                GroundTop = Ground2Top;
                GroundCeilings = Ground2Ceilings;
                break;
        }

        switch (Random.Range(0, 2))
        {
            case 0:
                Doors = Doors1;
                break;
            case 1:
                Doors = Doors2;
                break;
        }

        switch (Random.Range(0, 2))
        {
            case 0:
                Windows = Windows1;
                break;
            case 1:
                Windows = Windows2;
                break;
        }

        if (
            Vector3.Distance(playerPosition.position, endPosition4) <
            spawnDistance
        )
        {
            // generate the base ground
            Transform chosenLevelPart1 = Ground[Random.Range(0, Ground.Count)];
            Transform lastLevelPartTransform1 =
                Instantiate(chosenLevelPart1,
                endPosition4,
                Quaternion.identity);
            endPosition4 = lastLevelPartTransform1.Find("EndPosition").position;
            addPosition1 = lastLevelPartTransform1.Find("AddPosition").position;

            //generate doors for base ground
            Transform chosenObject = Doors[Random.Range(0, Doors.Count)];
            Instantiate(chosenObject,
            lastLevelPartTransform1.Find("DoorPosition").position,
            Quaternion.identity);

            //generate windows for base ground
            string[] windowPositions =
            {
                "WindowPosition1",
                "WindowPosition2",
                "WindowPosition3",
                "WindowPosition4"
            };
            for (int i = 0; i < windowPositions.Length; i++)
            {
                if (
                    HasChildWithName(lastLevelPartTransform1,
                    windowPositions[i])
                )
                {
                    chosenObject = Windows[Random.Range(0, Windows.Count)];
                    Instantiate(chosenObject,
                    lastLevelPartTransform1.Find(windowPositions[i]).position,
                    Quaternion.identity);
                }
            }

            //generate decorators for base ground
            string[] decoratorPositions =
            {
                "DecoratorPosition1",
                "DecoratorPosition2",
                "DecoratorPosition3",
                "DecoratorPosition4"
            };

            for (int i = 0; i < decoratorPositions.Length; i++)
            {
                if (
                    HasChildWithName(lastLevelPartTransform1,
                    decoratorPositions[i])
                )
                {
                    chosenObject =
                        FloorDecorator[Random.Range(0, FloorDecorator.Count)];
                    Instantiate(chosenObject,
                    lastLevelPartTransform1
                        .Find(decoratorPositions[i])
                        .position,
                    Quaternion.identity);
                }
            }

            // generate ground tops
            int TopLimitRandNum =
                Random.Range(groundTopMinimum, groundTopLimit);
            int groundRandNum = Random.Range(0, GroundTop.Count);
            for (int j = 0; j < TopLimitRandNum; j++)
            {
                //if it is not the last floor
                if (j != (TopLimitRandNum - 1))
                {
                    Transform chosenLevelPart2 = GroundTop[groundRandNum];
                    Transform lastLevelPartTransform2 =
                        Instantiate(chosenLevelPart2,
                        addPosition1,
                        Quaternion.identity);
                    addPosition1 =
                        lastLevelPartTransform2.Find("AddPosition").position;

                    //generate ground top doors
                    if (
                        HasChildWithName(lastLevelPartTransform2,
                        "DoorPosition")
                    )
                    {
                        chosenObject = Doors[Random.Range(0, Doors.Count)];
                        Instantiate(chosenObject,
                        lastLevelPartTransform2.Find("DoorPosition").position,
                        Quaternion.identity);
                    }

                    //generate ground top windows
                    for (int i = 0; i < windowPositions.Length; i++)
                    {
                        if (
                            HasChildWithName(lastLevelPartTransform2,
                            windowPositions[i])
                        )
                        {
                            chosenObject =
                                Windows[Random.Range(0, Windows.Count)];
                            Instantiate(chosenObject,
                            lastLevelPartTransform2
                                .Find(windowPositions[i])
                                .position,
                            Quaternion.identity);
                        }
                    }

                    //generate ground top decorators
                    for (int i = 0; i < decoratorPositions.Length; i++)
                    {
                        if (
                            HasChildWithName(lastLevelPartTransform2,
                            decoratorPositions[i])
                        )
                        {
                            chosenObject =
                                WallDecorator[Random
                                    .Range(0, WallDecorator.Count)];
                            Instantiate(chosenObject,
                            lastLevelPartTransform2
                                .Find(decoratorPositions[i])
                                .position,
                            Quaternion.identity);
                        }
                    }
                }
                else
                {
                    Transform chosenLevelPart2 =
                        GroundCeilings[Random.Range(0, GroundCeilings.Count)];
                    Transform lastLevelPartTransform2 =
                        Instantiate(chosenLevelPart2,
                        addPosition1,
                        Quaternion.identity);
                }
            }
        }
    }

    bool HasChildWithName(Transform parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        return child != null;
    }
}
