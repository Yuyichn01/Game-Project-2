using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this script generates items randomly
public class ItemGenerator : MonoBehaviour
{
    public List<GameObject> items;

    public List<Transform> generatePoints;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < generatePoints.Count; i++)
        {
            int randomNum1 = Random.Range(0, items.Count);
            int randomNum2 = Random.Range(0, generatePoints.Count);
            GameObject chosenItem = items[randomNum1];
            Transform itemPosition = generatePoints[randomNum2];
            Instantiate(chosenItem, itemPosition.position, Quaternion.identity);
            generatePoints.RemoveAt (randomNum2);
        }
    }
}
