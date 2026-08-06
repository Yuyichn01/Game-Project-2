using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档文件管理器 — 负责文件 I/O、槽位目录管理、元数据读写
/// </summary>
public class SaveFileManager
{
    private readonly string basePath;

    public SaveFileManager()
    {
        basePath = Path.Combine(Application.persistentDataPath, "saves");
        EnsureDirectoryExists();
    }

    /// <summary>
    /// 确保存档根目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
    }

    /// <summary>
    /// 获取槽位目录路径
    /// </summary>
    private string GetSlotDirectory(SaveSlot slot)
    {
        return Path.Combine(basePath, $"slot_{(int)slot}");
    }

    /// <summary>
    /// 获取加密存档数据文件路径
    /// </summary>
    public string GetDataPath(SaveSlot slot)
    {
        return Path.Combine(GetSlotDirectory(slot), "data.dat");
    }

    /// <summary>
    /// 获取明文元数据 JSON 文件路径
    /// </summary>
    public string GetMetaPath(SaveSlot slot)
    {
        return Path.Combine(GetSlotDirectory(slot), "meta.json");
    }

    /// <summary>
    /// 检查指定槽位是否存在存档
    /// </summary>
    public bool SlotExists(SaveSlot slot)
    {
        return File.Exists(GetDataPath(slot));
    }

    /// <summary>
    /// 读取槽位的元数据，不存在返回 null
    /// </summary>
    public SaveMetadata? ReadMetadata(SaveSlot slot)
    {
        string metaPath = GetMetaPath(slot);
        if (!File.Exists(metaPath))
            return null;

        try
        {
            string json = File.ReadAllText(metaPath);
            return JsonUtility.FromJson<SaveMetadata>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveFileManager: Failed to read metadata for slot {(int)slot}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 写入槽位元数据
    /// </summary>
    public void WriteMetadata(SaveSlot slot, SaveMetadata meta)
    {
        try
        {
            string dir = GetSlotDirectory(slot);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(meta, true);
            File.WriteAllText(GetMetaPath(slot), json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveFileManager: Failed to write metadata for slot {(int)slot}: {ex.Message}");
        }
    }

    /// <summary>
    /// 列出所有存在存档的槽位
    /// </summary>
    public List<SaveSlot> ListExistingSlots()
    {
        var slots = new List<SaveSlot>();
        foreach (SaveSlot slot in Enum.GetValues(typeof(SaveSlot)))
        {
            if (SlotExists(slot))
            {
                slots.Add(slot);
            }
        }
        return slots;
    }

    /// <summary>
    /// 删除指定槽位的所有存档文件
    /// </summary>
    public bool DeleteSlot(SaveSlot slot)
    {
        string dir = GetSlotDirectory(slot);
        if (!Directory.Exists(dir))
            return true;

        try
        {
            Directory.Delete(dir, true);
            Debug.Log($"SaveFileManager: Slot {(int)slot} deleted.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveFileManager: Failed to delete slot {(int)slot}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从 GameData 构建存档摘要元数据
    /// </summary>
    public SaveMetadata BuildMetadata(GameData data)
    {
        if (data == null)
            return default;

        return new SaveMetadata
        {
            version = data.version,
            dayIndex = data.dayIndex,
            score = data.score,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sceneName = SceneManager.GetActiveScene().name,
            playTimeSeconds = data.elapsedTimeSeconds
        };
    }
}
