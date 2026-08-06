using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLMIntegration
{
    /// <summary>
    /// 游戏上下文构建器 — 收集当前游戏状态，构造成 LLM 可理解的文本
    /// 挂载到 Persistent GameManager 上
    /// </summary>
    public class GameContextBuilder : MonoBehaviour
    {
        public static GameContextBuilder Instance { get; private set; }

        /// <summary>
        /// 最近发生的事件记录（用于 LLM 理解历史）
        /// </summary>
        private readonly List<string> _recentEvents = new(capacity: 20);

        // ============================================================
        //  叙事上下文 — 由各 LLM 组件写入和读取
        // ============================================================

        [Header("叙事上下文")]
        [Range(0, 100)]
        public int aiControlLevel = 60;

        [Range(0, 5)]
        public int liberatedDistricts = 0;

        public bool safeHouseAvailable = true;

        [TextArea(1, 2)]
        public string currentObjective = "找到西区变电站的备用电源入口";

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

        // ============================================================
        //  公开接口
        // ============================================================

        /// <summary>
        /// 记录一个游戏事件
        /// </summary>
        public void RecordEvent(string eventDescription)
        {
            _recentEvents.Add($"[{Time.time:F0}s] {eventDescription}");
            if (_recentEvents.Count > 20)
                _recentEvents.RemoveAt(0);
        }

        /// <summary>
        /// 获取最近事件列表
        /// </summary>
        public List<string> GetRecentEvents() => _recentEvents;

        /// <summary>
        /// 构建完整的游戏状态文本（供 LLM 作为 user message）
        /// </summary>
        public string BuildFullContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 当前游戏状态 ===");

            // 玩家状态
            AppendPlayerState(sb);

            // 时间/天气
            AppendWorldState(sb);

            // 背包
            AppendInventory(sb);

            // 叙事上下文
            AppendNarrativeContext(sb);

            // 附近 NPC
            AppendNearbyNPCs(sb);

            // 附近敌人
            AppendNearbyEnemies(sb);

            // 最近事件
            AppendRecentEvents(sb);

            return sb.ToString();
        }

        /// <summary>
        /// 构建精简的 NPC 上下文（用于 NPC 对话生成）
        /// </summary>
        public string BuildNPCContext(string npcName, string npcPersonality)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"你是 NPC「{npcName}」，性格：{npcPersonality}");
            sb.AppendLine();

            AppendPlayerState(sb);
            AppendWorldState(sb);
            AppendRecentEvents(sb);

            return sb.ToString();
        }

        /// <summary>
        /// 构建战斗上下文（用于敌人 AI 决策）
        /// </summary>
        public string BuildBattleContext(
            string enemyName,
            int enemyHP, int enemyMaxHP,
            List<BattleUnitContext> allies,
            List<BattleUnitContext> enemies)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"你是「{enemyName}」，当前 HP={enemyHP}/{enemyMaxHP}");
            sb.AppendLine();

            sb.AppendLine("=== 友方 ===");
            foreach (var u in allies)
                sb.AppendLine($"- {u.name}: HP={u.hp}/{u.maxHP}, ATK={u.atk}");

            sb.AppendLine();
            sb.AppendLine("=== 敌方 ===");
            foreach (var u in enemies)
                sb.AppendLine($"- {u.name}: HP={u.hp}/{u.maxHP}, ATK={u.atk}");

            return sb.ToString();
        }

        // ============================================================
        //  内部实现
        // ============================================================

        private void AppendPlayerState(StringBuilder sb)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null)
            {
                sb.AppendLine("[玩家未找到]");
                return;
            }

            sb.AppendLine($"玩家位置: {pc.transform.position}");
            sb.AppendLine($"玩家血量: {pc.health}");
            sb.AppendLine($"疲劳度: {pc.Fatigue}");
            sb.AppendLine($"饱食度: {pc.Starvation}");
        }

        private void AppendWorldState(StringBuilder sb)
        {
            var sm = FindAnyObjectByType<StatusManager>();
            if (sm != null)
            {
                sb.AppendLine($"当前时间: {sm.timeScale:F2}");
            }

            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                sb.AppendLine($"天数: 第 {ui.DayIndex} 天");
            }
        }

        private void AppendNarrativeContext(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 叙事上下文 ===");
            sb.AppendLine($"AI 控制强度: {aiControlLevel}/100");
            sb.AppendLine($"已解放街区: {liberatedDistricts}/5");
            sb.AppendLine($"安全屋状态: {(safeHouseAvailable ? "可用" : "已暴露")}");
            if (!string.IsNullOrEmpty(currentObjective))
                sb.AppendLine($"当前目标: {currentObjective}");
        }

        private void AppendInventory(StringBuilder sb)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null || pc.Items == null || pc.Items.Count == 0) return;

            sb.Append("背包物品: ");
            var names = new List<string>();
            foreach (var item in pc.Items)
            {
                if (item != null)
                    names.Add(item.ItemName);
            }

            sb.AppendLine(string.Join(", ", names));
        }

        private void AppendNearbyNPCs(StringBuilder sb)
        {
            var npcs = FindObjectsByType<npcAI>(FindObjectsSortMode.None);
            sb.AppendLine($"场景中 NPC 数量: {npcs.Length}");
        }

        private void AppendNearbyEnemies(StringBuilder sb)
        {
            var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            if (enemies.Length == 0) return;

            sb.AppendLine("附近敌人:");
            foreach (var e in enemies)
            {
                if (e != null && e.gameObject.activeInHierarchy)
                    sb.AppendLine($"  - {e.gameObject.name} (HP: {e.CurrentHealth})");
            }
        }

        private void AppendRecentEvents(StringBuilder sb)
        {
            if (_recentEvents.Count == 0) return;

            sb.AppendLine();
            sb.AppendLine("=== 最近事件 ===");
            foreach (var evt in _recentEvents)
                sb.AppendLine(evt);
        }

        // ============================================================
        //  数据结构
        // ============================================================

        public struct BattleUnitContext
        {
            public string name;
            public int hp;
            public int maxHP;
            public int atk;
        }
    }
}
