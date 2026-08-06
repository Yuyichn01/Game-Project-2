using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemDatabase — ScriptableObject 物品数据库
/// 维护所有 Item 的引用，支持通过 itemName 反查 Item
/// 存档时用 itemName 字符串，Item 本身不能被 JsonUtility 序列化
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Item/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<Item> allItems = new List<Item>();

    private Dictionary<string, Item> itemLookup;

    /// <summary>
    /// 构建名称查找字典（在首次访问时延迟初始化）
    /// </summary>
    private void BuildLookup()
    {
        if (itemLookup != null) return;

        itemLookup = new Dictionary<string, Item>();
        foreach (Item item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemName))
            {
                if (!itemLookup.ContainsKey(item.ItemName))
                {
                    itemLookup.Add(item.ItemName, item);
                }
                else
                {
                    Debug.LogWarning($"ItemDatabase: Duplicate item name '{item.ItemName}' found, skipping.");
                }
            }
        }
    }

    /// <summary>
    /// 通过物品名称获取 Item
    /// </summary>
    public Item GetItemByName(string itemName)
    {
        BuildLookup();
        itemLookup.TryGetValue(itemName, out Item item);
        return item;
    }

    /// <summary>
    /// 获取所有物品列表（只读）
    /// </summary>
    public List<Item> AllItems
    {
        get
        {
            // 清除缓存以强制重建（当列表可能变化时）
            // 实际使用时如果列表不变，可以在 OnEnable 中调用 BuildLookup
            return allItems;
        }
    }

    private void OnEnable()
    {
        BuildLookup();
    }
}
