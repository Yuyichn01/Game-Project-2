using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // button variables
    public Button startButton;

    public int SceneTransitionIndex1;

    public Button settingButton;

    public int SceneTransitionIndex2;

    public Button exitButton;

    // the menu sprite
    public Image menuSprite;

    // the background image
    public Image background;

    public GameObject sceneTransition;

    [SerializeField]
    Animator transitionAnimator;

    void Start()
    {
        sceneTransition.SetActive(false);

        // when button is clicked, do action
        Button btn1 = startButton.GetComponent<Button>();
        btn1.onClick.AddListener(() => sceneTransition.SetActive(true));
        btn1
            .onClick
            .AddListener(() =>
                StartCoroutine(LoadLevel(SceneTransitionIndex1)));

        Button btn2 = settingButton.GetComponent<Button>();
        btn2.onClick.AddListener(() => sceneTransition.SetActive(true));
        btn2
            .onClick
            .AddListener(() =>
                StartCoroutine(LoadLevel(SceneTransitionIndex2)));

        Button btn3 = exitButton.GetComponent<Button>();
        btn3.onClick.AddListener(() => Application.Quit());
    }

    IEnumerator LoadLevel(int num)
    {
        transitionAnimator.SetTrigger("start");
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene (num);
    }
}
