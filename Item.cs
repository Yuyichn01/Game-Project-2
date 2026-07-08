using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Author: Yi Yu

Date:2024/4/27

Description:
The script defines the item values
*/
[CreateAssetMenu(fileName = "New Item", menuName = "Item/Create New Item")]
public class Item : ScriptableObject
{
    //the Item type can be "food" "tool".....
    public string ItemType;

    //the Item name
    public string ItemName = " ";

    //the bonus point ranges from 0 to 100
    public int HealthBonus = 0;

    public int StaminaBonus = 0;

    public int StarvationBonus = 0;

    //the sprite to be displayed on Item UI
    public Sprite icon;

    //the dialog to be dislayed on dialog UI
    public Dialog dialog;

    //the object instance for player to pick or interact
    public GameObject objectInstance;

    //the projectile of the weapon
    public GameObject projectile;
}
