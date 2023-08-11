using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUI : MonoBehaviour
{
    public enum ItemType
    {
        InventoryItem,
        StorageItem
    }

    [Header("ItemUI setting section")]
    public ItemType type;

    [Header("State section")]
    public Item itemData;

    private GameObject InventoryManager;

    private GameObject DialogManager;

    private GameObject UIManager;

    public void Start()
    {
        //initialize managers
        DialogManager = GameObject.FindWithTag("DialogManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");
        UIManager = GameObject.FindWithTag("UIManager");
    }

    // Start is called before the first frame update
    public void TaskOnClick()
    {
        //set the current item data in Inventory to this item data
        InventoryManager.GetComponent<InventoryManager>().CurrentItem =
            itemData;
        switch (type)
        {
            case ItemType.InventoryItem:
                //set button visibility
                UIManager.GetComponent<UIManager>().ResetDialogButtons();
                UIManager
                    .GetComponent<UIManager>()
                    .UtilizeButton
                    .gameObject
                    .SetActive(true);
                UIManager
                    .GetComponent<UIManager>()
                    .DiscardButton
                    .gameObject
                    .SetActive(true);

                Debug.Log("This is InventoryItem");
                break;
            case ItemType.StorageItem:
                //set button visibility
                UIManager.GetComponent<UIManager>().ResetDialogButtons();
                UIManager
                    .GetComponent<UIManager>()
                    .UtilizeButton
                    .gameObject
                    .SetActive(true);
                UIManager
                    .GetComponent<UIManager>()
                    .StoreButton
                    .gameObject
                    .SetActive(true);
                UIManager
                    .GetComponent<UIManager>()
                    .DiscardButton
                    .gameObject
                    .SetActive(false);

                Debug.Log("this is StorageItem");
                break;
        }

        //display dialog window
        DialogManager
            .GetComponent<DialogManager>()
            .StartDialog(itemData.dialog);
    }
}
