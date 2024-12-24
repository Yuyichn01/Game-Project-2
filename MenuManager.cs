using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // button variables
    public Button startButton;

    public int TransitionTo1;

    public Button loadButton;

    public int TransitionTo2;

    public Button exitButton;

    // CanvasGroup for fade effect
    public CanvasGroup canvasGroup;

    public float fadeDuration = 1f; // Duration of fade in/out

    private GameObject DataManager;

    [Header("Manager section")]
    public List<AudioClip> bgmClips; // List of BGM clips

    public AudioSource BGMPlayer;

    void Start()
    {
        // Ensure the AudioSource is set to loop
        BGMPlayer.loop = false;

        // Start playing music
        PlayRandomTrack();

        DataManager = GameObject.FindWithTag("DataManager");

        // when button is clicked, do action
        Button btn1 = startButton.GetComponent<Button>();

        btn1
            .onClick
            .AddListener(() =>
            {
                DataManager.GetComponent<DataManager>().CreateNewGameData(10);
                StartCoroutine(LoadLevel(TransitionTo1));
            });

        Button btn2 = loadButton.GetComponent<Button>();

        btn2
            .onClick
            .AddListener(() =>
            {
                StartCoroutine(LoadLevel(TransitionTo2));
            });

        Button btn3 = exitButton.GetComponent<Button>();
        btn3.onClick.AddListener(() => Application.Quit());
    }

    IEnumerator LoadLevel(int num)
    {
        //slowly fade out scene
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = 1 - (timer / fadeDuration); // Gradually make transparent
            yield return null;
        }

        canvasGroup.alpha = 0;

        //load game data
        DataManager.GetComponent<DataManager>().LoadGameData();
        SceneManager.LoadScene (num);
    }

    void PlayRandomTrack()
    {
        if (bgmClips.Count == 0) return;

        // Choose a random clip from the list
        int randomIndex = Random.Range(0, bgmClips.Count);
        BGMPlayer.clip = bgmClips[randomIndex];
        BGMPlayer.Play();
    }
}
