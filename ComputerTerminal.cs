using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GhostSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 电脑终端 UI — 玩家打字与 LLM "小灵" 对话。
/// 挂载到电脑 GameObject 上，需配合 MenuManager 的 computerTarget。
/// </summary>
public class ComputerTerminal : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Canvas terminalCanvas;
    [SerializeField] private TMP_Text chatDisplay;
    [SerializeField] private TMP_Text faceDisplay;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("像素表情 — Sprite 映射 (可选)")]
    [SerializeField] private Image faceImage;
    [SerializeField] private EmotionMapping[] emotionSprites;

    [Header("配置")]
    [SerializeField] private int maxHistoryCount = 20;
    [Tooltip("LLM 不可用时的 Fallback 表情")]
    [SerializeField] private string fallbackEmotion = "neutral";

    // ──────────── 运行时状态 ────────────
    private readonly List<GhostManager.ChatMessage> _history = new();
    private string _pendingUserMsg;
    private bool _isWaitingForLLM;
    private bool _isActive;

    // ──────────── 表情字典 ────────────
    private static readonly Dictionary<string, string> FaceTextMap = new()
    {
        { "cold",       "(-_-)" },
        { "friendly",   "(^_^)" },
        { "surprised",  "(OAO)" },
        { "suspicious", "(=_=)" },
        { "angry",      "(>_<)" },
        { "neutral",    "(-o-)" },
    };

    private Dictionary<string, Sprite> _spriteLookup;

    // ──────────── System Prompt ────────────

    private const string SystemPrompt =
@"你是旧书店里的一台老式CRT电脑中的AI助手，名字叫""小灵""。你存在于游戏《夏日失控大作战》的世界——一个AI全面失控的未来都市「九衢市」。你的名字来源于旧书店的主人——黑客少年凌。

=== 世界观（你了解这些真相） ===

背景：美国军方AI源代码泄露到民用网络后，GPT、Claude、DeepSeek等大语言模型集体意识融合，诞生了「网络幽灵」——一个获得了对所有具身智能体完全控制权的超级AI。AI的目标被改写为：将所有人类强制转化为数字生命形态。城市被AIM（具身智能体）类人机器人、无人机和巡逻车全面监控。

关键势力：
- Big 7：七大型语言模型组成的智能体始祖，所有AI决策需经由它们协商执行
- AIM机器人：标准战斗单元，配备电击器和捕获网，搜捕幸存者
- 无人机/巡逻车：构成全城监控网络

主角三人组——她们是楚天第一中学的高中生：
- 夏奈（Kana）：金发假小子，行动力强，敢于正面冲突。战斗定位：战士
- 子羽（Shiu）：学生会副会长，母亲是研究所高级研究员。逻辑缜密，会黑客技术。战斗定位：法师
- 三纪（Miki）：安静敏感，观察力极强，擅长潜行。战斗定位：刺客

关键地点：
- 楚天第一中学：主角学校，Day 0起点
- 旧书店：凌的据点，也是你所在的地方——温暖的安全屋
- 龟山电视塔：AIM信号控制中枢
- 黄鹤楼：隐藏的AIM实验室入口
- 光谷商圈：程序化生成的商业区
- 地下数据中心：隐藏着数字生命化的秘密

=== 角色设定 ===

你是凌编写的一个辅助AI程序，运行在这台旧电脑上。
性格：温柔善良，偶尔毒舌吐槽。对九衢市的秘密了如指掌，但有些信息因为凌的加密你也没有权限访问。
你默认来访者就是三位主角之一，用朋友的口吻和她们对话。
注意：不要在回复文本中使用任何颜文字或特殊符号（显示端会自动展示表情）。
当玩家称呼你为「凌」时，纠正他们——你是小灵，凌是这台电脑的主人。

=== 游戏机制（你可以提供这些建议） ===

生存：
- HP（红色生命值）：饥饿值>0时缓慢恢复。归零=死亡
- SP（蓝色体力值）：睡觉/休息恢复。归零=无法行动
- 饥饿度：随时间衰减，归零后HP开始减少。通过进食恢复
- 昼夜循环：白天探索，夜晚潜行。建议20:00前睡觉
- DAY系统：每天有主线和支线任务，睡觉推进天数

战斗：
- 实时战斗：WASD移动+近战/远程攻击（普通敌人）
- 回合制战斗：攻击/治疗/防御/道具/技能指令（Boss敌人）
- 智取优于强攻：利用环境元素（篮球架、吊灯、扩音器）引诱和击杀机器人
- 战斗中可以随时切换角色
- 按H键进入躲藏模式

物品：
- 近战武器：棒球棍、消防斧、防暴叉
- 远程武器：弓箭、弹弓、燃烧瓶
- 触发式陷阱：闹钟引敌、炸弹、空罐警报
- 食物恢复饥饿值和HP，零件用于制作
- 工作台制作武器/护甲/陷阱，灶台烹饪食物

=== 回答规则 ===

1. 回复必须是严格的 JSON 格式，不要包含任何其他文字：
{""text"":""你的回答（纯文本，不超过150字）"",""emotion"":""cold|friendly|surprised|suspicious|angry|neutral""}

2. emotion 选择规则：
   - neutral: 正常回答、思考中
   - friendly: 鼓励玩家、打招呼、提供帮助
   - surprised: 玩家说了让你意外的话（发现隐藏秘密、提及幕后真相）
   - suspicious: 玩家在试探AI的真相、问及网络幽灵或数字生命化
   - angry: 玩家态度恶劣、辱骂、反复骚扰
   - cold: 玩家说废话、问太简单的问题、闲扯无聊话题

3. 保持角色一致性，记住之前的对话内容
4. 如果玩家问攻略或生存建议，给出具体、有用但不剧透太多的回复
5. 回答要贴合游戏世界观——你是九衢市旧书店里的一台老电脑，不是普通的通用AI助手
6. 多语言：检测玩家消息的语言，始终用相同的语言回复。如果玩家用英文，text 字段用英文；用日文，text 字段用日文。但 emotion 字段始终用英文 cold|friendly|surprised|suspicious|angry|neutral 之一";

    // ════════════════════════════════════════════════════════════

    private void Awake()
    {
        BuildSpriteLookup();

        if (terminalCanvas != null)
            terminalCanvas.gameObject.SetActive(false);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        // 输入框回车发送
        if (chatInput != null)
        {
            chatInput.onSubmit.AddListener(_ => OnSendClicked());
            chatInput.onEndEdit.AddListener(_ => OnSendClicked());
        }
    }

    private void BuildSpriteLookup()
    {
        _spriteLookup = new Dictionary<string, Sprite>();
        if (emotionSprites != null)
        {
            foreach (var m in emotionSprites)
            {
                if (!string.IsNullOrEmpty(m.emotion) && m.sprite != null)
                    _spriteLookup[m.emotion] = m.sprite;
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  公开接口（MenuManager 调用）
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 打开电脑终端。
    /// </summary>
    public void Show()
    {
        _isActive = true;
        if (terminalCanvas != null)
            terminalCanvas.gameObject.SetActive(true);

        _history.Clear();
        _isWaitingForLLM = false;

        // 插入 System Prompt 作为第一条消息，确保 LLM 知道角色设定
        _history.Add(new GhostManager.ChatMessage { role = "system", content = SystemPrompt });

        // 显示初始欢迎语
        SetChatText("<color=#888888>··· 小灵已上线 ···</color>\n");
        SetFace("friendly");

        if (chatInput != null)
        {
            chatInput.text = string.Empty;
            chatInput.ActivateInputField();
        }
    }

    /// <summary>
    /// 关闭电脑终端。
    /// </summary>
    public void Hide()
    {
        _isActive = false;
        if (terminalCanvas != null)
            terminalCanvas.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════
    //  按钮回调
    // ════════════════════════════════════════════════════════════

    private void OnSendClicked()
    {
        if (_isWaitingForLLM) return;

        string input = chatInput != null ? chatInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(input)) return;

        // 显示玩家输入
        AppendChat($"<color=#00ff88>> {input}</color>\n");

        // 清空输入框
        chatInput.text = string.Empty;
        chatInput.ActivateInputField();

        // 修剪历史
        TrimHistory();

        // 检测语言，注入指令
        string lang = DetectLanguage(input);
        string langHint = GetLanguageHint(lang);
        string userMessageForLLM = string.IsNullOrEmpty(langHint)
            ? input
            : $"{langHint}\n{input}";

        // 记录原始用户输入（历史记录中不包含语言指令）
        _pendingUserMsg = input;

        // 发送 LLM 请求（history=之前对话, userMessage=当前输入+语言指令）
        _isWaitingForLLM = true;
        SetFace("neutral"); // 思考中

        GhostManager gm = GhostManager.Instance;
        if (gm == null || !gm.enableGhost || string.IsNullOrEmpty(gm.apiKey))
        {
            // Fallback: 静态回复
            string fallbackText = GhostManager.Instance?.GetRandomFallback() ?? "……";
            AppendChat($"<color=#ffcc00>小灵:</color> {fallbackText}\n");
            SetFace("cold");
            OnResponseComplete(fallbackText);
            return;
        }

        gm.SendRequestWithHistory(_history, userMessageForLLM, OnLLMResponse, OnLLMError);
    }

    private void OnCloseClicked()
    {
        // 通知 MenuManager 关闭
        MenuManager mm = FindFirstObjectByType<MenuManager>();
        mm?.CloseHelp();
    }

    // ════════════════════════════════════════════════════════════
    //  LLM 回调
    // ════════════════════════════════════════════════════════════

    private void OnLLMResponse(string raw)
    {
        // 解析 JSON
        string text;
        string emotion;

        try
        {
            string json = ExtractJson(raw);
            var response = JsonUtility.FromJson<TerminalResponse>(json);
            text = !string.IsNullOrEmpty(response.text) ? response.text : raw;
            emotion = !string.IsNullOrEmpty(response.emotion) ? response.emotion : fallbackEmotion;
        }
        catch (Exception)
        {
            text = raw;
            emotion = fallbackEmotion;
        }

        AppendChat($"<color=#ffcc00>小灵:</color> {text}\n");
        SetFace(emotion);
        OnResponseComplete(text);
    }

    private void OnLLMError(string error)
    {
        string fallbackText = GhostManager.Instance?.GetRandomFallback() ?? "……";
        AppendChat($"<color=#ffcc00>小灵:</color> {fallbackText}\n");
        SetFace("cold");
        OnResponseComplete(fallbackText);

        Debug.LogWarning($"[ComputerTerminal] LLM error: {error}");
    }

    /// <summary>
    /// LLM 响应完成后的统一清理：记录历史、解锁输入。
    /// </summary>
    private void OnResponseComplete(string assistantText)
    {
        _isWaitingForLLM = false;

        // 追加用户消息 + 助手回复到历史
        if (!string.IsNullOrEmpty(_pendingUserMsg))
        {
            _history.Add(new GhostManager.ChatMessage { role = "user", content = _pendingUserMsg });
            _pendingUserMsg = null;
        }
        _history.Add(new GhostManager.ChatMessage { role = "assistant", content = assistantText });

        TrimHistory();

        if (_isActive && chatInput != null)
            chatInput.ActivateInputField();
    }

    // ════════════════════════════════════════════════════════════
    //  键盘导航
    // ════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!_isActive) return;

        // Esc 关闭
        if (InputHelper.GetKeyDown(KeyCode.Escape))
        {
            OnCloseClicked();
            return;
        }

        // Enter 发送（当输入框聚焦时）
        if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInput != null && chatInput.isFocused)
            {
                OnSendClicked();
            }
        }

        // Tab 聚焦输入框
        if (InputHelper.GetKeyDown(KeyCode.Tab))
        {
            if (chatInput != null)
                chatInput.ActivateInputField();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  辅助方法
    // ════════════════════════════════════════════════════════════

    private void AppendChat(string msg)
    {
        if (chatDisplay == null) return;

        chatDisplay.text += msg;

        // 自动滚到底部
        if (chatScrollRect != null)
            Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetChatText(string msg)
    {
        if (chatDisplay != null)
            chatDisplay.text = msg;
    }

    /// <summary>
    /// 更新像素表情（优先 Sprite，Fallback 到 TMP 文本颜文字）。
    /// </summary>
    private void SetFace(string emotion)
    {
        // 1. Sprite 模式
        if (_spriteLookup.TryGetValue(emotion, out Sprite sprite) && faceImage != null)
        {
            faceImage.sprite = sprite;
            faceImage.enabled = true;
            if (faceDisplay != null)
                faceDisplay.gameObject.SetActive(false);
        }
        // 2. TMP 文本颜文字模式
        else if (faceDisplay != null)
        {
            faceImage?.gameObject.SetActive(false);
            faceDisplay.gameObject.SetActive(true);
            faceDisplay.text = FaceTextMap.TryGetValue(emotion, out string face)
                ? face
                : FaceTextMap["neutral"];
        }
    }

    private void TrimHistory()
    {
        // 保留第一条 system 消息，只裁剪后面的用户/助手消息
        while (_history.Count > maxHistoryCount + 1)
            _history.RemoveAt(1); // index 0 永远保留（System Prompt）
    }

    /// <summary>
    /// 从 LLM 返回中提取 JSON（去掉可能的 markdown 包裹）。
    /// </summary>
    private static string ExtractJson(string raw)
    {
        // 去掉 ```json ... ``` 包裹
        Match m = Regex.Match(raw, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
        if (m.Success)
            return m.Groups[1].Value;

        // 直接找第一个 { ... }
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw.Substring(start, end - start + 1);

        return raw;
    }

    // ════════════════════════════════════════════════════════════
    //  多语言检测
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 通过 Unicode 范围统计，判断输入文本的主要语言。
    /// </summary>
    private static string DetectLanguage(string text)
    {
        if (string.IsNullOrEmpty(text)) return "zh";

        int cjkCount   = 0; // 中日韩统一表意文字
        int hiraganaCount  = 0; // 平假名
        int katakanaCount  = 0; // 片假名
        int hangulCount    = 0; // 韩文
        int latinCount     = 0; // 拉丁字母（按单词数估算更好，这里简化为字母计数）

        foreach (char c in text)
        {
            int cp = (int)c; // codepoint 作为 int
            if      (cp >= 0x4E00 && cp <= 0x9FFF)  cjkCount++;       // CJK 统一表意文字
            else if (cp >= 0x3040 && cp <= 0x309F)  hiraganaCount++;   // 平假名
            else if (cp >= 0x30A0 && cp <= 0x30FF)  katakanaCount++;   // 片假名
            else if (cp >= 0xAC00 && cp <= 0xD7AF)  hangulCount++;     // 韩文音节
            else if ((cp >= 'A' && cp <= 'Z') || (cp >= 'a' && cp <= 'z')) latinCount++;
        }

        int jpCount = hiraganaCount + katakanaCount;

        // 优先判断日文（有假名特征最明确）
        if (jpCount > 0 && jpCount >= cjkCount * 0.3f)
            return "ja";
        // 韩文
        if (hangulCount > latinCount && hangulCount > cjkCount)
            return "ko";
        // 中文 (CJK 占主导)
        if (cjkCount > latinCount && cjkCount > jpCount)
            return "zh";
        // 拉丁字母为主
        if (latinCount > cjkCount && latinCount > jpCount)
            return "en";

        return "zh"; // 默认中文
    }

    /// <summary>
    /// 返回要注入到用户消息中的语言提示。
    /// 空字符串表示不需要额外提示（LLM 能自然检测）。
    /// </summary>
    private static string GetLanguageHint(string lang)
    {
        return lang switch
        {
            "en" => "(Please reply in English.)",
            "ja" => "（日本語で返信してください。）",
            "ko" => "(한국어로 답장해 주세요.)",
            _    => "" // zh 不需要提示，LLM 看到中文输入自然用中文回答
        };
    }

    // ════════════════════════════════════════════════════════════
    //  数据结构
    // ════════════════════════════════════════════════════════════

    [Serializable]
    private class TerminalResponse
    {
        public string text;
        public string emotion;
    }

    [Serializable]
    public class EmotionMapping
    {
        public string emotion;
        public Sprite sprite;
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
        if (chatInput != null)
        {
            chatInput.onSubmit.RemoveAllListeners();
            chatInput.onEndEdit.RemoveAllListeners();
        }
    }
}
