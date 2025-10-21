using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*Attach to Canvas
Assign Health bar to slider
*/
public class UIManager : MonoBehaviour
{
    [Header("Buttons section")]
    public Button SaveButton;

    public Button LoadButton;

    public Button NewGameButton;

    public Button InventoryButton;

    public Button PhoneButton;

    public Button SwitchButton;

    public Button PressToStartButton;

    public Button ControlModeButton;

    public Button UtilizeButton;

    public Button StoreButton;

    public Button DiscardButton;

    public Button PlaceButton;

    public Button ReadyToCookButton;

    public Button StartToCookButton;

    public Button ReadyToCraftButton;

    public Button StartToCraftButton;

    public Button MapButton;

    public Button ToGoButton;

    private int switchCount = 1;

    [Header("Slider section")]
    public Slider HealthBar;

    public Slider StarvationBar;

    [Header("State section")]
    public bool isSingle = false;

    public GameObject CurrentCharacter;

    public TMP_Text DayRecord;

    public int DayIndex = 1;

    private bool InventoryOpened = false;

    private bool PhoneOpened = false;

    public bool panelOpened = false;

    [Header("Character section")]
    public GameObject[] Characters;

    [Header("Camera section")]
    public Camera MainCamera;

    public GameObject PortalBackground;

    private TextMeshProUGUI text;

    [Header("Sprite section")]
    public Sprite[] ControlModeButtonSprites;

    [Header("Manager section")]
    private GameObject DialogManager;

    private GameObject InventoryManager;

    [Header("Inventory section")]
    public Animator InventoryAnimator;

    public string lastOpenedPannel;

    [Header("Storage section")]
    public Animator StorageAnimator;

    private bool StoragePannelOpened = false;

    [Header("Cooker section")]
    public Animator CookerAnimator;

    private bool CookerPannelOpened = false;

    [Header("Phone section")]
    public Animator PhoneAnimator;

    public List<GameObject> phonePannels;

    [Header("Map section")]
    public GameObject MapPannel;

    private int nextPlace = 0;

    private bool MapOpened = false;

    [Header("Dialog section")]
    public bool dialogPannelOpened = false;

    [Header("Crafting table section")]
    public Animator CraftingTableAnimator;

    private bool CraftingTableOpened = false;

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
        UtilizeButton.onClick.AddListener (TaskOnUtilizeButton);
        StoreButton.onClick.AddListener (TaskOnStoreButton);
        DiscardButton.onClick.AddListener (TaskOnDiscardButton);
        PlaceButton.onClick.AddListener (TaskOnPlaceButton);
        ReadyToCookButton.onClick.AddListener (TaskOnReadyToCookButton);
        StartToCookButton.onClick.AddListener (TaskOnCookButton);
        ReadyToCraftButton.onClick.AddListener (TaskOnReadyToCraftButton);
        StartToCraftButton.onClick.AddListener (TaskOnCraftButton);
        PhoneButton.onClick.AddListener (TaskOnPhoneButton);
        MapButton.onClick.AddListener (TaskOnMapButton);
        ToGoButton.onClick.AddListener (TaskOnToGoButton);

        //Initialize the day record information
        text = DayRecord.GetComponent<TextMeshProUGUI>();
        text.SetText("DAY" + DayIndex);

        //Initialize to the first character
        CurrentCharacter = Characters[0];
        UIupdate();
    }

    public void SetNextPlace(int num)
    {
        nextPlace = num;
    }

    //Buttons section: where all the button control methods are written
    public void TaskOnClickSaveButton()
    {
        Debug.Log("Saving game data");
    }

    public void TaskOnClickLoadButton()
    {
        Debug.Log("Loading game data");
    }

    public void TaskOnClickNewGameButton()
    {
        Debug.Log("Initializing new game");
    }

    public void TaskOnInventoryButton()
    {
        updateInventoryUI();
        if (InventoryOpened == true)
        {
            //close the inventory
            InventoryAnimator.SetBool("IsOpen", false);
            InventoryOpened = false;
            Debug.Log("The Inventory is closed");
        }
        else
        {
            closeAllPannels();

            //open the inventory
            InventoryAnimator.SetBool("IsOpen", true);
            InventoryOpened = true;

            Debug.Log("The Inventory is opened");
            lastOpenedPannel = "Inventory";
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void TaskOnSwitchButton()
    {
        closeAllPannels();

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

        updateInventoryUI();

        //close all opned pannels
        closeAllPannels();

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

    public void TaskOnUtilizeButton()
    {
        //utilize the item
        CurrentCharacter
            .GetComponent<PlayerController>()
            .Utilize(InventoryManager
                .GetComponent<InventoryManager>()
                .CurrentItem);

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();

        //update UI
        UIupdate();
    }

    public void TaskOnStoreButton()
    {
        //add the item to the character's inventory
        CurrentCharacter
            .GetComponent<PlayerController>()
            .Add(InventoryManager.GetComponent<InventoryManager>().CurrentItem);

        updateInventoryUI();

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
            ItemBehaviour.ItemType.CraftingTable
        )
        //delete the food item in this item's food storage
        {
            for (
                int i = 0;
                i < m_gameobject.GetComponent<CraftingTable>().food.Count;
                i++
            )
            {
                if (
                    m_gameobject.GetComponent<CraftingTable>().food[i] ==
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .CurrentItem
                )
                {
                    m_gameobject
                        .GetComponent<CraftingTable>()
                        .CancelCook(m_gameobject
                            .GetComponent<CraftingTable>()
                            .food[i]);
                    break;
                }
            }
        } // else if the current object is of type crafting table
        else if (
            m_gameobject.GetComponent<ItemBehaviour>().type ==
            ItemBehaviour.ItemType.Cooker
        )
        //delete the food item in this item's food storage
        {
            for (
                int i = 0;
                i < m_gameobject.GetComponent<Cooker>().food.Count;
                i++
            )
            {
                if (
                    m_gameobject.GetComponent<Cooker>().food[i] ==
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .CurrentItem
                )
                {
                    m_gameobject
                        .GetComponent<Cooker>()
                        .CancelCook(m_gameobject
                            .GetComponent<Cooker>()
                            .food[i]);
                    break;
                }
            }
        }

        //Update the food items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            m_gameobject.GetComponent<Cooker>().food;
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

            updateInventoryUI();

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

        updateInventoryUI();

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
            .GetComponent<Cooker>()
            .PrepareToCook(InventoryManager
                .GetComponent<InventoryManager>()
                .CurrentItem);

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<Cooker>().food;
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

        updateInventoryUI();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();

        //place the food item inside cooker
        Debug.Log("The ready to cook button is pressed");
    }

    public void TaskOnCookButton()
    {
        //add the current item into collided item's cook inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem.GetComponent<Cooker>().StartToCook();

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<Cooker>().food;
        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();

        Debug.Log("The cook button is pressed");
    }

    //crafting table button tasks
    public void TaskOnReadyToCraftButton()
    {
        //add the current item into collided item's cook inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem
            .GetComponent<Cooker>()
            .PrepareToCook(InventoryManager
                .GetComponent<InventoryManager>()
                .CurrentItem);

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<CraftingTable>().food;
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

        updateInventoryUI();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();

        //place the food item inside cooker
        Debug.Log("The ready to craft button is pressed");
    }

    public void TaskOnCraftButton()
    {
        //add the current item into collided item's cook inventory
        GameObject collidedItem =
            CurrentCharacter.GetComponent<PlayerController>().item;
        collidedItem.GetComponent<CraftingTable>().StartToCook();

        //Update Cook items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().FoodItems =
            collidedItem.GetComponent<CraftingTable>().food;
        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();

        Debug.Log("The craft button is pressed");
    }

    public void TaskOnPhoneButton()
    {
        if (PhoneOpened == true)
        {
            //close the phone
            PhoneAnimator.SetBool("IsOpen", false);
            PhoneOpened = false;
            Debug.Log("The Inventory is closed");
        }
        else
        {
            closeAllPannels();
            for (int i = 0; i < Characters.Length; i++)
            {
                if (CurrentCharacter == Characters[i])
                {
                    if (phonePannels[i] != null && !phonePannels[i].activeSelf)
                    {
                        // Activate the current character's phone panel
                        phonePannels[i].SetActive(true);
                    }
                }
                else
                {
                    // Deactivate other characters' phone panels
                    if (phonePannels[i] != null && phonePannels[i].activeSelf)
                    {
                        phonePannels[i].SetActive(false);
                    }
                }
            }

            //open the phone
            PhoneAnimator.SetBool("IsOpen", true);
            PhoneOpened = true;

            Debug.Log("The phone pannel is pulled");
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void TaskOnMapButton()
    {
        //display the cooker pannel UI
        if (MapOpened)
        {
            //close the map
            MapPannel.GetComponent<AppOpenAnimation>().CloseApp();
            MapOpened = false;
            Debug.Log("The map pannel is closed");
        }
        else
        {
            MapOpened = true;
            lastOpenedPannel = "Map";

            //open the map
            MapPannel.GetComponent<AppOpenAnimation>().OpenApp();
            Debug.Log("The map pannel is pulled");
        }

        closeAllPannels();

        //check player freeze state
        PlayerFreezeState();
    }

    public void TaskOnToGoButton()
    {
        //teleport to the designated scene
        if (nextPlace != 0)
        {
            SceneManager.LoadScene (nextPlace);
        }
    }

    //Play Animation section: where all the method used to display animations
    public void PlayPortalAnimation()
    {
        if (PortalBackground.GetComponent<Animator>().GetBool("Start"))
        {
            PortalBackground.GetComponent<Animator>().SetBool("Start", false);
        }
        else
        {
            PortalBackground.GetComponent<Animator>().SetBool("Start", true);
        }
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

        //update inventory pannel
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        InventoryManager.GetComponent<InventoryManager>().ListFoodItems();

        InventoryManager.GetComponent<InventoryManager>().ListStorageItems();
    }

    public void updateInventoryUI()
    {
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerController>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();
    }

    public void addOneDay()
    {
        DayIndex += 1;

        DayRecord.text = ("DAY" + DayIndex);
    }

    public void displayStoragePannel()
    {
        if (StoragePannelOpened == true)
        {
            //close the storage
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
            Debug.Log("The Storage is closed");
        }
        else
        {
            closeAllPannels();

            //open the storage
            StorageAnimator.SetBool("IsOpen", true);
            StoragePannelOpened = true;
            Debug.Log("The Storage is opened");
            lastOpenedPannel = "Storage";
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void closeAllPannels()
    {
        //close the inventory
        if (InventoryOpened)
        {
            InventoryAnimator.SetBool("IsOpen", false);
            InventoryOpened = false;
        }
        if (StoragePannelOpened)
        {
            //close the storage
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
        }
        if (PhoneOpened)
        {
            //close the phone
            PhoneAnimator.SetBool("IsOpen", false);
            PhoneOpened = false;
        }

        //close the crafting table pannel UI
        if (CraftingTableOpened)
        {
            //close the crafting table
            CraftingTableAnimator.SetBool("IsOpen", false);
            CraftingTableOpened = false;
        }

        //close the dialog pannel UI
        DialogManager.GetComponent<DialogManager>().EndDialog();

        Debug.Log("All pannels are closed");
    }

    public void ResetDialogButtons()
    {
        UtilizeButton.gameObject.SetActive(false);
        StoreButton.gameObject.SetActive(false);
        DiscardButton.gameObject.SetActive(false);
        PlaceButton.gameObject.SetActive(false);
        ReadyToCookButton.gameObject.SetActive(false);
        ReadyToCraftButton.gameObject.SetActive(false);
        StartToCraftButton.gameObject.SetActive(false);
        ToGoButton.gameObject.SetActive(false);
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
            lastOpenedPannel = "Cooker";
        }

        //check player freeze state
        PlayerFreezeState();
    }

    public void displayCraftingTable()
    {
        //display the cooker pannel UI
        if (CraftingTableOpened == true)
        {
            //close the inventory
            CraftingTableAnimator.SetBool("IsOpen", false);
            CraftingTableOpened = false;
            Debug.Log("The crafting table is closed");
        }
        else
        {
            //open the inventory
            CraftingTableAnimator.SetBool("IsOpen", true);
            CraftingTableOpened = true;
            Debug.Log("The Crafting table is opened");
            lastOpenedPannel = "CraftTable";
        }

        //check player freeze state
        PlayerFreezeState();
    }

    //Character section: where all the actions for character methods are written
    public void PlayerFreezeState()
    {
        if (
            StoragePannelOpened ||
            CookerPannelOpened ||
            InventoryOpened ||
            PhoneOpened ||
            CraftingTableOpened ||
            MapOpened ||
            dialogPannelOpened
        )
        {
            CurrentCharacter.GetComponent<PlayerController>().FreezeCharacter();
            panelOpened = true;
        }
        else
        {
            CurrentCharacter
                .GetComponent<PlayerController>()
                .UnfreezeCharacter();
            panelOpened = false;
        }
    }

    public void disablePlayerController()
    {
        CurrentCharacter.GetComponent<PlayerController>().enabled = true;
    }

    public void eablePlayerController()
    {
        CurrentCharacter.GetComponent<PlayerController>().enabled = false;
    }
}
