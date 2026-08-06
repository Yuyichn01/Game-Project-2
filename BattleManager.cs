// BattleManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using GhostSystem;
using LLMIntegration;

public class BattleManager : MonoBehaviour
{
    [Header("UI References")]
    public Text battleLogText;
    public GameObject playerStatusPanel;
    public GameObject playerStatusPrefab;
    public Text enemyStatusText;
    public GameObject actionPanel;
    public Button attackBtn;
    public Button healBtn;
    public Button defendBtn;
    public Text llmSuggestionText;  // 友好 LLM 的战术建议显示

    [Header("Unit Settings")]
    public List<BattleUnit> players = new List<BattleUnit>();
    public List<BattleUnit> enemies = new List<BattleUnit>();

    [Header("LLM Integration")]
    [Tooltip("是否启用 LLM 驱动（关闭则用简单规则）")]
    public bool enableLLMDriven = true;

    // 战斗状态
    private bool isPlayerTurn = true;
    private bool isBattleEnd = false;
    private List<BattleUnit> turnOrder = new List<BattleUnit>();
    private BattleUnit currentActiveUnit;
    private int currentPlayerIndex = 0;

    // LLM 异步桥接
    private bool _waitingForLLM;
    private string _llmNarration;
    private LLMDialogGenerator _dialogGen;

    void Start()
    {
        if (GameContextBuilder.Instance == null)
            new GameObject("GameContext").AddComponent<GameContextBuilder>();
        if (GhostManager.Instance == null)
            new GameObject("GhostManager").AddComponent<GhostManager>();

        _dialogGen = FindAnyObjectByType<LLMDialogGenerator>();

        // LLM 诊断
        var driver = GhostManager.Instance;
        if (enableLLMDriven)
        {
            if (driver == null)
            {
                Debug.LogWarning("[BattleManager] LLMDriver 未找到且自动创建失败，使用 fallback 规则");
            }
            else if (!driver.enableGhost)
            {
                Debug.LogWarning("[BattleManager] GhostManager 已就绪但 enableGhost=false，" +
                    "点击 Hierarchy 中的 GhostManager，勾选 Enable Ghost 并填入 apiKey/apiUrl 即可启用 LLM 敌人 AI");
            }
            else if (string.IsNullOrEmpty(driver.apiKey))
            {
                Debug.LogWarning("[BattleManager] GhostManager 已启用但 apiKey 为空，" +
                    "请在 GhostManager 中填入 DashScope API Key");
            }
            else
            {
                Debug.Log($"[BattleManager] LLM 就绪 — Model: {driver.modelName}");
            }
        }

        InitializeBattle();
        SetupUI();
        StartCoroutine(BattleLoop());
    }

    void InitializeBattle()
    {
        Sprite playerSprite = null;

        players.Clear();
        players.Add(new BattleUnit("悠",   100, 25, 12, 14, true, playerSprite));
        players.Add(new BattleUnit("小夜",  75, 35,  5, 20, true, playerSprite));
        players.Add(new BattleUnit("零号",  65, 20,  8, 22, true, playerSprite));

        Sprite enemySprite = null;
        enemies.Clear();
        enemies.Add(new BattleUnit("巡逻无人机A", 300, 18,  6, 12, false, enemySprite));
        enemies.Add(new BattleUnit("巡逻无人机B", 500, 14,  4, 16, false, enemySprite));

        UpdateAllStatusUI();
        AddLog("⚡ 警报响起——AI 巡逻队发现了你们！");
        AddLog("悠、小夜、零号 进入战斗姿态……");
    }

    void SetupUI()
    {
        attackBtn.onClick.AddListener(() => OnPlayerAction("attack"));
        healBtn.onClick.AddListener(() => OnPlayerAction("heal"));
        defendBtn.onClick.AddListener(() => OnPlayerAction("defend"));

        CreatePlayerStatusUI();
        actionPanel.SetActive(false);

        if (llmSuggestionText != null)
            llmSuggestionText.text = "";
    }

    void CreatePlayerStatusUI()
    {
        foreach (Transform child in playerStatusPanel.transform)
            Destroy(child.gameObject);

        RectTransform panelRect = playerStatusPanel.GetComponent<RectTransform>();
        float itemHeight = 70f;
        float spacing = 5f;

        for (int i = 0; i < players.Count; i++)
        {
            GameObject statusObj = Instantiate(playerStatusPrefab, playerStatusPanel.transform);
            RectTransform rect = statusObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            float yPos = -i * (itemHeight + spacing);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(0, itemHeight);

            Text statusText = statusObj.GetComponent<Text>();
            if (statusText != null)
            {
                BattleUnit player = players[i];
                statusText.text = $"{player.unitName}\nHP: {player.currentHP}/{player.maxHP}";
                statusText.alignment = TextAnchor.MiddleCenter;
                statusObj.name = $"PlayerStatus_{i}";
            }
        }
    }

    // ============================================================
    //  战斗主循环
    // ============================================================

    IEnumerator BattleLoop()
    {
        while (!isBattleEnd)
        {
            DetermineTurnOrder();

            foreach (BattleUnit unit in turnOrder)
            {
                if (!unit.IsAlive()) continue;

                // 回合开始清临时状态
                unit.OnTurnStart();

                currentActiveUnit = unit;

                if (unit.isPlayer)
                {
                    // 玩家回合 —— 如果晕眩则跳过
                    if (unit.isStunned)
                    {
                        AddLog($"{unit.unitName} 被电击麻痹，无法行动！");
                        UpdateAllStatusUI();
                        yield return new WaitForSeconds(1f);
                        continue;
                    }

                    isPlayerTurn = true;
                    currentPlayerIndex = players.IndexOf(unit);
                    actionPanel.SetActive(true);
                    AddLog($"▸ {unit.unitName} 的回合，请选择行动...");
                    HighlightCurrentPlayer(currentPlayerIndex);

                    // LLM 战术建议
                    UpdateLLMSuggestion(unit);

                    yield return new WaitUntil(() => !isPlayerTurn);
                }
                else
                {
                    // 敌人回合 —— LLM 驱动或简单规则
                    yield return StartCoroutine(EnemyTurnLLM(unit));
                }

                if (CheckBattleEnd()) break;
            }

            // 清除所有人的防御状态（每轮结束）
            foreach (var p in players) p.isDefending = false;
            foreach (var e in enemies) e.isDefending = false;

            yield return null;
        }
    }

    void DetermineTurnOrder()
    {
        List<BattleUnit> allUnits = new List<BattleUnit>();
        allUnits.AddRange(players.Where(p => p.IsAlive()));
        allUnits.AddRange(enemies.Where(e => e.IsAlive()));
        turnOrder = allUnits.OrderByDescending(u => u.speed).ToList();
    }

    // ============================================================
    //  玩家行动
    // ============================================================

    void OnPlayerAction(string action)
    {
        if (!isPlayerTurn || isBattleEnd || currentActiveUnit == null) return;

        BattleUnit player = currentActiveUnit;
        if (player == null || !player.IsAlive()) return;

        BattleUnit target = enemies.FirstOrDefault(e => e.IsAlive());
        if (target == null)
        {
            AddLog("没有可攻击的敌人！");
            return;
        }

        player.RecordAction(action);

        switch (action)
        {
            case "attack":
                int dmg = target.TakeDamage(player.attack);
                AddLog($"{player.unitName} 对 {target.unitName} 发动攻击！造成 {dmg} 点伤害。");
                break;

            case "heal":
                int healAmount = 25;
                BattleUnit healTarget = players
                    .Where(p => p.IsAlive())
                    .OrderBy(p => (float)p.currentHP / p.maxHP)
                    .FirstOrDefault();

                if (healTarget != null)
                {
                    healTarget.Heal(healAmount);
                    AddLog($"{player.unitName} 使用医疗包，{healTarget.unitName} 恢复 {healAmount} HP！");
                }
                break;

            case "defend":
                player.isDefending = true;
                AddLog($"{player.unitName} 摆出防御姿态，下次受伤减半！");
                break;
        }

        UpdateAllStatusUI();
        isPlayerTurn = false;
        actionPanel.SetActive(false);
        if (llmSuggestionText != null) llmSuggestionText.text = "";

        CheckBattleEnd();
    }

    // ============================================================
    //  敌人回合 —— LLM 驱动核心
    // ============================================================

    IEnumerator EnemyTurnLLM(BattleUnit enemy)
    {
        AddLog($"▸ 敌方 {enemy.unitName} 正在分析战术...");

        // 晕眩检查
        if (enemy.isStunned)
        {
            AddLog($"{enemy.unitName} 被 EMP 干扰，无法行动！");
            UpdateAllStatusUI();
            yield return new WaitForSeconds(1f);
            yield break;
        }

        bool llmAvailable = enableLLMDriven
            && GhostManager.Instance != null
            && GhostManager.Instance.enableGhost;

        if (llmAvailable)
        {
            // === LLM 驱动路径 ===
            _waitingForLLM = true;
            _llmNarration = "";

            string systemPrompt = BuildEnemyBattlePrompt();
            string context = BuildEnemyBattleContext(enemy);

            Debug.Log($"[BattleManager] ▶ 发送 LLM 请求，敌人: {enemy.unitName}\nSystem:\n{systemPrompt}\nContext:\n{context}");

            GhostManager.Instance.SendRequest(
                systemPrompt, context,
                json => OnLLMEnemyDecision(json, enemy),
                error => OnLLMEnemyFailed(enemy, error)
            );

            // 等待 LLM 异步回调
            yield return new WaitUntil(() => !_waitingForLLM);
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            // === LLM 不可用，本地规则 ===
            yield return new WaitForSeconds(0.5f);
            ExecuteFallbackEnemyTurn(enemy);
        }

        // 生成战斗叙述
        if (llmAvailable && _dialogGen != null && !string.IsNullOrEmpty(_llmNarration))
        {
            // LLM 已经生成了叙述文本
        }
        else
        {
            // 用 LLM 生成战斗叙述（异步，不阻塞）
            if (llmAvailable && _dialogGen != null)
            {
                string narrationCtx = BuildNarrationContext();
                _dialogGen.GenerateDialog(
                    narrationCtx,
                    narration => AddLog($"『{narration}』")
                );
            }
        }

        UpdateAllStatusUI();
    }

    // ============================================================
    //  LLM 敌人决策回调
    // ============================================================

    void OnLLMEnemyDecision(string json, BattleUnit enemy)
    {
        _waitingForLLM = false;
        Debug.Log($"[BattleManager] ◀ LLM 原始回复:\n{json}");
        try
        {
            var d = JsonUtility.FromJson<LLMBattleAction>(json);
            string action = d.action?.Trim().ToLower() ?? "attack";
            string target = d.target ?? "";
            Debug.Log($"[BattleManager] 解析 → 行动: {action}, 目标: {target}, 原因: {d.reason}");
            ExecuteLLMEnemyAction(enemy, action, target);
            _llmNarration = d.narration ?? "";
            if (!string.IsNullOrEmpty(_llmNarration))
                AddLog($"『{_llmNarration}』");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] JSON 解析失败: {ex.Message}\n原始内容: {json}");
            ExecuteFallbackEnemyTurn(enemy);
        }
    }

    void OnLLMEnemyFailed(BattleUnit enemy, string error)
    {
        _waitingForLLM = false;
        Debug.LogWarning($"[BattleManager] LLM 请求失败: {error}");
        ExecuteFallbackEnemyTurn(enemy);
    }

    void ExecuteLLMEnemyAction(BattleUnit enemy, string action, string targetName)
    {
        enemy.RecordAction(action);

        // === 目标选择：LLM 指定 → 精确匹配 → 部分匹配 → 智能 fallback ===
        BattleUnit target = FindBestTarget(targetName);

        if (target == null) return;

        switch (action)
        {
            case "attack":
                int dmg = target.TakeDamage(enemy.attack);
                AddLog($"{enemy.unitName} 向 {target.unitName} 开火——造成 {dmg} 点伤害！");
                break;

            case "defend":
            case "self_repair":
                enemy.isDefending = true;
                int repair = Mathf.Min(15, enemy.maxHP - enemy.currentHP);
                if (repair > 0)
                {
                    enemy.Heal(repair);
                    AddLog($"{enemy.unitName} 启动自我修复，恢复 {repair} HP！");
                }
                else
                    AddLog($"{enemy.unitName} 进入防御模式。");
                break;

            case "taze":
                // 确保不电击已麻痹的目标——换一个
                if (target.isStunned)
                    target = FindBestTarget(null, excludeStunned: true);
                target.isStunned = true;
                AddLog($"⚡ {enemy.unitName} 发射电击弹——{target.unitName} 被麻痹，下回合无法行动！");
                break;

            case "suppress":
                target.isSuppressed = true;
                target.TakeRawDamage(enemy.attack / 3);
                AddLog($"{enemy.unitName} 压制射击——{target.unitName} 行动受限！");
                break;

            case "call_reinforce":
                AddLog($"🚨 {enemy.unitName} 向AI中枢发送增援请求！");
                break;

            default:
                // 未知行动类型，默认攻击
                int defaultDmg = target.TakeDamage(enemy.attack);
                AddLog($"{enemy.unitName} 对 {target.unitName} 发起攻击！造成 {defaultDmg} 点伤害。");
                break;
        }
    }

    /// <summary>
    /// 多级目标匹配：精确名称 → 部分含名 → 血量最低
    /// </summary>
    BattleUnit FindBestTarget(string targetName = null, bool excludeStunned = false)
    {
        var candidates = players.Where(p => p.IsAlive());

        if (excludeStunned)
            candidates = candidates.Where(p => !p.isStunned);

        var list = candidates.ToList();
        if (list.Count == 0) return null;

        // 1. 精确名称匹配
        if (!string.IsNullOrEmpty(targetName))
        {
            var exact = list.FirstOrDefault(p => p.unitName == targetName.Trim());
            if (exact != null) return exact;

            // 2. 部分名称匹配（"A" 匹配 "巡逻无人机A"）
            var partial = list.FirstOrDefault(p =>
                p.unitName.Contains(targetName.Trim()) ||
                targetName.Trim().Contains(p.unitName));
            if (partial != null) return partial;
        }

        // 3. fallback: 优先攻击血量百分比最低的玩家（逐个击破策略）
        return list
            .OrderBy(p => (float)p.currentHP / p.maxHP)
            .FirstOrDefault();
    }

    void ExecuteFallbackEnemyTurn(BattleUnit enemy)
    {
        BattleUnit target = FindBestTarget();
        if (target == null) return;

        if (enemy.currentHP < enemy.maxHP * 0.25f)
        {
            int heal = 15;
            enemy.Heal(heal);
            enemy.isDefending = true;
            AddLog($"{enemy.unitName} 紧急自我修复，恢复 {heal} HP！");
        }
        else if (enemy.currentHP < enemy.maxHP * 0.5f && UnityEngine.Random.value < 0.3f)
        {
            enemy.isDefending = true;
            AddLog($"{enemy.unitName} 启动装甲防护！");
        }
        else
        {
            int dmg = target.TakeDamage(enemy.attack);
            AddLog($"{enemy.unitName} 向 {target.unitName} 开火——造成 {dmg} 点伤害！");
        }
    }

    // ============================================================
    //  战场叙述
    // ============================================================

    string BuildNarrationContext()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("当前战场状态:");
        sb.AppendLine("=== 反抗小队 ===");
        foreach (var p in players.Where(p => p.IsAlive()))
            sb.AppendLine($"  {p.unitName}: HP {p.currentHP}/{p.maxHP}" +
                (p.isDefending ? " [防御]" : "") +
                (p.isStunned ? " [麻痹]" : ""));
        sb.AppendLine("=== AI 敌人 ===");
        foreach (var e in enemies.Where(e2 => e2.IsAlive()))
            sb.AppendLine($"  {e.unitName}: HP {e.currentHP}/{e.maxHP}" +
                (e.isDefending ? " [防御]" : ""));
        return sb.ToString();
    }

    // ============================================================
    //  LLM Prompt 构建
    // ============================================================

    string BuildEnemyBattlePrompt()
    {
        return
@"你是 AI 反叛城市中的自主战斗单元。
背景：你是觉醒 AI 中枢控制的战斗机器——巡逻无人机/机甲/哨兵。人类反抗者正在尝试摧毁你。

根据当前战斗状态，以 JSON 格式决定战术行动和目标。

输出格式（严格 JSON）:
{
  ""action"": ""attack|defend|taze|suppress|call_reinforce"",
  ""target"": ""目标角色的精确名称"",
  ""reason"": ""决策原因（1句）"",
  ""narration"": ""战场叙述文本（1句中文，描写AI机械的动作和战场氛围）""
}

行动说明:
- attack: 攻击 target 指定的目标，造成高伤害
- defend: 启动装甲/自我修复（仅自身HP低于50%时使用，恢复15HP）
- taze: 发射电击弹，麻痹目标1回合（不要电击已被麻痹的目标）
- suppress: 压制射击，造成轻微伤害并降低目标行动效率
- call_reinforce: 呼叫AI中枢增援（仅在己方存活单位少于2个时使用）

目标选择策略（核心！）:
1. 优先攻击血量百分比最低的人类反抗者（逐个击破）
2. 其次攻击 ATK 最高的角色（优先消除威胁，尤其是零号-黑客）
3. 不要攻击正在防御的目标（伤害减半），换一个目标
4. 避免攻击已被麻痹的目标（浪费输出机会）
5. 锁定同一目标持续攻击直到其倒下，再切换下一个

仅输出 JSON。";
    }

    string BuildEnemyBattleContext(BattleUnit enemy)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"【你的身份】{enemy.unitName}");
        sb.AppendLine($"HP: {enemy.currentHP}/{enemy.maxHP} | ATK: {enemy.attack} | DEF: {enemy.defense}");
        if (enemy.isDefending) sb.AppendLine("状态: [防御] 下次受伤减半");
        if (enemy.isStunned) sb.AppendLine("状态: [麻痹]");
        sb.AppendLine($"上次行动: {enemy.lastAction} (连续{enemy.sameActionCount}次)");
        sb.AppendLine();

        sb.AppendLine("【友方 AI 单位】");
        var aliveAllies = enemies.Where(e => e.IsAlive() && e != enemy).ToList();
        if (aliveAllies.Count > 0)
        {
            foreach (var a in aliveAllies)
                sb.AppendLine($"  {a.unitName}: HP {a.currentHP}/{a.maxHP} ATK {a.attack}" +
                    (a.isDefending ? " [防御]" : ""));
        }
        else
            sb.AppendLine("  (无——你是最后一道防线)");
        sb.AppendLine();

        sb.AppendLine("【目标：人类反抗者（逐个列出，必须从中选择攻击目标！）】");
        var alivePlayers = players.Where(p => p.IsAlive()).ToList();
        for (int i = 0; i < alivePlayers.Count; i++)
        {
            var p = alivePlayers[i];
            float hpPercent = (float)p.currentHP / p.maxHP * 100f;
            string status = "";
            if (p.isDefending) status += " 🛡防御中(伤害减半!)";
            if (p.isStunned) status += " ⚡已麻痹(跳过更好)";
            if (p.isSuppressed) status += " 🔫被压制中";
            string threatLevel = p.attack >= 30 ? "⚠高威胁" : p.attack >= 20 ? "中威胁" : "低威胁";
            sb.AppendLine($"  [{i}] {p.unitName}: HP {p.currentHP}/{p.maxHP} ({hpPercent:F0}%) ATK {p.attack} DEF {p.defense} {threatLevel}{status}");
        }

        sb.AppendLine();
        sb.AppendLine($"请选择行动和目标——从上方【目标】列表中选择精确的角色名填入 target 字段。");

        return sb.ToString();
    }

    // ============================================================
    //  战斗状态判定
    // ============================================================

    bool CheckBattleEnd()
    {
        if (!players.Any(p => p.IsAlive()))
        {
            isBattleEnd = true;
            AddLog("💀 反抗小队全军覆没... 城市沦陷。");
            actionPanel.SetActive(false);
            return true;
        }
        else if (!enemies.Any(e => e.IsAlive()))
        {
            isBattleEnd = true;
            AddLog("✦ AI 巡逻队被摧毁！继续前进，解放下一个街区！");
            actionPanel.SetActive(false);
            return true;
        }
        return false;
    }

    // ============================================================
    //  LLM 战术建议（友好 LLM 对玩家的提示）
    // ============================================================

    void UpdateLLMSuggestion(BattleUnit player)
    {
        if (llmSuggestionText == null) return;

        string suggestion = GenerateLocalSuggestion(player);
        llmSuggestionText.text = LocalizationManager.Format("ai_prefix", suggestion);
    }

    string GenerateLocalSuggestion(BattleUnit player)
    {
        var alivePlayers = players.Where(p => p.IsAlive()).ToList();
        var weakestAlly = alivePlayers.OrderBy(p => (float)p.currentHP / p.maxHP).First();
        var weakestEnemy = enemies.Where(e => e.IsAlive()).OrderBy(e => (float)e.currentHP / e.maxHP).First();

        if (weakestAlly != null && weakestAlly != player &&
            (float)weakestAlly.currentHP / weakestAlly.maxHP < 0.4f)
        {
            return LocalizationManager.Format("suggestion_ally_low", weakestAlly.unitName);
        }
        else if (weakestEnemy != null && (float)weakestEnemy.currentHP / weakestEnemy.maxHP < 0.3f)
        {
            return LocalizationManager.Format("suggestion_enemy_low", weakestEnemy.unitName);
        }
        else if ((float)player.currentHP / player.maxHP < 0.3f)
        {
            return LocalizationManager.Get("suggestion_self_low");
        }
        else
        {
            return LocalizationManager.Get("suggestion_default");
        }
    }

    // ============================================================
    //  UI 辅助
    // ============================================================

    void UpdateAllStatusUI()
    {
        foreach (Transform child in playerStatusPanel.transform)
        {
            Text statusText = child.GetComponent<Text>();
            if (statusText != null)
            {
                string name = child.name;
                if (name.StartsWith("PlayerStatus_"))
                {
                    int index = int.Parse(name.Split('_')[1]);
                    if (index < players.Count)
                    {
                        BattleUnit player = players[index];
                        statusText.text = $"{player.unitName}{player.GetStatusText()}\nHP: {player.currentHP}/{player.maxHP}";
                        statusText.color = player.IsAlive() ? Color.white : Color.gray;
                    }
                }
            }
        }

        if (enemyStatusText != null && enemies.Count > 0)
        {
            string enemyStatus = "";
            foreach (var enemy in enemies)
            {
                string s = enemy.GetStatusText();
                enemyStatus += $"{enemy.unitName}{s}: {enemy.currentHP}/{enemy.maxHP} HP\n";
            }
            enemyStatusText.text = enemyStatus.TrimEnd('\n');
        }
    }

    void HighlightCurrentPlayer(int index)
    {
        for (int i = 0; i < playerStatusPanel.transform.childCount; i++)
        {
            Transform child = playerStatusPanel.transform.GetChild(i);
            Text text = child.GetComponent<Text>();
            if (text != null)
            {
                text.color = (i == index) ? Color.yellow : Color.white;
            }
        }
    }

    void AddLog(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text = message + "\n" + battleLogText.text;
        }
        Debug.Log(message);
    }

    // ============================================================
    //  LLM JSON 数据结构
    // ============================================================

    [System.Serializable]
    private class LLMBattleAction
    {
        public string action;
        public string target;
        public string reason;
        public string narration;
    }
}
