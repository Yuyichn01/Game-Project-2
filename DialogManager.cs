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

    private float timeScaletmp;

    private bool withPortrait = true;

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
                    Debug.Log("Day1 dialog started");
                    withPortrait = false;
                    StartDialog(dialogs[DayIndex - 1]);
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

    //start to display the sentences and background image for dialog
    public void StartDialog(Dialog dialog)
    {
        timeScaletmp = StatusManager.GetComponent<StatusManager>().timeScale;

        //if portrait is allowed to display
        if (withPortrait)
        {
            //reset and display character portrait
            ResetDialogPortrait();
            UpdatePortrait();
        }

        //freeze character movement
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .FreezeCharacter();

        //start to display dialog animation
        DialogAnimator.SetBool("IsOpen", true);

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
        StatusManager.GetComponent<StatusManager>().timeScale = timeScaletmp;

        //pull out the Dailog window
        DialogAnimator.SetBool("IsOpen", false);
        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", false);
        }

        //reset and close dialog background
        StartCoroutine(CloseDialogBackground());

        //unfreeze character movement conditionally
        if (
            !UIManager.GetComponent<UIManager>().StoragePannelOpened &&
            !UIManager.GetComponent<UIManager>().CookerPannelOpened &&
            !UIManager.GetComponent<UIManager>().CraftingTableOpened
        )
        {
            UIManager
                .GetComponent<UIManager>()
                .CurrentCharacter
                .GetComponent<PlayerController>()
                .UnfreezeCharacter();
        }

        Debug.Log("End of conversation");

        //reset the withPortrait to default
        withPortrait = true;
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

    //Align portrait
    public void UpdatePortrait()
    {
        DialogPortrait[0].GetComponent<Image>().sprite =
            UIManager
                .GetComponent<UIManager>()
                .CurrentCharacter
                .GetComponent<PlayerController>()
                .CharacterDialogSprite;

        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", true);
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
}
