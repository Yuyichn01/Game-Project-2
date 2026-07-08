using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlot", menuName = "Dialogue/Plot")]
public class Plot : ScriptableObject
{
    [Header("Meta")]
    public string id;

    public string title;

    [Header("Lines (played in order)")]
    public List<PlotStep> lines = new List<PlotStep>();
}

[System.Serializable]
public class PlotStep
{
    [TextArea(2, 8)]
    public string sentence;

    public AudioClip sound;

    // Up to three portraits (left / right / extra)
    public Sprite portrait1;

    public Sprite portrait2;

    public Sprite portrait3;

    public List<CharacterAction> actions = new List<CharacterAction>();
}

public enum ActionType
{
    MoveTo,
    PlayAnimation,
    Wait,
    FaceTowards,
    PlaySFX,
    RePosition,
    FlipDirection
}

[System.Serializable]
public class CharacterAction
{
    [Header("Who")]
    public int characterId; // 运行时用此ID找到场景里的角色

    [Header("What")]
    public ActionType action;

    [Header("Params")]
    public Vector3 targetPosition; // MoveTo 参数

    public float moveSpeed = 2.5f;

    public string animParam; // PlayAnimation：Trigger/Bool/State 名

    public bool animBoolValue;

    public float waitSeconds; // Wait 参数

    public AudioClip sfx; // PlaySFX
}
