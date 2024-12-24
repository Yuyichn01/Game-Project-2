using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Author: Yi Yu

Date:2024/4/27

Description:
The script respond to UI click

*/
public class ItemUI : MonoBehaviour
{
    public enum ItemType
    {
        InventoryItem,
        StorageItem,
        CookerItem,
        CraftItem
    }

    [Header("ItemUI setting section")]
    public ItemType type;

    [Header("State section")]
    public Item itemData;

    private GameObject InventoryManager;

    private GameObject DialogManager;

    private GameObject UIManager;

    private GameObject currentCharacter;

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
        Debug.Log("The item UI button is clicked");

        //set the current item data in Inventory to this item data
        InventoryManager.GetComponent<InventoryManager>().CurrentItem =
            itemData;

        //set utilize button visibility
        UIManager.GetComponent<UIManager>().ResetDialogButtons();
        UIManager
            .GetComponent<UIManager>()
            .UtilizeButton
            .gameObject
            .SetActive(true);

        //set the current character
        currentCharacter = UIManager.GetComponent<UIManager>().CurrentCharacter;

        switch (type)
        {
            case ItemType.InventoryItem:
                UIManager.GetComponent<UIManager>().lastOpenedPannel =
                    "Inventory";
                if (
                    currentCharacter
                        .GetComponent<PlayerController>()
                        .itemCollider !=
                    null
                )
                {
                    GameObject collidedItem =
                        currentCharacter.GetComponent<PlayerController>().item;

                    //if the player is colliding with storage objects, display place button
                    if (
                        collidedItem.GetComponent<ItemBehaviour>().type ==
                        ItemBehaviour.ItemType.Storage
                    )
                    {
                        UIManager
                            .GetComponent<UIManager>()
                            .PlaceButton
                            .gameObject
                            .SetActive(true);
                    } //if the player is colliding with cooker object and the object is of type "food", display cook button
                    else if (
                        collidedItem.GetComponent<ItemBehaviour>().type ==
                        ItemBehaviour.ItemType.Cooker &&
                        itemData.ItemType == "food"
                    )
                    {
                        UIManager
                            .GetComponent<UIManager>()
                            .ReadyToCookButton
                            .gameObject
                            .SetActive(true);
                    }
                }
                else
                //else display discard button
                {
                    UIManager
                        .GetComponent<UIManager>()
                        .DiscardButton
                        .gameObject
                        .SetActive(true);
                }

                //else, display discard button
                Debug.Log("This is InventoryItem");
                break;
            case ItemType.StorageItem:
                UIManager.GetComponent<UIManager>().lastOpenedPannel =
                    "Storage";
                UIManager
                    .GetComponent<UIManager>()
                    .StoreButton
                    .gameObject
                    .SetActive(true);

                Debug.Log("this is Storage Item");
                break;
            case ItemType.CookerItem:
                UIManager.GetComponent<UIManager>().lastOpenedPannel = "Cooker";
                UIManager
                    .GetComponent<UIManager>()
                    .StoreButton
                    .gameObject
                    .SetActive(true);

                Debug.Log("this is Storage Item");
                break;
        }

        //display dialog window
        DialogManager
            .GetComponent<DialogManager>()
            .StartDialog(itemData.dialog);
    }
}
