using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("State section")]
    public Item CurrentItem;

    [Header("Inventory item section")]
    public Transform ItemContent;

    public GameObject InventoryItem;

    public List<Item> Items = new List<Item>();

    [Header("Storage item section")]
    public Transform StorageContent;

    public List<Item> StorageItems = new List<Item>();

    [Header("Food item section")]
    public Transform FoodContent;

    public List<Item> FoodItems = new List<Item>();

    [Header("Manager section")]
    private GameObject UIManager;

    private GameObject DialogManager;

    private void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        UIManager = GameObject.FindWithTag("UIManager");
        DialogManager = GameObject.FindWithTag("DialogManager");
    }

    public void ListItems()
    {
        foreach (Transform item in ItemContent)
        {
            Destroy(item.gameObject);
        }
        foreach (var item in Items)
        {
            GameObject obj = Instantiate(InventoryItem, ItemContent);
            Image image;
            image = obj.GetComponent<Image>();
            image.sprite = item.icon;
            obj.GetComponent<ItemUI>().itemData = item;
            obj.GetComponent<ItemUI>().type = ItemUI.ItemType.InventoryItem;
        }
    }

    public void ListStorageItems()
    {
        foreach (Transform storageItem in StorageContent)
        {
            Destroy(storageItem.gameObject);
        }
        foreach (var storageItem in StorageItems)
        {
            GameObject obj = Instantiate(InventoryItem, StorageContent);
            Image image = obj.GetComponent<Image>();
            image.sprite = storageItem.icon;
            obj.GetComponent<ItemUI>().itemData = storageItem;
            obj.GetComponent<ItemUI>().type = ItemUI.ItemType.StorageItem;
        }
    }

    public void ListFoodItems()
    {
        foreach (Transform foodItem in FoodContent)
        {
            Destroy(foodItem.gameObject);
        }
        foreach (var foodItem in FoodItems)
        {
            GameObject obj = Instantiate(InventoryItem, FoodContent);
            Image image = obj.GetComponent<Image>();
            image.sprite = foodItem.icon;
            obj.GetComponent<ItemUI>().itemData = foodItem;
            obj.GetComponent<ItemUI>().type = ItemUI.ItemType.CookerItem;
        }
    }
}
