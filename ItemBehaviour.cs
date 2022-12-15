using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// this script stores the information of Items the player interact with
public class ItemBehaviour : MonoBehaviour
{
    // the type of the item
    public enum ItemType
    {
        Food,
        Tool,
        Door
    }

    public ItemType type;

    // wheather the item is pickable
    public bool pickable;

    // the text description of the item
    public Text itemDescription;

    // the image of item displayed in inventory system
    public Image itemImage;

    // the name of the item;
    public static string ItemName;

    // the transform of current item
    public void reaction()
    {
        if (pickable == true)
        {
            // Add the object to the picked up Items list
            FindObjectOfType<InventorySystem>().addItem(gameObject);
            switch (type)
            {
                case ItemType.Food:
                    Debug.Log("this is food");

                    // delete the object
                    gameObject.SetActive(false);
                    break;
                case ItemType.Tool:
                    Debug.Log("this is tool");

                    // delete the object
                    gameObject.SetActive(false);
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case ItemType.Door:
                    Debug.Log("this is a door");
                    goToNextDoor("goUp");

                    break;
            }
        }
    }

    public void goToNextDoor(string code)
    {
        float distance = Mathf.Infinity;

        // all door position near current door
        GameObject[] allNextDoor;

        // the closest door
        GameObject closestDoor = gameObject;

        // collect all game objects with tag "door"
        allNextDoor = GameObject.FindGameObjectsWithTag("door");

        // filter out the doors in all potential door
        foreach (GameObject door in allNextDoor)
        {
            float diff =
                Mathf.Abs(door.transform.position.y - transform.position.y);
            float currentDistance = diff;
            if (code == "goUp")
            {
                if (currentDistance < distance && currentDistance != 0)
                {
                    closestDoor = door;
                    distance = currentDistance;
                }
            }
            if (code == "goDown")
            {
                if (currentDistance < distance && currentDistance != 0)
                {
                    closestDoor = door;
                    distance = currentDistance;
                }
            }

            GameObject.FindWithTag("Player").transform.position =
                closestDoor.transform.position;
        }
    }
}
