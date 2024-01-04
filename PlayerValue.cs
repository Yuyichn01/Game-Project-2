using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* attach the script to Canvas
*/
public class PlayerValue : MonoBehaviour
{
    [Header("Character UI Section")]
    public Sprite CharacterMiniSprite;

    public Sprite CharacterDialogSprite;

    [Header("Item Section")]
    public List<Item> Items = new List<Item>();

    [Header("Player Section")]
    // set the player's health
    public int health = 0;

    public int Stamina;

    public int Starvation;

    // set the player's hunger
    public static int hunger = 0;

    public void Add(Item item)
    {
        Item tmpItem;
        tmpItem = item;
        Items.Add (tmpItem);
    }

    public void Remove(Item item)
    {
        Items.Remove (item);
    }
}
