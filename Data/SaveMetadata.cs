using System;

/// <summary>
/// 存档元数据 — 明文存储的摘要信息，用于存档列表展示
/// </summary>
[Serializable]
public struct SaveMetadata
{
    public int version;
    public int dayIndex;
    public int score;
    public long timestamp;       // Unix 毫秒时间戳
    public string sceneName;
    public float playTimeSeconds;
}
