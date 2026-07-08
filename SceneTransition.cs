using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [SerializeField]
    Animator transitionAnimator;

    public GameObject gbToDestroy;

    void Start()
    {
        StartCoroutine(fadeIn());
    }

    public IEnumerator fadeIn()
    {
        yield return new WaitForSeconds(3);
        transitionAnimator.SetTrigger("end");
        gbToDestroy.SetActive(false);
    }
}
