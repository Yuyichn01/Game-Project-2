using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
Author: Yi Yu

Date:2024/4/27

Description:
The script manages the dialog display

*/
public class DialogManager : MonoBehaviour
{
    [Header("Button section")]
    public Button ContinueButton;

    public Button ContinuePlotButton;

    [Header("Managers section")]
    private GameObject UIManager;

    private GameObject InventoryManager;

    private GameObject StatusManager;

    [Header("Text section")]
    private Queue<string> sentences;

    public TextMeshProUGUI dialogText;

    [Header("Portrait section")]
    public GameObject[] DialogPortrait;

    [Header("Animator section")]
    public Animator DialogAnimator;

    public float TypingInterval = 0.05f;

    [Header("Texture section")]
    public Sprite TransparentTexture;

    [Header("Background section")]
    public GameObject DialogBackground;

    [Header("Dialog section")]
    public Dialog[] dialogs;

    public Plot plot;

    private float timeScaletmp;

    private bool withPortrait = true;

    private Dialog currentDialog;

    private Plot currentPlot;

    // --- Plot playback state ---
    private AudioSource plotAudio;

    private bool plotIsTyping = false; // 是否正在逐字打字

    private bool plotRequestNext = false; // 是否请求进入下一段（按钮触发）

    private bool plotRequestFastForward = false; // 是否请求快进当前句（按钮在打字中按下）

    //reapeted stretch section (attach the ReaptedStretch script to dialogIndicator)
    public GameObject[] dialogComponents;

    private Coroutine currentMoveCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        //Initialize managers
        UIManager = GameObject.FindWithTag("UIManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");
        StatusManager = GameObject.FindWithTag("StatusManager");

        //initialize sentences
        sentences = new Queue<string>();

        //get the date
        int DayIndex = UIManager.GetComponent<UIManager>().DayIndex;

        //reset the dialog background
        ResetDialogBackground();

        //determine the dialog based on the date
        if (dialogs.Length != 0)
        {
            switch (DayIndex)
            {
                case 1:
                    UIManager.GetComponent<UIManager>().ResetDialogButtons();
                    ContinueButton.gameObject.SetActive(false);

                    Debug.Log("Day1 plot started");
                    StartPlot (plot);

                    break;
                case 2:
                    Debug.Log("Day2 dialog started");
                    break;
                case 3:
                    Debug.Log("Day3 dialog started");
                    break;
                case 4:
                    Debug.Log("Day4 dialog started");
                    break;
                case 5:
                    Debug.Log("Day5 dialog started");
                    break;
                case 6:
                    Debug.Log("Day6 dialog started");
                    break;
                case 7:
                    Debug.Log("Day7 dialog started");
                    break;
            }
        }
    }

    public void StartPlot(Plot plot)
    {
        //activate dialog indicator animation
        foreach (var incator in dialogComponents)
        {
            incator.SetActive(true);
        }

        if (plot == null || plot.lines == null || plot.lines.Count == 0)
        {
            Debug.LogWarning("StartPlot: plot is null or empty.");
            return;
        }

        Debug.Log("plot started");

        currentPlot = plot;

        if (withPortrait && DialogPortrait != null)
        {
            ResetDialogPortrait();
            for (int i = 0; i < DialogPortrait.Length; i++)
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", true);
        }

        DialogAnimator.SetBool("IsOpen", true);
        UIManager.GetComponent<UIManager>().dialogPannelOpened =
            DialogAnimator.GetBool("IsOpen");
        UIManager.GetComponent<UIManager>().PlayerFreezeState();

        // 音频源（如无则自动挂上）
        plotAudio = GetComponent<AudioSource>();
        if (!plotAudio) plotAudio = gameObject.AddComponent<AudioSource>();

        // 清理状态
        plotIsTyping = false;
        plotRequestNext = false;
        plotRequestFastForward = false;
        if (dialogText) dialogText.text = " ";

        StopAllCoroutines();
        StartCoroutine(CoPlayPlot(currentPlot));

        //屏蔽所有输入
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .disableInput();
    }

    private IEnumerator CoTypePlot(string content)
    {
        if (!dialogText) yield break;

        plotIsTyping = true;
        plotRequestFastForward = false;
        dialogText.text = "";

        if (TypingInterval <= 0f)
        {
            dialogText.text = content;
            plotIsTyping = false;
            yield break;
        }

        for (int idx = 0; idx < content.Length; idx++)
        {
            // 如果在打字中点了按钮：直接快进到全文
            if (plotRequestFastForward)
            {
                dialogText.text = content;
                plotRequestFastForward = false; // 消耗快进请求
                break;
            }

            dialogText.text += content[idx];
            yield return new WaitForSecondsRealtime(TypingInterval); // 不受 timeScale 影响
        }

        plotIsTyping = false;
    }

    private IEnumerator WaitForNextButton()
    {
        plotRequestNext = false;
        while (!plotRequestNext) yield return null;
        plotRequestNext = false; // 消耗一次
    }

    private IEnumerator CoPlayPlot(Plot plot)
    {
        int i = 0;
        while (i < plot.lines.Count)
        {
            var step = plot.lines[i];

            // 1) 立绘
            ApplyPlotPortraits (step);

            // 2) 音频
            float clipLen = 0f;
            if (plotAudio && step.sound)
            {
                plotAudio.Stop();
                plotAudio.clip = step.sound;
                plotAudio.Play();
                clipLen = step.sound.length;
            }

            foreach (var act in step.actions)
            {
                if (act == null) continue;

                // 取角色
                GameObject characterGo = null;
                if (act.characterId != -1)
                {
                    characterGo =
                        UIManager
                            .GetComponent<UIManager>()
                            .Characters[act.characterId];
                }
                switch (act.action)
                {
                    case ActionType.MoveTo:
                        {
                            if (
                                characterGo != null &&
                                characterGo.transform != null
                            )
                            {
                                // 启动新的移动协程
                                currentMoveCoroutine =
                                    StartCoroutine(MoveToPositionCoroutine(characterGo,
                                    act.targetPosition,
                                    act.moveSpeed));
                            }
                            break;
                        }
                    case ActionType.PlayAnimation:
                        {
                            if (characterGo)
                            {
                                var anim = characterGo.GetComponent<Animator>();
                                if (anim && !string.IsNullOrEmpty(act.animParam)
                                )
                                {
                                    var pType =
                                        GetAnimatorParamType(anim,
                                        act.animParam);
                                    switch (pType)
                                    {
                                        case AnimatorControllerParameterType
                                            .Trigger:
                                            anim.SetTrigger(act.animParam);
                                            break;
                                        case AnimatorControllerParameterType
                                            .Bool:
                                            anim
                                                .SetBool(act.animParam,
                                                act.animBoolValue);
                                            Debug.Log("character sleeping");
                                            break;
                                        case AnimatorControllerParameterType
                                            .Float:
                                            // 如需支持 Float，可把 animBoolValue 改成 float 字段
                                            break;
                                        case AnimatorControllerParameterType
                                            .Int:
                                            break;
                                        default:
                                            // 未在 Animator 中找到同名参数，尝试当作 Trigger
                                            anim.SetTrigger(act.animParam);
                                            break;
                                    }
                                }
                            }
                            break;
                        }
                    case ActionType.Wait:
                        {
                            var t = Mathf.Max(0f, act.waitSeconds);
                            if (t > 0f) yield return new WaitForSeconds(t);
                            break;
                        }
                    case ActionType.FaceTowards:
                        {
                            if (characterGo)
                            {
                                FaceTowards2D(characterGo.transform,
                                act.targetPosition);
                            }
                            break;
                        }
                    case ActionType.PlaySFX:
                        {
                            if (act.sfx)
                            {
                                var pos =
                                    characterGo
                                        ? characterGo.transform.position
                                        : Vector3.zero;
                                AudioSource.PlayClipAtPoint(act.sfx, pos);
                            }
                            break;
                        }
                    case ActionType.RePosition:
                        {
                            // 防止空引用
                            if (
                                characterGo != null &&
                                characterGo.transform != null
                            )
                            {
                                characterGo.transform.localPosition =
                                    act.targetPosition;
                            }
                            else
                            {
                                Debug.Log (characterGo);
                            }
                            break;
                        }
                    default:
                        // 未知 ActionType：忽略
                        break;
                    case ActionType.FlipDirection:
                        // 防止空引用
                        if (characterGo != null && characterGo.transform != null
                        )
                        {
                            characterGo.transform.localScale =
                                new Vector3(-Mathf
                                        .Abs(characterGo
                                            .transform
                                            .localScale
                                            .x),
                                    characterGo.transform.localScale.y,
                                    characterGo.transform.localScale.z);
                        }
                        break;
                }
            }

            // 逐字打字（允许按钮快进到全文）
            yield return StartCoroutine(CoTypePlot(step.sentence ?? ""));

            // 等待“继续”按钮（只有按钮能继续）
            yield return StartCoroutine(WaitForNextButton());

            i++;
        }

        // 结束收尾
        EndDialog();
    }

    // 读取 Animator 参数类型（无则返回 -1）
    private AnimatorControllerParameterType
    GetAnimatorParamType(Animator animator, string param)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == param) return p.type;
        }

        // 未找到：返回一个“未知”类型，用 default 分支处理
        return (AnimatorControllerParameterType)(-1);
    }

    // 2D 朝向：仅根据 X 方向翻转
    private void FaceTowards2D(Transform t, Vector3 target)
    {
        var dir = target - t.position;
        if (Mathf.Abs(dir.x) > 0.001f)
        {
            var s = t.localScale;
            s.x = Mathf.Sign(dir.x) * Mathf.Abs(s.x);
            t.localScale = s;
        }
    }

    public void OnPlotNextButton()
    {
        if (plotIsTyping)
            plotRequestFastForward = true; // 打字中：先快进到全文
        else
            plotRequestNext = true; // 打字结束后：进入下一段
    }

    private void ApplyPlotPortraits(PlotStep step)
    {
        if (DialogPortrait == null || DialogPortrait.Length == 0) return;

        if (DialogPortrait.Length >= 1)
            DialogPortrait[0].GetComponent<Image>().sprite =
                step.portrait1 ? step.portrait1 : TransparentTexture;

        if (DialogPortrait.Length >= 2)
            DialogPortrait[1].GetComponent<Image>().sprite =
                step.portrait2 ? step.portrait2 : TransparentTexture;

        if (DialogPortrait.Length >= 3)
            DialogPortrait[2].GetComponent<Image>().sprite =
                step.portrait3 ? step.portrait3 : TransparentTexture;
    }

    //start to display the sentences and background image for dialog
    public void StartDialog(Dialog dialog)
    {
        //activate dialog indicator animation
        foreach (var incator in dialogComponents)
        {
            incator.SetActive(true);
        }
        ContinueButton.gameObject.SetActive(true);
        ContinuePlotButton.gameObject.SetActive(false);

        //set the current dialog to this dialog
        currentDialog = dialog;

        //freeze character movement
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .FreezeCharacter();

        //if portrait is allowed to display
        if (withPortrait)
        {
            //reset character portrait
            ResetDialogPortrait();

            //update portrait
            switch (currentDialog.mood)
            {
                case Dialog.Mood.Happy:
                    DialogPortrait[0].GetComponent<Image>().sprite =
                        UIManager
                            .GetComponent<UIManager>()
                            .CurrentCharacter
                            .GetComponent<PlayerController>()
                            .CharacterDialogHappy;
                    break;
                case Dialog.Mood.Normal:
                    DialogPortrait[0].GetComponent<Image>().sprite =
                        UIManager
                            .GetComponent<UIManager>()
                            .CurrentCharacter
                            .GetComponent<PlayerController>()
                            .CharacterDialogNormal;
                    break;
            }

            if (currentDialog.Portrait != null)
            {
                DialogPortrait[1].GetComponent<Image>().sprite =
                    currentDialog.Portrait;
            }

            for (int i = 0; i < DialogPortrait.Length; i++)
            {
                DialogPortrait[i]
                    .GetComponent<Animator>()
                    .SetBool("IsOpen", true);
            }
        }

        //start to display dialog animation
        DialogAnimator.SetBool("IsOpen", true);
        UIManager.GetComponent<UIManager>().dialogPannelOpened =
            DialogAnimator.GetBool("IsOpen");
        UIManager.GetComponent<UIManager>().PlayerFreezeState();

        //start to set the dialog background
        if (dialog.dialogBackground != null)
        {
            //set the speed of time to zero
            StatusManager.GetComponent<StatusManager>().timeScale = 0;
            DialogBackground.SetActive(true);
            UpdateDialogBackground(dialog.dialogBackground);
        }

        //start to display character sprite animator
        Debug.Log("Starting conversation");
        sentences.Clear();

        foreach (string sentence in dialog.sentences)
        {
            sentences.Enqueue (sentence);
        }
        DisplayNextSentence();
    }

    //display the next sentence and background image
    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialog();
            return;
        }
        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    //end the conversation
    public void EndDialog()
    {
        // 停止之前的移动
        if (currentMoveCoroutine != null)
        {
            StopCoroutine (currentMoveCoroutine);
        }

        //reset the time scale to original
        if (currentDialog != null)
        {
            if (currentDialog.dialogBackground != null)
            {
                StatusManager.GetComponent<StatusManager>().timeScale =
                    timeScaletmp;

                //reset and close dialog background
                StartCoroutine(CloseDialogBackground());
            }
        }

        //pull out the Dialog window
        DialogAnimator.SetBool("IsOpen", false);
        UIManager.GetComponent<UIManager>().dialogPannelOpened =
            DialogAnimator.GetBool("IsOpen");
        UIManager.GetComponent<UIManager>().PlayerFreezeState();
        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", false);
        }

        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .UnfreezeCharacter();

        Debug.Log("End of conversation");

        //reset the withPortrait to default
        withPortrait = true;
        ContinueButton.gameObject.SetActive(false);
        ContinuePlotButton.gameObject.SetActive(false);

        //开放所有输入
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .enableInput();

        //activate dialog indicator animation
        foreach (var incator in dialogComponents)
        {
            incator.SetActive(false);
        }
    }

    //display the sentence character by character
    IEnumerator TypeSentence(string sentence)
    {
        dialogText.text = " ";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(TypingInterval);
        }
    }

    // reset all dialog portrait to default
    public void ResetDialogPortrait()
    {
        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Image>().sprite = TransparentTexture;
        }
    }

    public void ResetDialogBackground()
    {
        DialogBackground.GetComponent<Image>().sprite = TransparentTexture;
        DialogBackground.SetActive(false);
    }

    IEnumerator CloseDialogBackground()
    {
        DialogBackground.GetComponent<Animator>().SetBool("start", true);
        yield return new WaitForSeconds(2.0f);
        DialogBackground.SetActive(false);
    }

    public void UpdateDialogBackground(Sprite sprite)
    {
        DialogBackground.SetActive(true);
        DialogBackground.GetComponent<Image>().sprite = sprite;
    }

    private IEnumerator
    MoveToPositionCoroutine(
        GameObject characterGo,
        Vector3 targetPos,
        float speed
    )
    {
        var anim = characterGo.GetComponent<Animator>();
        Transform transform = characterGo.transform;
        Vector3 startPos = transform.localPosition;
        float journeyLength = Vector3.Distance(startPos, targetPos);
        float startTime = Time.time;

        while (Vector3.Distance(transform.localPosition, targetPos) > 0.1f)
        {
            float distanceCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distanceCovered / journeyLength;

            transform.localPosition =
                Vector3.Lerp(startPos, targetPos, fractionOfJourney);

            anim.SetFloat("speed", speed);

            yield return null;
        }

        // 确保最终位置准确
        transform.localPosition = targetPos;
        currentMoveCoroutine = null;

        // 可以在这里触发移动完成事件
    }
}
