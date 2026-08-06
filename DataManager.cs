using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [SerializeField] private ItemDatabase itemDatabase;

    private SaveFileManager fileManager;
    private List<ISaveable> saveables = new List<ISaveable>();

    // 玩家画像临时缓存（菜单阶段收集，创建 GameData 时注入）
    private string[] _pendingProfileAnswers;
    private string _pendingProfileAnalysis;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        fileManager = new SaveFileManager();
    }

    /// <summary>
    /// 注册一个 ISaveable，存档/读档时会被遍历调用
    /// </summary>
    public void Register(ISaveable saveable)
    {
        if (saveable == null) return;
        if (!saveables.Contains(saveable))
        {
            saveables.Add(saveable);
        }
    }

    /// <summary>
    /// 取消注册 ISaveable
    /// </summary>
    public void Unregister(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }

    // ────────────────────────────────────────────
    // 新接口 — 多槽位 + 错误处理 + 校验
    // ────────────────────────────────────────────

    /// <summary>
    /// 保存到指定槽位
    /// </summary>
    /// <param name="slot">目标槽位</param>
    /// <param name="externalData">可选：外部预填充的 GameData（跳过 ISaveable 收集步骤）</param>
    public SaveResult SaveToSlot(SaveSlot slot, GameData externalData = null)
    {
        try
        {
            GameData data = externalData ?? new GameData();

            // 仅当未提供外部数据时才从 ISaveable 收集
            if (externalData == null)
            {
                foreach (ISaveable saveable in saveables)
                {
                    saveable.Save(data);
                }
            }

            // 序列化 JSON
            string jsonData = JsonUtility.ToJson(data);

            // AES 加密
            var (cipher, iv) = CryptoHelper.Encrypt(jsonData);
            if (cipher == null || iv == null)
            {
                return SaveResult.Fail(SaveErrorCode.IoError, "Encryption failed.");
            }

            // 打包 [CRC32][IV][Cipher]
            byte[] packed = CryptoHelper.PackEncryptedData(cipher, iv);
            if (packed == null)
            {
                return SaveResult.Fail(SaveErrorCode.IoError, "Failed to pack encrypted data.");
            }

            // 确保槽位目录存在
            string dataPath = fileManager.GetDataPath(slot);
            string dir = Path.GetDirectoryName(dataPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 写入加密文件
            File.WriteAllBytes(dataPath, packed);

            // 写入元数据
            SaveMetadata meta = fileManager.BuildMetadata(data);
            fileManager.WriteMetadata(slot, meta);

            Debug.Log($"DataManager: Game data saved to slot {(int)slot}.");
            return SaveResult.Ok();
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataManager: Save to slot {(int)slot} failed: {ex.Message}");
            return SaveResult.Fail(SaveErrorCode.IoError, ex.Message);
        }
    }

    /// <summary>
    /// 从指定槽位读取存档
    /// </summary>
    public SaveResult LoadFromSlot(SaveSlot slot)
    {
        try
        {
            string dataPath = fileManager.GetDataPath(slot);

            if (!File.Exists(dataPath))
            {
                Debug.LogWarning($"DataManager: No save file found in slot {(int)slot}.");
                return SaveResult.Fail(SaveErrorCode.FileNotFound, $"No save file in slot {(int)slot}.");
            }

            // 读取加密文件
            byte[] packed = File.ReadAllBytes(dataPath);
            if (packed == null || packed.Length < 4 + 16 + 1)
            {
                return SaveResult.Fail(SaveErrorCode.FileCorrupted, "Save file is too small or empty.");
            }

            // CRC32 校验 + 解包
            var (cipher, iv) = CryptoHelper.UnpackAndVerify(packed);
            if (cipher == null || iv == null)
            {
                return SaveResult.Fail(SaveErrorCode.FileCorrupted, "CRC32 checksum mismatch — file may be corrupted.");
            }

            // AES 解密
            string jsonData = CryptoHelper.Decrypt(cipher, iv);
            if (jsonData == null)
            {
                return SaveResult.Fail(SaveErrorCode.DecryptionFailed, "Failed to decrypt save data.");
            }

            // JSON 反序列化
            GameData data;
            try
            {
                data = JsonUtility.FromJson<GameData>(jsonData);
            }
            catch (Exception ex)
            {
                return SaveResult.Fail(SaveErrorCode.DeserializationFailed, $"JSON deserialization failed: {ex.Message}");
            }

            if (data == null)
            {
                return SaveResult.Fail(SaveErrorCode.DeserializationFailed, "Deserialized GameData is null.");
            }

            // 遍历所有注册的 saveable，让它们读取数据
            foreach (ISaveable saveable in saveables)
            {
                saveable.Load(data);
            }

            Debug.Log($"DataManager: Game data loaded from slot {(int)slot}.");
            return SaveResult.Ok(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataManager: Load from slot {(int)slot} failed: {ex.Message}");
            return SaveResult.Fail(SaveErrorCode.IoError, ex.Message);
        }
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public SaveResult DeleteSlot(SaveSlot slot)
    {
        try
        {
            if (!fileManager.SlotExists(slot))
            {
                return SaveResult.Fail(SaveErrorCode.FileNotFound, $"Slot {(int)slot} does not exist.");
            }

            bool deleted = fileManager.DeleteSlot(slot);
            if (!deleted)
            {
                return SaveResult.Fail(SaveErrorCode.IoError, $"Failed to delete slot {(int)slot}.");
            }

            Debug.Log($"DataManager: Slot {(int)slot} deleted.");
            return SaveResult.Ok();
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataManager: Delete slot {(int)slot} failed: {ex.Message}");
            return SaveResult.Fail(SaveErrorCode.IoError, ex.Message);
        }
    }

    /// <summary>
    /// 获取指定槽位的存档元数据
    /// </summary>
    public SaveMetadata GetSlotMetadata(SaveSlot slot)
    {
        SaveMetadata? meta = fileManager.ReadMetadata(slot);
        return meta ?? default;
    }

    /// <summary>
    /// 获取所有存在存档的槽位列表
    /// </summary>
    public List<SaveSlot> GetAvailableSlots()
    {
        return fileManager.ListExistingSlots();
    }

    /// <summary>
    /// 检查指定槽位是否存在存档
    /// </summary>
    public bool SlotExists(SaveSlot slot)
    {
        return fileManager.SlotExists(slot);
    }

    // ────────────────────────────────────────────
    // 旧接口 — 向后兼容（默认使用 Slot0）
    // ────────────────────────────────────────────

    /// <summary>
    /// [兼容] 保存到默认槽位 Slot0
    /// </summary>
    public void SaveGameData(GameData data)
    {
        // 如果提供了外部数据，用它；否则让 SaveToSlot 自行收集
        SaveResult result = data != null
            ? SaveToSlot(SaveSlot.Slot0, data)
            : SaveToSlot(SaveSlot.Slot0);

        if (!result.Success)
        {
            Debug.LogError($"DataManager: SaveGameData failed: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// [兼容] 从默认槽位 Slot0 读取存档
    /// </summary>
    public GameData LoadGameData()
    {
        SaveResult result = LoadFromSlot(SaveSlot.Slot0);
        if (!result.Success)
        {
            if (result.ErrorCode != SaveErrorCode.FileNotFound)
            {
                Debug.LogError($"DataManager: LoadGameData failed: {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("No saved data found.");
            }
            return null;
        }

        // 恢复玩家画像缓存到内存（供 LLM 对战等后续系统使用）
        if (result.Data.profileAnswers != null)
            _pendingProfileAnswers = result.Data.profileAnswers;
        if (!string.IsNullOrEmpty(result.Data.profileAnalysis))
            _pendingProfileAnalysis = result.Data.profileAnalysis;

        Debug.Log("Game data loaded.");
        return result.Data;
    }

    /// <summary>
    /// 创建新的游戏数据并写入 Slot0
    /// </summary>
    /// <param name="score">初始分数</param>
    /// <param name="dayIndex">初始天数</param>
    /// <param name="targetSceneIndex">目标游戏场景索引（避免存档指向菜单场景）</param>
    public GameData CreateNewGameData(int score = 0, int dayIndex = 1, int targetSceneIndex = -1)
    {
        GameData data = new GameData();
        data.score = score;
        data.dayIndex = dayIndex;
        data.version = 1;
        data.timeScale = 2.0f;
        data.currentSceneIndex = targetSceneIndex >= 0 ? targetSceneIndex : data.currentSceneIndex;
        data.characters = new CharacterSaveData[3];
        data.inventory = new InventorySaveData();
        data.settings = new SettingsSaveData();
        data.plot = new PlotSaveData();

        // 注入玩家画像（菜单阶段收集的）
        if (_pendingProfileAnswers != null)
            data.profileAnswers = _pendingProfileAnswers;
        if (!string.IsNullOrEmpty(_pendingProfileAnalysis))
            data.profileAnalysis = _pendingProfileAnalysis;

        SaveResult result = SaveToSlot(SaveSlot.Slot0, data);
        if (!result.Success)
        {
            Debug.LogError($"DataManager: CreateNewGameData save failed: {result.ErrorMessage}");
        }

        Debug.Log($"New game data created (scene={data.currentSceneIndex}).");
        return data;
    }

    /// <summary>
    /// 保存当前天数到存档（收集当前游戏状态 + 写入指定 dayIndex）
    /// </summary>
    public void SaveDayIndex(int dayIndex)
    {
        GameData data = new GameData();
        foreach (ISaveable saveable in saveables)
            saveable.Save(data);
        data.dayIndex = dayIndex;

        SaveResult result = SaveToSlot(SaveSlot.Slot0, data);
        if (!result.Success)
        {
            Debug.LogError($"DataManager: SaveDayIndex save failed: {result.ErrorMessage}");
        }

        Debug.Log($"Day index saved: {dayIndex}");
    }

    /// <summary>
    /// 获取 ItemDatabase 引用
    /// </summary>
    public ItemDatabase ItemDatabase => itemDatabase;

    // ──────────── 玩家画像托管（DataManager 为数据唯一归属）────────────

    /// <summary>设置玩家问卷回答（菜单阶段调用）</summary>
    public void SetProfileAnswers(string[] answers)
    {
        _pendingProfileAnswers = answers;
    }

    /// <summary>获取缓存的玩家问卷回答</summary>
    public string[] GetProfileAnswers()
    {
        return _pendingProfileAnswers;
    }

    /// <summary>设置 LLM 分析结果（菜单阶段调用）</summary>
    public void SetProfileAnalysis(string analysis)
    {
        _pendingProfileAnalysis = analysis;
    }

    /// <summary>获取 LLM 分析结果</summary>
    public string GetProfileAnalysis()
    {
        return _pendingProfileAnalysis;
    }
}
