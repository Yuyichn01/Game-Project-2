using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Script description:
This script defines object's behaviour when player interacts with it. Attach this script to the object that the player interacts with. 

Remeber to set all item tag and layer:
*add "door" tag to door
*add "ladder" tag to ladder
*tick isTrigger for all item

Make sure to set values:
*change gravity scale in rigidbody to 10

set UI manager tag
*/
public class ItemBehaviour : MonoBehaviour
{
    // the type of the item
    public enum ItemType
    {
        Food,
        Tool,
        Heart,
        Entry,
        Portal,
        Storage
    }

    [Header("Item section")]
    // wheather the item is pickable
    public bool pickable;

    public ItemType type;

    public Item ItemData;

    [Header("Character section")]
    private GameObject Player1;

    private GameObject Player2;

    private GameObject Player3;

    private GameObject CurrentCharacter;

    [Header("Manager section")]
    private GameObject UIManager;

    private GameObject DialogManager;

    private GameObject InventoryManager;

    [Header("Entry section")]
    public int sceneIndex = 0;

    [Header("Portal section")]
    public Transform NextPortalPosition;

    [Header("Storage section")]
    public int StorageSpace = 1;

    public List<Item> Items = new List<Item>();

    public bool isFull = false;

    public void Start()
    {
        Player1 = GameObject.FindWithTag("Player1");
        Player2 = GameObject.FindWithTag("Player2");
        Player3 = GameObject.FindWithTag("Player3");
        UIManager = GameObject.FindWithTag("UIManager");
        DialogManager = GameObject.FindWithTag("DialogManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");
    }

    public void Add(Item item)
    {
        Item tmpItem;
        tmpItem = item;
        Items.Add (tmpItem);
    }

    public void Remove(Item item)
    {
        Items.Remove (item);
    }

    // the transform of current item
    public void itemBehaviour()
    {
        //update the Current Character
        CurrentCharacter = UIManager.GetComponent<UIManager>().CurrentCharacter;

        // if the item is pickable do these action
        if (pickable == true)
        {
            // Add the object to the picked up Items list
            switch (type)
            {
                case ItemType.Food:
                    CurrentCharacter.GetComponent<PlayerValue>().Add(ItemData);
                    InventoryManager.GetComponent<InventoryManager>().Items =
                        CurrentCharacter.GetComponent<PlayerValue>().Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListItems();
                    Debug.Log("This is food");
                    break;
                case ItemType.Tool:
                    Debug.Log("this is tool");
                    break;
                case ItemType.Heart:
                    Debug.Log("this is a Heart");
                    Player1.GetComponent<PlayerValue>().health += 1;
                    break;
            }

            Destroy(this.gameObject);
        }
        else
        // if the item is not pickable do these action
        {
            switch (type)
            {
                case ItemType.Entry:
                    Debug.Log("this is an entry");
                    SceneManager.LoadScene (sceneIndex);
                    break;
                case ItemType.Portal:
                    // play transition background
                    StartCoroutine(UIManager
                        .GetComponent<UIManager>()
                        .PlayPortalAnimation());

                    //if characters are together, teleport them all
                    if (UIManager.GetComponent<UIManager>().isSingle == true)
                    {
                        CurrentCharacter.transform.position =
                            NextPortalPosition.position;
                    }
                    else
                    //else teleport single
                    {
                        Player1.transform.position =
                            NextPortalPosition.position;
                        Player2.transform.position =
                            NextPortalPosition.position;
                        Player3.transform.position =
                            NextPortalPosition.position;
                    }
                    Debug.Log("this is a portal");
                    break;
                case ItemType.Storage:
                    //get,list and display items in this Object
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .StorageItems = Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListStorageItems();
                    UIManager.GetComponent<UIManager>().displayStoragePannel();
                    Debug.Log("this is Storage");
                    break;
            }
        }
    }
}
