using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemButton : MonoBehaviour
{
    public Dialog dialog;

    public Item itemData;

    private GameObject InventoryManager;

    public void Start()
    {
        InventoryManager = GameObject.FindWithTag("InventoryManager");
    }

    public void TriggerDialog()
    {
        FindObjectOfType<DialogManager>().StartDialog(dialog);
    }

    public void SetCurrentItem()
    {
        InventoryManager.GetComponent<InventoryManager>().CurrentItem =
            itemData;
        Debug.Log("current item set to pressed item");
    }
}
