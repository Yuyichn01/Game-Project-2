using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        Bed,
        Escalator,
        CheckPoint,
        CraftingTable,
        Weapon
    }

    [Header("Interaction UI section")]
    // Changed from Image to SpriteRenderer for a 2D sprite image
    public SpriteRenderer interactionUI;

    private float fadeDuration = 0.2f;

    private bool isFadingIn = true;

    [Header("Dialog section")]
    public Dialog ItemDialog;

    public bool destroyAfterRead = false;

    [Header("Item section")]
    // Whether the item is pickable
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

    [Header("Check point section")]
    public int CheckPointSceneIndex = 0;

    public void Start()
    {
        // Assign managers and players variable
        Player1 = GameObject.FindWithTag("Player1");
        Player2 = GameObject.FindWithTag("Player2");
        Player3 = GameObject.FindWithTag("Player3");
        UIManager = GameObject.FindWithTag("UIManager");
        DialogManager = GameObject.FindWithTag("DialogManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");

        StartCoroutine(FadeImage());
    }

    public void Add(Item item)
    {
        Item tmpItem = item;
        Items.Add (tmpItem);
    }

    public void Remove(Item item)
    {
        //remove the item in the storage
        Items.Remove (item);
    }

    IEnumerator FadeImage()
    {
        while (true)
        {
            float alpha = isFadingIn ? 0f : 1f;
            float targetAlpha = isFadingIn ? 1f : 0f;

            Color color = interactionUI.color;
            while (Mathf.Abs(color.a - targetAlpha) > 0.01f)
            {
                color.a =
                    Mathf
                        .Lerp(color.a,
                        targetAlpha,
                        Time.deltaTime / fadeDuration);
                interactionUI.color = color;
                yield return null;
            }

            isFadingIn = !isFadingIn;
            yield return new WaitForSeconds(0.01f);
        }
    }

    // the transform of current item
    public void itemBehaviour()
    {
        // Update the Current Character
        CurrentCharacter = UIManager.GetComponent<UIManager>().CurrentCharacter;

        // If the item is pickable, do these actions
        if (pickable == true)
        {
            //Play interact animation
            CurrentCharacter.GetComponent<Animator>().SetTrigger("Interact");

            // Add the object to the picked-up Items list
            switch (type)
            {
                case ItemType.Food:
                    CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Add(ItemData);
                    InventoryManager.GetComponent<InventoryManager>().Items =
                        CurrentCharacter.GetComponent<PlayerController>().Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListItems();
                    Debug.Log("This is food");
                    break;
                case ItemType.Tool:
                    Debug.Log("this is a tool");
                    break;
                case ItemType.Heart:
                    Debug.Log("this is a Heart");
                    CurrentCharacter.GetComponent<PlayerController>().health +=
                        10;
                    break;
                case ItemType.Weapon:
                    CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Add(ItemData);
                    InventoryManager.GetComponent<InventoryManager>().Items =
                        CurrentCharacter.GetComponent<PlayerController>().Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListItems();
                    Debug.Log("This is a weapon");
                    break;
            }

            Destroy(this.gameObject);
        }
        else
        {
            // If the item is not pickable do these actions
            switch (type)
            {
                case ItemType.Entry:
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    Debug.Log("this is an entry");
                    SceneManager.LoadScene (sceneIndex);
                    break;
                case ItemType.Portal:
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    StartCoroutine(UIManager
                        .GetComponent<UIManager>()
                        .PlayPortalAnimation());

                    if (UIManager.GetComponent<UIManager>().isSingle == true)
                    {
                        CurrentCharacter.transform.position =
                            NextPortalPosition.position;
                    }
                    else
                    {
                        Player1.transform.position =
                            NextPortalPosition.position;
                        Player2.transform.position =
                            NextPortalPosition.position;
                        Player3.transform.position =
                            NextPortalPosition.position;
                    }

                    UIManager
                        .GetComponent<UIManager>()
                        .MainCamera
                        .transform
                        .position =
                        new Vector3(NextPortalPosition.position.x,
                            NextPortalPosition.position.y,
                            NextPortalPosition.position.z -
                            UIManager
                                .GetComponent<UIManager>()
                                .MainCamera
                                .GetComponent<CameraController>()
                                .fixedZPosition);

                    Debug.Log("this is a portal");
                    break;
                case ItemType.Storage:
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .StorageItems = Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListStorageItems();
                    UIManager.GetComponent<UIManager>().displayStoragePannel();
                    Debug.Log("this is Storage");
                    break;
                case ItemType.Cooker:
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    // Access the Cooker script on this object
                    Cooker cooker = GetComponent<Cooker>();
                    if (cooker != null)
                    {
                        // Display the cooker UI panel, list its items
                        InventoryManager
                            .GetComponent<InventoryManager>()
                            .FoodItems = cooker.food;
                        InventoryManager
                            .GetComponent<InventoryManager>()
                            .ListFoodItems();
                        UIManager
                            .GetComponent<UIManager>()
                            .displayCookerPannel();
                        Debug.Log("this is a cooker");
                    }
                    else
                    {
                        Debug
                            .LogWarning("Please attatch the cooker script under this object");
                    }
                    break;
                case ItemType.Information:
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    UIManager.GetComponent<UIManager>().ResetDialogButtons();
                    if (ItemDialog != null)
                    {
                        DialogManager
                            .GetComponent<DialogManager>()
                            .StartDialog(ItemDialog);
                    }

                    Debug.Log("this is a piece of information");

                    //check whether to set active or not
                    if (destroyAfterRead == true)
                    {
                        Destroy(this.gameObject);
                    }
                    break;
                case ItemType.Bed:
                    UIManager.GetComponent<UIManager>().addOneDay();
                    Debug.Log("this is a bed");
                    break;
                case ItemType.Escalator:
                    EscalatorController controller =
                        GetComponentInChildren<EscalatorController>();
                    controller.stepOnEscalator = true;
                    break;
                case ItemType.CheckPoint:
                    SceneManager.LoadScene (CheckPointSceneIndex);
                    break;
                case ItemType.CraftingTable:
                    //Play interact animation
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    // Access the Crafting table script on this object
                    CraftingTable craftTable = GetComponent<CraftingTable>();
                    if (craftTable != null)
                    {
                        // Display the crafting UI panel, list its items
                        InventoryManager
                            .GetComponent<InventoryManager>()
                            .FoodItems = craftTable.food;
                        InventoryManager
                            .GetComponent<InventoryManager>()
                            .ListFoodItems();
                        UIManager
                            .GetComponent<UIManager>()
                            .displayCraftingTable();
                        Debug.Log("This is a Crafting table");
                    }
                    else
                    {
                        Debug
                            .LogWarning("Please attatch the crafting table script under this object");
                    }
                    break;
            }
        }
    }
}
