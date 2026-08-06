using UnityEngine.Localization;

public static class LocalizationManager
{
    private const string TableName = "Main table";

    /// <summary>
    /// 获取本地化字符串（同步，依赖 String Table 已加载）。
    /// </summary>
    public static string Get(string key)
    {
        var loc = new LocalizedString(TableName, key);
        string result = loc.GetLocalizedString();
        // 如果返回的是 key 本身，说明表中没有，输出警告
        if (result == key)
            UnityEngine.Debug.LogWarning($"[Localization] 未找到 key: {key}");
        return result;
    }

    /// <summary>
    /// 获取本地化字符串并格式化。
    /// </summary>
    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
