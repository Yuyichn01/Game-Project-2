using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [Header("Managers section")]
    private GameObject UIManager;

    private GameObject InventoryManager;

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

    // Start is called before the first frame update
    void Start()
    {
        //Initialize managers
        UIManager = GameObject.FindWithTag("UIManager");
        InventoryManager = GameObject.FindWithTag("InventoryManager");

        //initialize sentences
        sentences = new Queue<string>();
    }

    public void StartDialog(Dialog dialog)
    {
        //reset Dialog status
        ResetDialogPortrait();

        UpdatePortrait();

        //freeze character movement
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .FreezeCharacter();

        DialogAnimator.SetBool("IsOpen", true);

        //start to display character sprite animator
        Debug.Log("Starting conversation");
        sentences.Clear();

        foreach (string sentence in dialog.sentences)
        {
            sentences.Enqueue (sentence);
        }
        DisplayNextSentence();
    }

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

    public void EndDialog()
    {
        DialogAnimator.SetBool("IsOpen", false);
        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", false);
        }

        //unfreeze character movement
        UIManager
            .GetComponent<UIManager>()
            .CurrentCharacter
            .GetComponent<PlayerController>()
            .UnfreezeCharacter();
        Debug.Log("End of conversation");
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogText.text = " ";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(TypingInterval);
        }
    }

    public void ResetDialogPortrait()
    {
        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Image>().sprite = TransparentTexture;
        }
    }

    public void UpdatePortrait()
    {
        DialogPortrait[0].GetComponent<Image>().sprite =
            UIManager
                .GetComponent<UIManager>()
                .CurrentCharacter
                .GetComponent<PlayerValue>()
                .CharacterDialogSprite;

        for (int i = 0; i < DialogPortrait.Length; i++)
        {
            DialogPortrait[i].GetComponent<Animator>().SetBool("IsOpen", true);
        }
    }
}
