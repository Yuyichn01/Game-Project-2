using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*Attach to Canvas
Assign Health bar to slider
*/
public class UIManager : MonoBehaviour, IDataPersistence
{
    [Header("Buttons section")]
    public Button SaveButton;

    public Button LoadButton;

    public Button NewGameButton;

    public Button InventoryButton;

    public Button SwitchButton;

    public Button PressToStartButton;

    public Button ControlModeButton;

    public Button UtilizeButton;

    public Button StoreButton;

    public Button DiscardButton;

    public Button PlaceButton;

    public Button ReadyToCookButton;

    public Button StartToCookButton;

    public CameraController Camera;

    private int switchCount = 1;

    [Header("Slider section")]
    public Slider HealthBar;

    public Slider StarvationBar;

    [Header("State section")]
    public bool isSingle = false;

    public GameObject CurrentCharacter;

    public GameObject DayRecord;

    public int DayIndex = 1;

    private bool InventoryOpened = false;

    [Header("Character section")]
    public GameObject[] Characters;

    [Header("Camera section")]
    public Camera MainCamera;

    [Header("Background section")]
    public float transitionInTime = 1.0f;

    public float transitionOutTime = 1.0f;

    public float sleepTransitionTime = 1.0f;

    public GameObject PortalBackground;

    private TextMeshProUGUI text;

    [Header("Sprite section")]
    public Sprite[] ControlModeButtonSprites;

    [Header("Manager section")]
    private GameObject DialogManager;

    private GameObject InventoryManager;

    [Header("Inventory section")]
    public Animator InventoryAnimator;

    [Header("Storage section")]
    public Animator StorageAnimator;

    private bool StoragePannelOpened = false;

    [Header("Cooker section")]
    public Animator CookerAnimator;

    private bool CookerPannelOpened = false;

    // Start is called before the first frame update
    public void Start()
    {
        //initialize managers
        DialogManager = GameObject.FindWithTag("DialogManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");

        //Button listeners
        SaveButton.onClick.AddListener (TaskOnClickSaveButton);
        LoadButton.onClick.AddListener (TaskOnClickLoadButton);
        NewGameButton.onClick.AddListener (TaskOnClickNewGameButton);
        InventoryButton.onClick.AddListener (TaskOnInventoryButton);
        SwitchButton.onClick.AddListener (TaskOnSwitchButton);
        PressToStartButton.onClick.AddListener (TaskOnPressToStartButton);
        ControlModeButton.onClick.AddListener (TaskOnControlModeButton);
        StoreButton.onClick.AddListener (TaskOnStoreButton);
        DiscardButton.onClick.AddListener (TaskOnDiscardButton);
        PlaceButton.onClick.AddListener (TaskOnPlaceButton);
        ReadyToCookButton.onClick.AddListener (TaskOnReadyToCookButton);
        StartToCookButton.onClick.AddListener (TaskOnStartToCookButton);

        //Initialize the day record information
        text = DayRecord.GetComponent<TextMeshProUGUI>();
        text.SetText("DAY" + DayIndex);

        //Initialize to the first character
        CurrentCharacter = Characters[0];
        UIupdate();
    }

    //Buttons section: where all the button control methods are written
    public void TaskOnClickSaveButton()
    {
        Debug.Log("Saving game data");
        DataPersistenceManager.instance.SaveGame();
    }

    public void TaskOnClickLoadButton()
    {
        Debug.Log("Loading game data");
        DataPersistenceManager.instance.LoadGame();
    }

    public void TaskOnClickNewGameButton()
    {
        Debug.Log("Initializing new game");
        DataPersistenceManager.instance.NewGame();
    }

    public void TaskOnInventoryButton()
    {
        //Update Inventory items in real times
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();
        if (InventoryOpened == true)
        {
            //close the inventory
            InventoryAnimator.SetBool("IsOpen", false);
            InventoryOpened = false;
            Debug.Log("The Inventory is closed");
        }
        else
        {
            //open the inventory
            InventoryAnimator.SetBool("IsOpen", true);
            InventoryOpened = true;

            Debug.Log("The Inventory is opened");
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void TaskOnSwitchButton()
    {
        //enable all character animator and position constraints
        for (int i = 0; i < Characters.Length; i++)
        {
            Characters[i].GetComponent<PlayerController>().UnfreezeCharacter();
        }

        //switch to another character
        switchCount += 1;
        if (switchCount % 3 == 1)
        {
            CurrentCharacter = Characters[0];
            UIupdate();
        }
        else if (switchCount % 3 == 2)
        {
            CurrentCharacter = Characters[1];
            UIupdate();
        }
        else if (switchCount % 3 == 0)
        {
            CurrentCharacter = Characters[2];
            UIupdate();
        }
        SwitchButton.GetComponent<Image>().sprite =
            CurrentCharacter
                .GetComponent<PlayerController>()
                .CharacterMiniSprite;

        //Update Inventory items
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        //close the cooker pannel UI
        if (CookerPannelOpened == true)
        {
            //close the inventory
            CookerAnimator.SetBool("IsOpen", false);
            CookerPannelOpened = false;
            Debug.Log("The Storage is closed");
        }

        //close the storage pannel UI
        if (StoragePannelOpened == true)
        {
            //close the inventory
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
            Debug.Log("The Storage is closed");
        }

        //close the dialog pannel UI
        DialogManager.GetComponent<DialogManager>().EndDialog();

        //check player freeze state
        PlayerFreezeState();
    }

    public void TaskOnPressToStartButton()
    {
        PressToStartButton.gameObject.SetActive(false);
    }

    public void TaskOnControlModeButton()
    {
        if (isSingle == false)
        {
            isSingle = true;
            ControlModeButton.GetComponent<Image>().sprite =
                ControlModeButtonSprites[0];
            Debug.Log("Switched to single control mode");
        }
        else
        {
            isSingle = false;
            ControlModeButton.GetComponent<Image>().sprite =
                ControlModeButtonSprites[1];
            Debug.Log("Switched to Co-op control mode");
        }
    }

    public void TaskOnStoreButton()
    {
        //add the item to the character's inventory
        CurrentCharacter
            .GetComponent<PlayerController>()
            .Add(InventoryManager.GetComponent<InventoryManager>().CurrentItem);

        //update item in Inventory manager
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        //remove the item from orginal item behaviour
        GameObject m_gameobject =
            CurrentCharacter.GetComponent<PlayerController>().item;

        //if the current item is of type storage
        if (
            m_gameobject.GetComponent<ItemBehaviour>().type ==
            ItemBehaviour.ItemType.Storage
        )
        //delete the item in this item's storage
        {
            for (
                int i = 0;
                i < m_gameobject.GetComponent<ItemBehaviour>().Items.Count;
                i++
            )
            {
                if (
                    m_gameobject.GetComponent<ItemBehaviour>().Items[i] ==
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .CurrentItem
                )
                {
                    m_gameobject
                        .GetComponent<ItemBehaviour>()
                        .Remove(m_gameobject
                            .GetComponent<ItemBehaviour>()
                            .Items[i]);
                    break;
                }
            }
        } // else if the current object is of type cooker
        else if (
            m_gameobject.GetComponent<ItemBehaviour>().type ==
            ItemBehaviour.ItemType.Cooker
        )
        //delete the food item in this item's food storage
        {
            for (
                int i = 0;
                i < m_gameobject.GetComponent<ItemBehaviour>().food.Count;
                i++
            )
            {
                if (
                    m_gameobject.GetComponent<ItemBehaviour>().food[i] ==
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .CurrentItem
                )
                {
                    m_gameobject
                        .GetComponent<ItemBehaviour>()
                        .CancelCook(m_gameobject
                            .GetComponent<ItemBehaviour>()
                            .food[i]);
                    break;
                }
            }
        }

        //Update the food items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            m_gameobject.GetComponent<ItemBehaviour>().food;
        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();

        //Update storage items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().StorageItems =
            m_gameobject.GetComponent<ItemBehaviour>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListStorageItems();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();
        Debug.Log("Store button is pressed");
    }

    public void TaskOnDiscardButton()
    {
        //if player is colliding with other objects
        if (
            CurrentCharacter.GetComponent<PlayerController>().itemCollider !=
            null
        )
        {
            Debug
                .Log("can't discard item because player is interacting with other objects");
        }
        else
        {
            //remove the item from the character's inventory
            Item item =
                InventoryManager.GetComponent<InventoryManager>().CurrentItem;
            for (
                int i = 0;
                i <
                CurrentCharacter.GetComponent<PlayerController>().Items.Count;
                i++
            )
            {
                if (
                    CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Items[i] ==
                    item
                )
                {
                    CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Remove(CurrentCharacter
                            .GetComponent<PlayerController>()
                            .Items[i]);
                    break;
                }
            }

            //update inventory items in Inventory UI
            InventoryManager.GetComponent<InventoryManager>().Items =
                CurrentCharacter.GetComponent<PlayerController>().Items;
            InventoryManager.GetComponent<InventoryManager>().ListItems();

            //respawn the item instance
            GameObject objectToDiscard = Instantiate(item.objectInstance);
            objectToDiscard.transform.position =
                CurrentCharacter.transform.position;

            //close dialog window
            DialogManager.GetComponent<DialogManager>().EndDialog();
            Debug.Log("Discard  button is pressed");
        }
    }

    public void TaskOnPlaceButton()
    {
        //add the current item into collided item's Storage inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem
            .GetComponent<ItemBehaviour>()
            .Add(InventoryManager.GetComponent<InventoryManager>().CurrentItem);
        Item item =
            InventoryManager.GetComponent<InventoryManager>().CurrentItem;

        //remove the item in the current character's inventory
        for (
            int i = 0;
            i < CurrentCharacter.GetComponent<PlayerController>().Items.Count;
            i++
        )
        {
            if (
                CurrentCharacter.GetComponent<PlayerController>().Items[i] ==
                item
            )
            {
                CurrentCharacter
                    .GetComponent<PlayerController>()
                    .Remove(CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Items[i]);
                break;
            }
        }

        //update inventory items in Inventory UI
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        //Update Storage items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().StorageItems =
            collidedItem.GetComponent<ItemBehaviour>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListStorageItems();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();
        Debug.Log("The place button is pressed");
    }

    public void TaskOnReadyToCookButton()
    {
        //add the current item into collided item's cook inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem
            .GetComponent<ItemBehaviour>()
            .PrepareToCook(InventoryManager
                .GetComponent<InventoryManager>()
                .CurrentItem);

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<ItemBehaviour>().food;
        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();

        Item item =
            InventoryManager.GetComponent<InventoryManager>().CurrentItem;

        //remove the item in the current character's inventory
        for (
            int i = 0;
            i < CurrentCharacter.GetComponent<PlayerController>().Items.Count;
            i++
        )
        {
            if (
                CurrentCharacter.GetComponent<PlayerController>().Items[i] ==
                item
            )
            {
                CurrentCharacter
                    .GetComponent<PlayerController>()
                    .Remove(CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Items[i]);
                break;
            }
        }

        //update inventory items in Inventory UI
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();

        //place the food item inside cooker
        Debug.Log("The cook button is pressed");
    }

    public void TaskOnStartToCookButton()
    {
        //add the current item into collided item's cook inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem.GetComponent<ItemBehaviour>().StartToCook();

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<ItemBehaviour>().food;
        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();
    }

    //Data save section: where all the data load and save methods are written
    public void LoadData(GameData data)
    {
        //CurrentCharacter.GetComponent<PlayerController>().health = data.health;
    }

    public void SaveData(ref GameData data)
    {
        //data.health = CurrentCharacter.GetComponent<PlayerController>().health;
    }

    //Play Animation section: where all the method used to display animations
    public IEnumerator PlayPortalAnimation()
    {
        PortalBackground.GetComponent<Animator>().SetBool("start", true);
        yield return new WaitForSeconds(transitionOutTime);
        PortalBackground.GetComponent<Animator>().SetBool("start", false);
        PortalBackground.GetComponent<Animator>().SetBool("end", true);
        yield return new WaitForSeconds(transitionOutTime);
        PortalBackground.GetComponent<Animator>().SetBool("end", false);
    }

    public void PlaySleepAnimation()
    {
    }

    //Update the UI information when an event occurs
    public void UIupdate()
    {
        //Update the current UI information
        HealthBar.value =
            CurrentCharacter.GetComponent<PlayerController>().health;
        StarvationBar.value =
            CurrentCharacter.GetComponent<PlayerController>().Starvation;

        //Update the current camera position
        MainCamera.GetComponent<CameraController>().lookAt =
            CurrentCharacter.transform;

        //Update the character controller script
        for (int i = 0; i < Characters.Length; i++)
        {
            Characters[i].GetComponent<PlayerController>().enabled = false;
            Characters[i].GetComponent<Animator>().SetFloat("speed", 0);
        }
        CurrentCharacter.GetComponent<PlayerController>().enabled = true;
        MainCamera.GetComponent<CameraController>().lookAt =
            CurrentCharacter.transform;
    }

    public void addOneDay()
    {
        DayIndex += 1;
        text = DayRecord.GetComponent<TextMeshProUGUI>();
        text.SetText("DAY" + DayIndex);
    }

    public void displayStoragePannel()
    {
        if (StoragePannelOpened == true)
        {
            //close the inventory
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
            Debug.Log("The Storage is closed");
        }
        else
        {
            //open the inventory
            StorageAnimator.SetBool("IsOpen", true);
            StoragePannelOpened = true;
            Debug.Log("The Storage is opened");
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void ResetDialogButtons()
    {
        UtilizeButton.gameObject.SetActive(false);
        StoreButton.gameObject.SetActive(false);
        DiscardButton.gameObject.SetActive(false);
        PlaceButton.gameObject.SetActive(false);
        ReadyToCookButton.gameObject.SetActive(false);
    }

    public void displayCookerPannel()
    {
        //display the cooker pannel UI
        if (CookerPannelOpened == true)
        {
            //close the inventory
            CookerAnimator.SetBool("IsOpen", false);
            CookerPannelOpened = false;
            Debug.Log("The Storage is closed");
        }
        else
        {
            //open the inventory
            CookerAnimator.SetBool("IsOpen", true);
            CookerPannelOpened = true;
            Debug.Log("The Storage is opened");
        }

        //check player freeze state
        PlayerFreezeState();
    }

    //Character section: where all the actions for character methods are written
    public void PlayerFreezeState()
    {
        if (
            StoragePannelOpened == true ||
            CookerPannelOpened == true ||
            InventoryOpened == true
        )
        {
            CurrentCharacter.GetComponent<PlayerController>().FreezeCharacter();
        }
        else
        {
            CurrentCharacter
                .GetComponent<PlayerController>()
                .UnfreezeCharacter();
        }
    }
}
