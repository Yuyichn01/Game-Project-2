// BattleManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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

    [Header("Unit Settings")]
    public List<BattleUnit> players = new List<BattleUnit>();
    public List<BattleUnit> enemies = new List<BattleUnit>();

    private bool isPlayerTurn = true;
    private bool isBattleEnd = false;
    private List<BattleUnit> turnOrder = new List<BattleUnit>();
    private BattleUnit currentActiveUnit;
    private int currentPlayerIndex = 0;

    void Start()
    {
        InitializeBattle();
        SetupUI();
        StartCoroutine(BattleLoop());
    }

    void InitializeBattle()
    {
        // 创建3个玩家角色
        Sprite playerSprite = null;

        players.Clear();
        players.Add(new BattleUnit("战士", 120, 30, 15, 12, true, playerSprite));
        players.Add(new BattleUnit("法师", 80, 40, 5, 18, true, playerSprite));
        players.Add(new BattleUnit("牧师", 90, 15, 10, 14, true, playerSprite));

        // 创建敌人
        Sprite enemySprite = null;
        enemies.Clear();
        enemies.Add(new BattleUnit("史莱姆", 80, 18, 5, 10, false, enemySprite));
        enemies.Add(new BattleUnit("蝙蝠", 60, 15, 3, 16, false, enemySprite));

        UpdateAllStatusUI();
        AddLog("战斗开始！勇者们，准备战斗！");
    }

    void SetupUI()
    {
        attackBtn.onClick.AddListener(() => OnPlayerAction("attack"));
        healBtn.onClick.AddListener(() => OnPlayerAction("heal"));
        defendBtn.onClick.AddListener(() => OnPlayerAction("defend"));

        CreatePlayerStatusUI();
        actionPanel.SetActive(false);
    }

    void CreatePlayerStatusUI()
    {
        // 清空旧的UI
        foreach (Transform child in playerStatusPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // 获取面板的 RectTransform
        RectTransform panelRect = playerStatusPanel.GetComponent<RectTransform>();

        // 设置每个状态的高度和间距
        float itemHeight = 70f;
        float spacing = 5f;
        float totalHeight = players.Count * itemHeight + (players.Count - 1) * spacing;

        for (int i = 0; i < players.Count; i++)
        {
            GameObject statusObj = Instantiate(playerStatusPrefab, playerStatusPanel.transform);

            // 设置 RectTransform
            RectTransform rect = statusObj.GetComponent<RectTransform>();

            // 锚点设置为左上角
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);

            // 计算位置：从上到下排列
            float yPos = -i * (itemHeight + spacing);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(0, itemHeight);

            // 设置文本
            Text statusText = statusObj.GetComponent<Text>();
            if (statusText != null)
            {
                BattleUnit player = players[i];
                statusText.text = $"P{i + 1}. {player.unitName}\nHP: {player.currentHP}/{player.maxHP}";
                statusText.alignment = TextAnchor.MiddleCenter;
                statusObj.name = $"PlayerStatus_{i}";
            }
        }
    }

    IEnumerator BattleLoop()
    {
        while (!isBattleEnd)
        {
            DetermineTurnOrder();

            foreach (BattleUnit unit in turnOrder)
            {
                if (!unit.IsAlive()) continue;

                currentActiveUnit = unit;

                if (unit.isPlayer)
                {
                    isPlayerTurn = true;
                    currentPlayerIndex = players.IndexOf(unit);
                    actionPanel.SetActive(true);
                    AddLog($"{unit.unitName} 的回合，请选择行动...");
                    HighlightCurrentPlayer(currentPlayerIndex);

                    yield return new WaitUntil(() => !isPlayerTurn);
                }
                else
                {
                    yield return StartCoroutine(EnemyTurn(unit));
                }

                if (CheckBattleEnd()) break;
            }

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

        switch (action)
        {
            case "attack":
                AddLog($"{player.unitName} 对 {target.unitName} 发动攻击！");
                target.TakeDamage(player.attack);
                break;

            case "heal":
                int healAmount = 30;
                // 治疗血量百分比最低的队友（包括自己）
                BattleUnit healTarget = players
                    .Where(p => p.IsAlive())
                    .OrderBy(p => (float)p.currentHP / p.maxHP)
                    .FirstOrDefault();

                if (healTarget != null)
                {
                    healTarget.Heal(healAmount);
                    AddLog($"{player.unitName} 使用治疗，{healTarget.unitName} 恢复了 {healAmount} HP！");
                }
                break;

            case "defend":
                player.isDefending = true;
                AddLog($"{player.unitName} 进行防御，下次受伤减半！");
                break;
        }

        UpdateAllStatusUI();
        isPlayerTurn = false;
        actionPanel.SetActive(false);
        CheckBattleEnd();
    }

    IEnumerator EnemyTurn(BattleUnit enemy)
    {
        AddLog($"敌人 {enemy.unitName} 的回合...");
        yield return new WaitForSeconds(1.0f);

        if (enemy.IsAlive())
        {
            List<BattleUnit> alivePlayers = players.Where(p => p.IsAlive()).ToList();
            if (alivePlayers.Count == 0) yield break;

            BattleUnit target = alivePlayers[Random.Range(0, alivePlayers.Count)];

            // 敌人AI：70%攻击，30%治疗自己（如果血量低）
            int action = Random.Range(0, 100);
            if (action < 70)
            {
                AddLog($"{enemy.unitName} 对 {target.unitName} 发动攻击！");
                target.TakeDamage(enemy.attack);
            }
            else if (enemy.currentHP < enemy.maxHP * 0.3f)
            {
                int healAmount = 20;
                enemy.Heal(healAmount);
                AddLog($"{enemy.unitName} 使用了自我修复！");
            }
            else
            {
                AddLog($"{enemy.unitName} 在蓄力...（跳过回合）");
            }
        }

        UpdateAllStatusUI();
        yield return new WaitForSeconds(0.5f);
    }

    bool CheckBattleEnd()
    {
        if (!players.Any(p => p.IsAlive()))
        {
            isBattleEnd = true;
            AddLog("💀 全员阵亡... 游戏结束！");
            actionPanel.SetActive(false);
            return true;
        }
        else if (!enemies.Any(e => e.IsAlive()))
        {
            isBattleEnd = true;
            AddLog("🎉 胜利！所有敌人被击败了！");
            actionPanel.SetActive(false);
            return true;
        }
        return false;
    }

    void UpdateAllStatusUI()
    {
        // 更新玩家状态
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
                        string defending = player.isDefending ? " 🛡️" : "";
                        statusText.text = $"P{index + 1}. {player.unitName}{defending}\nHP: {player.currentHP}/{player.maxHP}";
                        statusText.color = player.IsAlive() ? Color.white : Color.gray;
                    }
                }
            }
        }

        // 更新敌人状态
        if (enemyStatusText != null && enemies.Count > 0)
        {
            string enemyStatus = "";
            foreach (var enemy in enemies)
            {
                enemyStatus += $"{enemy.unitName}: {enemy.currentHP}/{enemy.maxHP} HP\n";
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
}