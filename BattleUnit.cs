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
    public bool isDefending = false;

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
    }

    public void TakeDamage(int damage)
    {
        if (isDefending)
        {
            damage = damage / 2;
            isDefending = false;
        }

        int actualDamage = Mathf.Max(1, damage - defense / 2);
        currentHP = Mathf.Max(0, currentHP - actualDamage);
        Debug.Log($"{unitName} 受到 {actualDamage} 点伤害！剩余 HP: {currentHP}");
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"{unitName} 恢复了 {amount} 点 HP！当前 HP: {currentHP}");
    }

    public bool IsAlive() => currentHP > 0;
}