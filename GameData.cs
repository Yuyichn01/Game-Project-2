using System;

[Serializable]
public class GameData
{
    public int version = 1;
    public int currentSceneIndex;
    public int dayIndex = 1;
    public float elapsedTimeSeconds;
    public float timeScale = 2.0f;
    public CharacterSaveData[] characters = new CharacterSaveData[3];
    public InventorySaveData inventory;
    public SettingsSaveData settings;
    public PlotSaveData plot;

    /// <summary>LLM 玩家画像 — 问卷原始回答（Q: … → A: …）</summary>
    public string[] profileAnswers;

    /// <summary>LLM 对玩家画像的分析结果（AI 反派策略文本）</summary>
    public string profileAnalysis;

    // 向后兼容字段（v0 存档迁移用）
    public int score;
    public float[] position;
}

[Serializable]
public struct CharacterSaveData
{
    public float[] position;
    public float health;
    public float starvation;
    public float fatigue;
    public string[] inventoryItems;
}

[Serializable]
public struct InventorySaveData
{
    public string[] storageItemNames;
    public string[] foodItemNames;
}

[Serializable]
public struct SettingsSaveData
{
    public float masterVolume;
    public string llmApiUrl;
    public string llmApiKey;
    public string llmModelName;
    public string localeCode;
}

[Serializable]
public struct PlotSaveData
{
    public string[] completedPlotIds;
}
