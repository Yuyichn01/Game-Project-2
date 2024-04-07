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
        Storage,
        Cooker,
        Door,
        Information,
        Bed
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
    public int StorageSpace = 0;

    public List<Item> Items = new List<Item>();

    public bool isFull = false;

    [Header("Cooker section")]
    public List<Item> dish = new List<Item>();

    public List<Item> food = new List<Item>();

    private int NumberOfInstantNoodle = 0;

    private int NumberOfRice = 0;

    private int NumberOfBread = 0;

    private int NumberOfLunchMeat = 0;

    private int NumberOfSausage = 0;

    private int NumberOfWater = 0;

    private int NumberOfDishes = 0;

    private int NumebrOfFruitJam = 0;

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
        //remove the item in the storage
        Items.Remove (item);
    }

    public void AddToFoodList(Item item)
    {
        Item tmpItem;
        tmpItem = item;
        food.Add (tmpItem);
    }

    public void PrepareToCook(Item item)
    {
        //add the food item in the cooker
        Item tmpItem;
        tmpItem = item;
        food.Add (tmpItem);
    }

    public void CancelCook(Item item)
    {
        //remove the food item in the cooker
        food.Remove (item);
    }

    public void StartToCook()
    {
        //reset the number of dish and food to 0
        NumberOfDishes = 0;

        NumberOfInstantNoodle = 0;

        NumberOfRice = 0;

        NumberOfBread = 0;

        NumberOfLunchMeat = 0;

        NumberOfSausage = 0;

        NumberOfWater = 0;

        NumberOfDishes = 0;

        NumebrOfFruitJam = 0;

        //calculate number of food element in the food list
        for (int i = 0; i < food.Count; i++)
        {
            if (food[i].ItemName == "InstantNoodle")
            {
                NumberOfInstantNoodle += 1;
            }
            else if (food[i].ItemName == "Rice")
            {
                NumberOfRice += 1;
            }
            else if (food[i].ItemName == "Bread")
            {
                NumberOfBread += 1;
            }
            else if (food[i].ItemName == "LunchMeat")
            {
                NumberOfLunchMeat += 1;
            }
            else if (food[i].ItemName == "Sausage")
            {
                NumberOfSausage += 1;
            }
            else if (food[i].ItemName == "Water")
            {
                NumberOfWater += 1;
            }
            else if (food[i].ItemName == "FruitJam")
            {
                NumebrOfFruitJam += 1;
            }
            else
            {
                Debug.Log("The Item Name of food is not assigned");
            }
        }

        //Instant noodle receipe
        if (
            NumberOfInstantNoodle != 0 &&
            NumberOfRice == 0 &&
            NumberOfBread == 0 &&
            NumberOfWater != 0
        )
        {
            if (NumberOfInstantNoodle == 1 && NumberOfWater == 1)
            {
                //remove all food items in the cooker
                food.Clear();

                //add fried instant noodle UI in cooker
                AddToFoodList(dish[0]);

                //spawn Fried instant noodle
                Debug.Log("The fried noodle has been cooked");
            }
            else if (NumberOfInstantNoodle == 1 && NumberOfWater == 2)
            {
                //spawn instant noodle with soup
                Debug.Log("The instant noodle with soup has been cooked");
            }
            else
            {
                //cannot cook the item
                Debug.Log("The food cannot be cooked");
            }
        } //Rice receipe
        else if (
            NumberOfRice != 0 &&
            NumberOfInstantNoodle == 0 &&
            NumberOfBread == 0 &&
            NumberOfWater != 0
        )
        {
            NumberOfDishes = NumberOfRice;
            switch (NumberOfRice)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
        } //Bread receipe
        else if (
            NumberOfBread != 0 &&
            NumberOfRice == 0 &&
            NumberOfInstantNoodle == 0
        )
        {
            NumberOfDishes = NumberOfBread;
            switch (NumberOfBread)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
        }
        else
        {
            //cannot cook the item
            Debug.Log("The food cannot be cooked");
        }
    }

    // the transform of current item
    public void itemBehaviour()
    {
        //update the Current Character
        CurrentCharacter = UIManager.GetComponent<UIManager>().CurrentCharacter;

        // if the item is pickable, do these action
        if (pickable == true)
        {
            //Play interact animation
            CurrentCharacter.GetComponent<Animator>().SetTrigger("Interact");

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
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    Debug.Log("this is an entry");
                    SceneManager.LoadScene (sceneIndex);
                    break;
                case ItemType.Portal:
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

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
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    //get,list and display items in this Object
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .StorageItems = Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListStorageItems();

                    //display storage UI pannel
                    UIManager.GetComponent<UIManager>().displayStoragePannel();
                    Debug.Log("this is Storage");
                    break;
                case ItemType.Cooker:
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    //get,list and display food items in this Object
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .FoodItems = food;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListFoodItems();

                    //display storage UI pannel
                    UIManager.GetComponent<UIManager>().displayCookerPannel();
                    Debug.Log("this is a cooker");

                    break;
                case ItemType.Information:
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    //display dialog window
                    UIManager.GetComponent<UIManager>().ResetDialogButtons();
                    DialogManager
                        .GetComponent<DialogManager>()
                        .StartDialog(ItemData.dialog);

                    Debug.Log("this is a piece of information");
                    break;
                case ItemType.Bed:
                    // play transition background
                    StartCoroutine(UIManager
                        .GetComponent<UIManager>()
                        .PlaySleepAnimation());

                    //add the date number in UI manager
                    UIManager.GetComponent<UIManager>().addOneDay();
                    Debug.Log("this is a bed");
                    break;
            }
        }
    }
}
