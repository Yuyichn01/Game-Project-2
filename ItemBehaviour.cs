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
        CheckPoint,
        CraftingTable,
        Weapon,
        Ladder,
        Plot
    }

    [Header("Interaction UI section")]
    // Changed from Image to SpriteRenderer for a 2D sprite image
    public SpriteRenderer interactionUI;

    public float zoomSpeed = 1f;

    public float scaleAmount = 0.2f;

    public float fadeDuration = 0.2f;

    private Coroutine fadeCoroutine;

    private Vector3 originalScale;

    private bool zoomingOut = true;

    [Header("Dialog section")]
    public Dialog ItemDialog;

    [Header("Plot section")]
    public Plot ItemPlot;

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

    public List<GameObject> objectToHide;

    public List<GameObject> objectToAppear;

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

        if (interactionUI != null)
        {
            originalScale = interactionUI.transform.localScale;
            interactionUI.enabled = false;
        }
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

    IEnumerator ZoomLoop()
    {
        while (true)
        {
            float duration = 1f / zoomSpeed; // 计算完整动画持续时间
            float elapsed = 0f;
            Vector3 startScale = interactionUI.transform.localScale;
            Vector3 targetScale =
                zoomingOut
                    ? originalScale + Vector3.one * scaleAmount
                    : originalScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration; // 标准化进度 [0,1]
                interactionUI.transform.localScale =
                    Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // 确保最终值准确
            interactionUI.transform.localScale = targetScale;
            zoomingOut = !zoomingOut;

            // 可选：添加间隔时间
            yield return new WaitForSeconds(0.1f);
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
                case ItemType.Ladder:
                    CurrentCharacter
                        .GetComponent<PlayerController>()
                        .Add(ItemData);
                    InventoryManager.GetComponent<InventoryManager>().Items =
                        CurrentCharacter.GetComponent<PlayerController>().Items;
                    InventoryManager
                        .GetComponent<InventoryManager>()
                        .ListItems();
                    Debug.Log("this is a ladder");
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
                    if (objectToAppear != null)
                    {
                        for (int i = 0; i < objectToAppear.Count; i++)
                        {
                            if (
                                !objectToAppear[i].activeSelf // only active if it is inactive
                            ) objectToAppear[i].SetActive(true);
                        }
                    }

                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");

                    UIManager.GetComponent<UIManager>().PlayPortalAnimation();

                    if (UIManager.GetComponent<UIManager>().isSingle)
                    {
                        Vector3 newPosition =
                            new Vector3(NextPortalPosition.position.x,
                                NextPortalPosition.position.y,
                                NextPortalPosition.transform.position.z);
                        CurrentCharacter.transform.position = newPosition;
                    }
                    else
                    {
                        Vector3 newPosition =
                            new Vector3(NextPortalPosition.position.x,
                                NextPortalPosition.position.y,
                                NextPortalPosition.transform.position.z + 0.1f);
                        Player1.transform.position = newPosition;

                        newPosition =
                            new Vector3(NextPortalPosition.position.x,
                                NextPortalPosition.position.y,
                                NextPortalPosition.transform.position.z + 0.2f);
                        Player2.transform.position = newPosition;

                        newPosition =
                            new Vector3(NextPortalPosition.position.x,
                                NextPortalPosition.position.y,
                                NextPortalPosition.transform.position.z + 0.3f);
                        Player3.transform.position = newPosition;
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

                    if (objectToHide != null)
                    {
                        for (int i = 0; i < objectToHide.Count; i++)
                        {
                            if (
                                objectToHide[i].activeSelf // only inactive if it is active
                            ) objectToHide[i].SetActive(false);
                        }
                    }

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

                    //add one day
                    UIManager.GetComponent<UIManager>().addOneDay();

                    //reset the current time
                    //allign sky
                    Debug.Log("this is a bed");
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
                case ItemType.Plot:
                    CurrentCharacter
                        .GetComponent<Animator>()
                        .SetTrigger("Interact");
                    UIManager.GetComponent<UIManager>().ResetDialogButtons();
                    if (ItemDialog != null)
                    {
                        DialogManager
                            .GetComponent<DialogManager>()
                            .StartPlot(ItemPlot);
                    }

                    Debug.Log("this is a piece of information");

                    //check whether to set active or not
                    if (destroyAfterRead == true)
                    {
                        Destroy(this.gameObject);
                    }
                    break;
            }
        }
    }

    void SetAlpha(float alpha)
    {
        Color color = interactionUI.color;
        color.a = Mathf.Clamp01(alpha);
        interactionUI.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (
            other.CompareTag("Player1") ||
            other.CompareTag("Player2") ||
            other.CompareTag("Player3")
        )
        {
            interactionUI.enabled = true;
            SetAlpha(0f); // set to transparent
            StartFade(1f); // fade in
            StartCoroutine(ZoomLoop());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (
            other.CompareTag("Player1") ||
            other.CompareTag("Player2") ||
            other.CompareTag("Player3")
        )
        {
            interactionUI.enabled = false;
        }
    }

    void StartFade(float targetAlpha)
    {
        if (Mathf.Approximately(interactionUI.color.a, targetAlpha)) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToAlpha(targetAlpha));
    }

    IEnumerator FadeToAlpha(float targetAlpha)
    {
        float startAlpha = interactionUI.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha =
                Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            SetAlpha (newAlpha);
            yield return null;
        }

        fadeCoroutine = null;
    }
}
