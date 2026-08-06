// BattleUnit.cs
using System;
using UnityEngine;

[Serializable]
public class BattleUnit
{
    public string unitName;
    public int maxHP;
    public int currentHP;
    public int attack;
    public int defense;
    public int speed;
    public bool isPlayer;
    public Sprite unitSprite;

    // 状态标记
    public bool isDefending = false;
    public bool isStunned = false;       // LLM taze 导致的晕眩
    public bool isSuppressed = false;    // LLM suppress 导致的压制

    // 连续行动追踪（供 LLM 决策用）
    public string lastAction = "";
    public int sameActionCount = 0;

    public BattleUnit(string name, int hp, int atk, int def, int spd, bool player, Sprite sprite = null)
    {
        unitName = name;
        maxHP = hp;
        currentHP = hp;
        attack = atk;
        defense = def;
        speed = spd;
        isPlayer = player;
        unitSprite = sprite;
        isDefending = false;
        isStunned = false;
        isSuppressed = false;
        lastAction = "";
        sameActionCount = 0;
    }

    /// <summary>
    /// 受伤处理
    /// </summary>
    /// <param name="damage">原始伤害</param>
    /// <returns>实际造成的伤害值</returns>
    public int TakeDamage(int damage)
    {
        if (isDefending)
        {
            damage = damage / 2;
            isDefending = false;
        }

        int actualDamage = Mathf.Max(1, damage - defense / 2);
        currentHP = Mathf.Max(0, currentHP - actualDamage);
        Debug.Log($"{unitName} 受到 {actualDamage} 点伤害！剩余 HP: {currentHP}");
        return actualDamage;
    }

    /// <summary>
    /// 受到纯粹伤害（无视防御，用于环境伤害等）
    /// </summary>
    public int TakeRawDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log($"{unitName} 受到 {damage} 点固定伤害！剩余 HP: {currentHP}");
        return damage;
    }

    public void Heal(int amount)
    {
        int actualHeal = Mathf.Min(maxHP - currentHP, amount);
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"{unitName} 恢复了 {actualHeal} 点 HP！当前 HP: {currentHP}");
    }

    public bool IsAlive() => currentHP > 0;

    /// <summary>
    /// 回合开始时清除临时状态
    /// </summary>
    public void OnTurnStart()
    {
        // 晕眩持续到回合结束，这里清除
        if (isStunned)
        {
            isStunned = false;
            Debug.Log($"{unitName} 从晕眩中恢复。");
        }
    }

    /// <summary>
    /// 记录行动（用于追踪连续相同行动）
    /// </summary>
    public void RecordAction(string action)
    {
        if (action == lastAction)
            sameActionCount++;
        else
        {
            lastAction = action;
            sameActionCount = 1;
        }
    }

    /// <summary>
    /// 获取状态描述文本
    /// </summary>
    public string GetStatusText()
    {
        string status = "";
        if (isDefending) status += " 🛡️";
        if (isStunned) status += " ⚡";
        if (isSuppressed) status += " 🔒";
        return status;
    }
}
