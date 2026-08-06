using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using LLMIntegration;

namespace GhostSystem
{
    // ============================================================
    //  GhostManager — LLM 内核 + Ghost 调度 统一管理器
    // ============================================================

    /// <summary>
    /// Ghost 系统统一管理器。挂载到 Persistent GameObject，全局单例。
    /// 合并了原 LLMDriver 的 API 调用能力和原 GhostManager 的 NPC/敌人/叙事决策调度。
    /// </summary>
    public class GhostManager : MonoBehaviour
    {
        public static GhostManager Instance { get; private set; }

        // ============================================================
        //  LLM 配置 (原 LLMDriver)
        // ============================================================

        [Header("LLM 核心配置")]
        [Tooltip("LLM API 地址")]
        public string apiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
        [Tooltip("LLM API Key")]
        public string apiKey = "";
        [Tooltip("模型名称")]
        public string modelName = "qwen-plus";
        [Tooltip("最大 token 数")]
        public int maxTokens = 512;
        [Tooltip("温度（0~1）")]
        [Range(0f, 1f)]
        public float temperature = 0.7f;
        [Tooltip("HTTP 请求超时(秒)")]
        public int requestTimeout = 15;
        [Tooltip("两次请求最小间隔(秒)")]
        [Range(0.5f, 5f)]
        public float requestCooldown = 1f;
        [Tooltip("启用响应缓存（相同 prompt 不重复请求）")]
        public bool enableCache = true;
        [Tooltip("全局 LLM 开关")]
        public bool enableGhost = true;
        [Tooltip("Fallback 对话列表（LLM 不可用时的备用文本）")]
        [TextArea(1, 2)]
        public string[] fallbackDialogues = new[]
        {
            "...", "(沉默)", "……", "[nothing]"
        };

        // ============================================================
        //  Ghost 调度配置
        // ============================================================

        [Header("Ghost 调度")]
        [Tooltip("单帧最大 LLM 调用次数。")]
        [Range(1, 5)]
        public int maxRequestsPerFrame = 1;
        [Tooltip("NPC 决策全局冷却（秒）。")]
        [Range(0.5f, 30f)]
        public float npcDecisionCooldown = 3f;
        [Tooltip("敌人决策全局冷却（秒）。")]
        [Range(0.5f, 30f)]
        public float enemyDecisionCooldown = 2f;
        [Tooltip("叙事事件检查间隔（秒）。")]
        [Range(5f, 120f)]
        public float narrativeCheckInterval = 30f;

        [Header("调试")]
        public bool verboseLogging;

        // ============================================================
        //  运行时状态 (LLM)
        // ============================================================

        private float _lastSendTime = float.MinValue;
        private readonly Dictionary<string, string> _responseCache = new();

        // ============================================================
        //  运行时状态 (调度)
        // ============================================================

        private readonly Queue<PendingRequest> _queue = new();
        private int _requestsThisFrame;
        private Coroutine _processCoroutine;
        private float _lastNPCTime = float.MinValue;
        private float _lastEnemyTime = float.MinValue;
        private float _lastNarrativeTime = float.MinValue;

        // 调度优先级常量
        private const int PRIORITY_BATTLE = 0;
        private const int PRIORITY_NPC = 100;
        private const int PRIORITY_NARRATIVE = 200;

        // ============================================================
        //  Unity 生命周期
        // ============================================================

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
        }

        private void Update()
        {
            if (!enableGhost) return;
            _requestsThisFrame = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ════════════════════════════════════════════════════════
        //  底层 API — LLM 请求 (原 LLMDriver.SendRequest)
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 发送 LLM 请求。
        /// </summary>
        /// <param name="systemPrompt">系统角色 prompt</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="onResult">成功回调，返回 LLM 响应原文</param>
        /// <param name="onError">失败回调</param>
        public void SendRequest(string systemPrompt, string userMessage,
            Action<string> onResult, Action<string> onError)
        {
            if (!enableGhost || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
            {
                onError?.Invoke("Ghost not enabled or missing api config");
                return;
            }

            StartCoroutine(CoSendRequest(systemPrompt, userMessage, onResult, onError));
        }

        /// <summary>
        /// 发送带对话历史的 LLM 请求。
        /// </summary>
        public void SendRequestWithHistory(List<ChatMessage> history,
            string userMessage, Action<string> onResult, Action<string> onError)
        {
            if (!enableGhost || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
            {
                onError?.Invoke("Ghost not enabled or missing api config");
                return;
            }
            StartCoroutine(CoSendRequestWithHistory(history, userMessage, onResult, onError));
        }

        /// <summary>
        /// 获取随机 Fallback 对话。
        /// </summary>
        public string GetRandomFallback()
        {
            if (fallbackDialogues == null || fallbackDialogues.Length == 0)
                return "...";
            return fallbackDialogues[UnityEngine.Random.Range(0, fallbackDialogues.Length)];
        }

        /// <summary>
        /// 清除 LLM 响应缓存。
        /// </summary>
        public void ClearCache()
        {
            _responseCache.Clear();
            if (verboseLogging)
                Debug.Log("[GhostManager] LLM response cache cleared.");
        }

        // ════════════════════════════════════════════════════════
        //  底层 API — 玩家画像分析 (原 LLMDriver.AnalyzePlayerProfile)
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 分析玩家画像（菜单阶段）。
        /// </summary>
        public void AnalyzePlayerProfile(Action<string> onResult, Action<string> onError)
        {
            if (!enableGhost || string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Ghost not enabled");
                return;
            }

            DataManager dm = DataManager.Instance;
            string[] answers = dm != null ? dm.GetProfileAnswers() : new string[0];
            if (answers.Length == 0)
            {
                onError?.Invoke("No profile answers available");
                return;
            }

            string systemPrompt =
@"你是玩家画像分析专家。根据玩家的问卷回答，生成一段简短的个性画像描述（50-80字），
并给出推荐的初始装备偏好（explorer/survivor/fighter 三选一）。
输出严格 JSON:
{
  ""profile"": ""中文描述"",
  ""playstyle"": ""explorer|survivor|fighter"",
  ""reasoning"": ""一句话理由""
}";

            string userMsg = "玩家问卷答案:\n" + string.Join("\n- ", answers);

            SendRequest(systemPrompt, userMsg, raw =>
            {
                try
                {
                    string json = ExtractJson(raw);
                    var r = JsonUtility.FromJson<ProfileResult>(json);
                    onResult?.Invoke(r.profile ?? "一位冒险者从远方而来。");
                    if (dm != null) dm.SetProfileAnalysis(r.profile ?? "");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GhostManager] Profile parsing failed: {e.Message}");
                    onResult?.Invoke("一位冒险者从远方而来。");
                }
            }, onError);
        }

        // ════════════════════════════════════════════════════════
        //  公开 API — NPC 决策
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 请求 LLM 决定 NPC 行为和对话。
        /// </summary>
        public void RequestNPCDecision(NPCContext ctx, Action<NPCResult> onResult)
        {
            if (!enableGhost || ctx == null)
            {
                onResult?.Invoke(NPCResult.Fallback());
                return;
            }
            if (Time.time < _lastNPCTime + npcDecisionCooldown)
            {
                onResult?.Invoke(NPCResult.Fallback());
                return;
            }
            _lastNPCTime = Time.time;

            string systemPrompt = @"你是末日生存RPG中NPC的AI大脑。根据NPC身份和当前状态，决定其行为和对话。
输出严格 JSON:
{""behavior"":""idle|wander|flee|trade|quest_give|follow_player|combat_support|call_help"",""dialog"":""中文对话，不超过80字"",""fearChange"":-1~1,""trustChange"":-1~1,""angerChange"":-1~1,""reasoning"":""决策理由""}
规则:恐惧高时更容易flee/call_help，信任高时更容易trade/follow_player，愤怒高时更容易combat_support，行为应有持续性";

            string userMessage = BuildNPCUserMessage(ctx);
            Enqueue(PRIORITY_NPC, systemPrompt, userMessage, json =>
                onResult?.Invoke(ParseNPCResult(json)),
                _ => onResult?.Invoke(NPCResult.Fallback()));
        }

        // ════════════════════════════════════════════════════════
        //  公开 API — 敌人决策
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 请求 LLM 决定敌人战术。
        /// </summary>
        public void RequestEnemyDecision(EnemyContext ctx, Action<EnemyResult> onResult)
        {
            if (!enableGhost || ctx == null)
            {
                onResult?.Invoke(EnemyResult.Fallback());
                return;
            }
            if (Time.time < _lastEnemyTime + enemyDecisionCooldown)
            {
                onResult?.Invoke(EnemyResult.Fallback());
                return;
            }
            _lastEnemyTime = Time.time;

            string systemPrompt = @"你是末日生存RPG中敌方AI的战术大脑。根据敌人类型和战场状态，输出战术决策。
输出严格 JSON:
{""tactic"":""patrol|chase|ambush|retreat|call_reinforce|flank|hold_position|kamikaze"",""attackNow"":true/false,""narration"":""战场叙述（中文，不超过50字）"",""aggressionBias"":-1~1,""reasoning"":""决策理由""}
规则:血量低于30%且非boss→撤退，附近有友军→夹击/呼叫支援，玩家可见且距离近→追击/攻击，boss永不撤退";

            string userMessage = BuildEnemyUserMessage(ctx);
            Enqueue(PRIORITY_BATTLE, systemPrompt, userMessage, json =>
                onResult?.Invoke(ParseEnemyResult(json)),
                _ => onResult?.Invoke(EnemyResult.Fallback()));
        }

        // ════════════════════════════════════════════════════════
        //  公开 API — 叙事事件
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// 请求 LLM 决定全局叙事事件（天气、敌袭、NPC迁移等）。
        /// </summary>
        public void RequestNarrativeEvent(Action<NarrativeResult> onResult)
        {
            if (!enableGhost)
            {
                onResult?.Invoke(NarrativeResult.Fallback());
                return;
            }
            if (Time.time < _lastNarrativeTime + narrativeCheckInterval)
            {
                onResult?.Invoke(NarrativeResult.Fallback());
                return;
            }
            _lastNarrativeTime = Time.time;

            string systemPrompt = @"你是末日生存RPG的叙事导演（Ghost AI）。根据当前游戏状态，决定是否触发全局叙事事件。
输出严格 JSON:
{""triggerEvent"":true/false,""eventType"":""weather_change|enemy_raid|npc_arrival|npc_betrayal|item_spawn|zone_unlock|none"",""eventName"":""事件名称"",""description"":""中文描述，不超过100字"",""severity"":0~1,""targetZone"":""目标区域或none"",""durationSeconds"":0~300}
规则:事件间隔>=60秒,severity>0.7的事件应稀有,事件应符合世界观(203X年AI统治世界)";

            string state = BuildGameState();
            Enqueue(PRIORITY_NARRATIVE, systemPrompt, state, json =>
                onResult?.Invoke(ParseNarrativeResult(json)),
                _ => onResult?.Invoke(NarrativeResult.Fallback()));
        }

        // ════════════════════════════════════════════════════════
        //  内部 — 调度队列
        // ════════════════════════════════════════════════════════

        private void Enqueue(int priority, string systemPrompt, string userMessage,
            Action<string> onSuccess, Action<string> onError)
        {
            if (!enableGhost || string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Ghost disabled");
                return;
            }

            _queue.Enqueue(new PendingRequest
            {
                priority = priority,
                systemPrompt = systemPrompt,
                userMessage = userMessage,
                onSuccess = onSuccess,
                onError = onError
            });

            if (_processCoroutine == null)
                _processCoroutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (true)
            {
                if (_queue.Count == 0)
                {
                    _processCoroutine = null;
                    yield break;
                }

                if (_requestsThisFrame >= maxRequestsPerFrame)
                {
                    yield return null;
                    continue;
                }

                if (Time.time - _lastSendTime < requestCooldown)
                {
                    yield return null;
                    continue;
                }

                var req = DequeueBest();
                _lastSendTime = Time.time;
                _requestsThisFrame++;

                if (verboseLogging)
                    Debug.Log($"[GhostManager] 发送请求 (优先={req.priority})");

                bool done = false;
                SendRequest(req.systemPrompt, req.userMessage,
                    content => { req.onSuccess?.Invoke(content); done = true; },
                    error => { req.onError?.Invoke(error); done = true; });

                yield return new WaitUntil(() => done);
            }
        }

        private PendingRequest DequeueBest()
        {
            var arr = _queue.ToArray();
            _queue.Clear();

            int best = 0;
            for (int i = 1; i < arr.Length; i++)
                if (arr[i].priority < arr[best].priority) best = i;

            for (int i = 0; i < arr.Length; i++)
                if (i != best) _queue.Enqueue(arr[i]);

            return arr[best];
        }

        // ════════════════════════════════════════════════════════
        //  内部 — Prompt 构建
        // ════════════════════════════════════════════════════════

        private string BuildNPCUserMessage(NPCContext ctx)
        {
            var state = BuildGameState();
            return $@"{state}

【NPC 信息】
名称: {ctx.npcName}
性格: {ctx.npcPersonality}
阵营: {ctx.npcFaction}
当前行为: {ctx.currentBehavior}
情感值: 恐惧={ctx.fear:F2} 信任={ctx.trust:F2} 愤怒={ctx.anger:F2}
记忆: {(ctx.memories?.Length > 0 ? string.Join("; ", ctx.memories) : "无")}

请输出此 NPC 的下一步行为和对话。";
        }

        private string BuildEnemyUserMessage(EnemyContext ctx)
        {
            var state = BuildGameState();
            return $@"{state}

【敌方信息】
类型: {ctx.enemyType}
战斗风格: {ctx.combatStyle}
血量: {ctx.healthRatio:P0}
距玩家: {ctx.distanceToPlayer:F1}m
玩家可见: {ctx.playerVisible}
附近友军: {ctx.nearbyAllies}个
学到的玩家战术: {(ctx.learnedTactics?.Length > 0 ? string.Join(", ", ctx.learnedTactics) : "无")}

请输出此敌人的战术决策。";
        }

        private string BuildGameState()
        {
            var ctx = GameContextBuilder.Instance;
            return ctx != null ? ctx.BuildFullContext() : "[游戏上下文不可用]";
        }

        // ════════════════════════════════════════════════════════
        //  内部 — JSON 解析
        // ════════════════════════════════════════════════════════

        private static string ExtractJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "{}";
            raw = raw.Trim();
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);
            return raw;
        }

        private NPCResult ParseNPCResult(string json)
        {
            try
            {
                var clean = ExtractJson(json);
                var r = JsonUtility.FromJson<NPCResult>(clean);
                if (string.IsNullOrEmpty(r.behavior)) return NPCResult.Fallback();
                return r;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GhostManager] NPC JSON 解析失败: {e.Message}");
                return NPCResult.Fallback();
            }
        }

        private EnemyResult ParseEnemyResult(string json)
        {
            try
            {
                var clean = ExtractJson(json);
                var r = JsonUtility.FromJson<EnemyResult>(clean);
                if (string.IsNullOrEmpty(r.tactic)) return EnemyResult.Fallback();
                return r;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GhostManager] Enemy JSON 解析失败: {e.Message}");
                return EnemyResult.Fallback();
            }
        }

        private NarrativeResult ParseNarrativeResult(string json)
        {
            try
            {
                var clean = ExtractJson(json);
                return JsonUtility.FromJson<NarrativeResult>(clean);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GhostManager] Narrative JSON 解析失败: {e.Message}");
                return NarrativeResult.Fallback();
            }
        }

        // ════════════════════════════════════════════════════════
        //  内部 — HTTP 协程 (原 LLMDriver)
        // ════════════════════════════════════════════════════════

        private IEnumerator CoSendRequest(string systemPrompt, string userMessage,
            Action<string> onResult, Action<string> onError)
        {
            yield return new WaitForSeconds(Cooldown());

            string key = enableCache ? ComputeCacheKey(systemPrompt, userMessage) : null;
            if (key != null && _responseCache.TryGetValue(key, out string cached))
            {
                if (verboseLogging)
                    Debug.Log("[GhostManager] LLM cache hit.");
                onResult?.Invoke(cached);
                yield break;
            }

            var messages = new List<ChatMessage>
            {
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user",   content = userMessage }
            };

            yield return StartCoroutine(SendAndHandle(messages, key, onResult, onError));
        }

        private IEnumerator CoSendRequestWithHistory(List<ChatMessage> history,
            string userMessage, Action<string> onResult, Action<string> onError)
        {
            yield return new WaitForSeconds(Cooldown());

            string key = enableCache ? ComputeCacheKey("history", userMessage) : null;
            if (key != null && _responseCache.TryGetValue(key, out string cached))
            {
                if (verboseLogging)
                    Debug.Log("[GhostManager] LLM cache hit (history).");
                onResult?.Invoke(cached);
                yield break;
            }

            var messages = new List<ChatMessage>(history)
            {
                new ChatMessage { role = "user", content = userMessage }
            };

            yield return StartCoroutine(SendAndHandle(messages, key, onResult, onError));
        }

        private IEnumerator SendAndHandle(List<ChatMessage> messages, string cacheKey,
            Action<string> onResult, Action<string> onError)
        {
            string body = BuildRequestBody(messages);
            var request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.timeout = requestTimeout;

            if (verboseLogging)
                Debug.Log($"[GhostManager] ▶ LLM request to {apiUrl}");

            yield return request.SendWebRequest();

            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorBody = request.downloadHandler?.text ?? "(empty)";
                    long responseCode = request.responseCode;
                    Debug.LogWarning($"[GhostManager] LLM HTTP {responseCode}: {request.error}\nRequest: {body}\nResponse: {errorBody}");
                    onError?.Invoke($"{request.error}\n{errorBody}");
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                if (verboseLogging)
                    Debug.Log($"[GhostManager] ◀ LLM raw response: {responseText}");

                var resp = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
                if (resp?.choices != null && resp.choices.Length > 0
                    && resp.choices[0].message != null)
                {
                    string content = resp.choices[0].message.content ?? "";
                    if (cacheKey != null && !string.IsNullOrEmpty(content))
                        _responseCache[cacheKey] = content;
                    onResult?.Invoke(content);
                }
                else
                {
                    onError?.Invoke("Empty LLM response");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GhostManager] Parse error: {e.Message}");
                onError?.Invoke(e.Message);
            }
            finally
            {
                request.Dispose();
            }
        }

        private float Cooldown()
        {
            float elapsed = Time.time - _lastSendTime;
            return elapsed < requestCooldown ? requestCooldown - elapsed : 0f;
        }

        private string ComputeCacheKey(string sys, string user)
        {
            return $"ghost_{sys.Length}_{user.Length}_{sys.GetHashCode()}_{user.GetHashCode()}";
        }

        private string BuildRequestBody(List<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":\"");
            sb.Append(EscapeJson(modelName));
            sb.Append("\",\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"role\":\"");
                sb.Append(EscapeJson(messages[i].role));
                sb.Append("\",\"content\":\"");
                sb.Append(EscapeJson(messages[i].content));
                sb.Append("\"}");
            }
            sb.Append("],\"max_tokens\":");
            sb.Append(maxTokens);
            sb.Append(",\"temperature\":");
            sb.Append(temperature.ToString("F2"));
            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        // ════════════════════════════════════════════════════════
        //  内部类
        // ════════════════════════════════════════════════════════

        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatCompletionResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public ChatMessage message;
        }

        [Serializable]
        private class ProfileResult
        {
            public string profile;
            public string playstyle;
            public string reasoning;
        }

        private sealed class PendingRequest
        {
            public int priority;
            public string systemPrompt;
            public string userMessage;
            public Action<string> onSuccess;
            public Action<string> onError;
        }
    }

    // ============================================================
    //  Ghost 数据结构 (公开)
    // ============================================================

    [Serializable]
    public class NPCContext
    {
        public string npcName;
        public string npcPersonality;
        public string npcFaction;
        public string currentBehavior;
        public float fear;
        public float trust;
        public float anger;
        public string[] memories;
    }

    [Serializable]
    public class NPCResult
    {
        public string behavior;
        public string dialog;
        public float fearChange;
        public float trustChange;
        public float angerChange;
        public string reasoning;

        public static NPCResult Fallback()
        {
            return new NPCResult
            {
                behavior = "idle", dialog = "…",
                fearChange = 0f, trustChange = 0f, angerChange = 0f, reasoning = "fallback"
            };
        }
    }

    [Serializable]
    public class EnemyContext
    {
        public string enemyType;
        public string combatStyle;
        public float healthRatio;
        public float distanceToPlayer;
        public bool playerVisible;
        public int nearbyAllies;
        public string[] learnedTactics;
    }

    [Serializable]
    public class EnemyResult
    {
        public string tactic;
        public bool attackNow;
        public string narration;
        public float aggressionBias;
        public string reasoning;

        public static EnemyResult Fallback()
        {
            return new EnemyResult
            {
                tactic = "patrol", attackNow = false, narration = "",
                aggressionBias = 0f, reasoning = "fallback"
            };
        }
    }

    [Serializable]
    public class NarrativeResult
    {
        public bool triggerEvent;
        public string eventType;
        public string eventName;
        public string description;
        public float severity;
        public string targetZone;
        public float durationSeconds;

        public static NarrativeResult Fallback()
        {
            return new NarrativeResult
            {
                triggerEvent = false, eventType = "none", eventName = "",
                description = "", severity = 0f, targetZone = "", durationSeconds = 0f,
            };
        }
    }
}
