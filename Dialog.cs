using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialog
{
    [TextArea(3, 10)]
    public string[] sentences;

    public Sprite dialogBackground;

    public Sprite Portrait;

    public AudioClip sound;

    public enum Mood
    {
        Normal,
        Happy,
        Sad
    }

    public Mood mood;
}
