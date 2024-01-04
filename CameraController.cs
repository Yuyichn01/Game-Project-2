using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
attach this to camara
*/
public class CameraController : MonoBehaviour
{
    public Transform lookAt;

    public float Smoothness = 0.0f;

    private void LateUpdate()
    {
        Vector3 delta = Vector3.zero;

        float deltaX = lookAt.position.x - transform.position.x;
        if (deltaX > Smoothness || deltaX < -Smoothness)
        {
            if (transform.position.x < lookAt.position.x)
            {
                delta.x = deltaX - Smoothness;
            }
            else
            {
                delta.x = deltaX + Smoothness;
            }
        }

        float deltaY = lookAt.position.y - transform.position.y;
        if (deltaY > Smoothness || deltaY < -Smoothness)
        {
            if (transform.position.y < lookAt.position.y)
            {
                delta.y = deltaY - Smoothness;
            }
            else
            {
                delta.y = deltaY + Smoothness;
            }
        }

        transform.position += new Vector3(delta.x, deltaY, 0);
    }
}
