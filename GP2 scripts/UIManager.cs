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

    private int switchCount = 1;

    [Header("Slider section")]
    public Slider HealthBar;

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
    public GameObject PortalBackground;

    public float ScreenBlankTime = 0.8f;

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

        //Initialize the health bar value
        HealthBar.value = Characters[0].GetComponent<PlayerValue>().health;

        //Initialize the day record information
        text = DayRecord.GetComponent<TextMeshProUGUI>();
        text.SetText("DAY" + DayIndex);

        //Initialize to the first character
        CurrentCharacter = Characters[0];
        UIupdate();
    }

    //Update the UI information when an event occurs
    public void UIupdate()
    {
        //Update the current UI information
        HealthBar.value = CurrentCharacter.GetComponent<PlayerValue>().health;

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
            CurrentCharacter.GetComponent<PlayerValue>().Items;
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
    }

    public void TaskOnSwitchButton()
    {
        switchCount += 1;
        if (switchCount % 3 == 1)
        {
            Debug.Log("switched to character 1");
            CurrentCharacter = Characters[0];
            UIupdate();
        }
        else if (switchCount % 3 == 2)
        {
            Debug.Log("switched to character 2");
            CurrentCharacter = Characters[1];
            UIupdate();
        }
        else if (switchCount % 3 == 0)
        {
            Debug.Log("switched to character 3");
            CurrentCharacter = Characters[2];
            UIupdate();
        }
        SwitchButton.GetComponent<Image>().sprite =
            CurrentCharacter.GetComponent<PlayerValue>().CharacterMiniSprite;

        //Update Inventory items
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerValue>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();
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
            .GetComponent<PlayerValue>()
            .Add(InventoryManager.GetComponent<InventoryManager>().CurrentItem);

        //update item in Inventory manager
        InventoryManager.GetComponent<InventoryManager>().Items =
            CurrentCharacter.GetComponent<PlayerValue>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListItems();

        //remove the item from orginal item behaviour
        GameObject m_gameobject =
            CurrentCharacter.GetComponent<PlayerController>().item;
        for (
            int i = 0;
            i < m_gameobject.GetComponent<ItemBehaviour>().Items.Count;
            i++
        )
        {
            if (
                m_gameobject.GetComponent<ItemBehaviour>().Items[i] ==
                InventoryManager.GetComponent<InventoryManager>().CurrentItem
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

        //Update Storage items in item behaviour
        InventoryManager.GetComponent<InventoryManager>().StorageItems =
            m_gameobject.GetComponent<ItemBehaviour>().Items;
        InventoryManager.GetComponent<InventoryManager>().ListStorageItems();

        //close dialog window
        DialogManager.GetComponent<DialogManager>().EndDialog();
        Debug.Log("Store button is pressed");
    }

    public void TaskOnDiscardButton()
    {
        //remove the item from the character's inventory
        //respon the item instance
        //close dialog window
        Debug.Log("Discard  button is pressed");
    }

    public void LoadData(GameData data)
    {
        CurrentCharacter.GetComponent<PlayerValue>().health = data.health;
    }

    public void SaveData(ref GameData data)
    {
        data.health = CurrentCharacter.GetComponent<PlayerValue>().health;
    }

    public IEnumerator PlayPortalAnimation()
    {
        PortalBackground.SetActive(true);
        PortalBackground.GetComponent<Animator>().SetBool("end", true);
        yield return new WaitForSeconds(ScreenBlankTime);
        PortalBackground.SetActive(false);
    }

    public void displayStoragePannel()
    {
        if (StoragePannelOpened == true)
        {
            //close the inventory
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
            Debug.Log("The Storage is closed");

            //unfreeze current character position
            CurrentCharacter
                .GetComponent<PlayerController>()
                .UnfreezeCharacter();
        }
        else
        {
            //open the inventory
            StorageAnimator.SetBool("IsOpen", true);
            StoragePannelOpened = true;
            Debug.Log("The Storage is opened");

            //unfreeze current character position
            CurrentCharacter.GetComponent<PlayerController>().FreezeCharacter();
        }
    }

    public void CloseStoragePannel()
    {
        if (StoragePannelOpened == true)
        {
            //close the inventory
            StorageAnimator.SetBool("IsOpen", false);
            StoragePannelOpened = false;
            Debug.Log("The Storage is closed");
        }

        //unfreeze current character position
        CurrentCharacter.GetComponent<PlayerController>().UnfreezeCharacter();
    }

    public void ResetDialogButtons()
    {
        UtilizeButton.gameObject.SetActive(false);

        StoreButton.gameObject.SetActive(false);

        DiscardButton.gameObject.SetActive(false);
    }
}
