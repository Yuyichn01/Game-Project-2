using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour, ISaveable
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

        if (DataManager.Instance != null)
            DataManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.Unregister(this);
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

    public void Save(GameData data)
    {
        if (data?.inventory == null)
            data.inventory = new InventorySaveData();

        // 序列化存储箱物品名称
        if (StorageItems != null && StorageItems.Count > 0)
        {
            data.inventory.storageItemNames = new string[StorageItems.Count];
            for (int i = 0; i < StorageItems.Count; i++)
            {
                data.inventory.storageItemNames[i] = StorageItems[i]?.ItemName ?? string.Empty;
            }
        }
        else
        {
            data.inventory.storageItemNames = new string[0];
        }

        // 序列化烹饪食物名称
        if (FoodItems != null && FoodItems.Count > 0)
        {
            data.inventory.foodItemNames = new string[FoodItems.Count];
            for (int i = 0; i < FoodItems.Count; i++)
            {
                data.inventory.foodItemNames[i] = FoodItems[i]?.ItemName ?? string.Empty;
            }
        }
        else
        {
            data.inventory.foodItemNames = new string[0];
        }
    }

    public void Load(GameData data)
    {
        if (data?.inventory == null) return;

        ItemDatabase itemDb = DataManager.Instance?.ItemDatabase;

        // 恢复存储箱物品
        StorageItems.Clear();
        if (data.inventory.storageItemNames != null && itemDb != null)
        {
            foreach (string itemName in data.inventory.storageItemNames)
            {
                if (string.IsNullOrEmpty(itemName)) continue;
                Item item = itemDb.GetItemByName(itemName);
                if (item != null)
                {
                    StorageItems.Add(item);
                }
            }
        }

        // 恢复烹饪食物
        FoodItems.Clear();
        if (data.inventory.foodItemNames != null && itemDb != null)
        {
            foreach (string itemName in data.inventory.foodItemNames)
            {
                if (string.IsNullOrEmpty(itemName)) continue;
                Item item = itemDb.GetItemByName(itemName);
                if (item != null)
                {
                    FoodItems.Add(item);
                }
            }
        }
    }
}
