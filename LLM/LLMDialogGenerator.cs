using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GhostSystem;
using LLMIntegration;

/// <summary>
/// LLM 对话生成器 — 通过 GhostManager 为 NPC 生成动态对话和剧情文本。
/// </summary>
public class LLMDialogGenerator : MonoBehaviour
{
    public static LLMDialogGenerator Instance { get; private set; }

    [Header("对话模板")]
    [TextArea(2, 5)] public string systemPrompt;

    [Header("生成参数")]
    [Range(0.5f, 10f)] public float generationCooldown = 1f;
    [Range(0f, 1f)] public float fallbackChance = 0.2f;

    private float _lastGenerationTime = float.MinValue;
    private readonly Dictionary<string, string> _cache = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 生成一段 NPC 对话
    /// </summary>
    public void GenerateDialog(string npcContext, Action<string> onDialog)
    {
        var ghost = GhostManager.Instance;
        if (ghost == null || !ghost.enableGhost)
        {
            onDialog?.Invoke(ghost?.GetRandomFallback() ?? "……");
            return;
        }

        string prompt = string.IsNullOrEmpty(systemPrompt)
            ? SYS_PROMPT : systemPrompt;
        string userMessage = BuildUserMessage(npcContext);

        string cacheKey = $"{prompt}|{userMessage}";
        if (_cache.TryGetValue(cacheKey, out string cached))
        {
            onDialog?.Invoke(cached);
            return;
        }

        if (UnityEngine.Random.value < fallbackChance || Time.time - _lastGenerationTime < generationCooldown)
        {
            onDialog?.Invoke(ghost.GetRandomFallback());
            return;
        }

        StartCoroutine(CoGenerate(prompt, userMessage, cacheKey, onDialog));
    }

    public void ClearCache() => _cache.Clear();

    private IEnumerator CoGenerate(string prompt, string userMessage, string cacheKey, Action<string> onDialog)
    {
        _lastGenerationTime = Time.time;
        bool done = false;
        string result = "";

        GhostManager.Instance.SendRequest(prompt, userMessage,
            json =>
            {
                result = ExtractContent(json);
                if (!string.IsNullOrEmpty(result)) _cache[cacheKey] = result;
                done = true;
            },
            _ =>
            {
                result = GhostManager.Instance.GetRandomFallback();
                done = true;
            });

        yield return new WaitUntil(() => done);
        onDialog?.Invoke(result);
    }

    private string BuildUserMessage(string npcContext)
    {
        var ctx = GameContextBuilder.Instance;
        string stateText = ctx != null ? ctx.BuildFullContext() : "";
        return $"NPC上下文：{npcContext}\n游戏状态：{stateText}\n请输出NPC的对话（纯中文，不包含JSON）：";
    }

    private static string ExtractContent(string json)
    {
        try { var r = JsonUtility.FromJson<DialogResponse>(json); if (!string.IsNullOrEmpty(r?.dialog)) return r.dialog; }
        catch { /* 非 JSON，直接返回 */ }
        return json.Trim().Trim('"');
    }

    private const string SYS_PROMPT = @"你是一个末日生存RPG游戏中的NPC对话生成器。
根据NPC的上下文和游戏状态，生成一句自然中文对话（不超过50字）。
风格：203X年AI统治世界后的压抑、紧张、偶尔带有一丝希望。直接输出对话内容。";

    [Serializable] private class DialogResponse { public string dialog; }
}
