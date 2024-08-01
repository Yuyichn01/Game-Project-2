using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Author: Yi Yu

Date:2024/4/27

Description:
The script that can make the camera follow one game object

Implementation steps:
1.attach this to camara
2.place the object to follow in the "look at" slot
3.the smoothness defines the following latency
4.Y offset for the distance between Y axis and camera
*/
public class CameraController : MonoBehaviour
{
    public Transform lookAt; // lookAt to follow

    public float smoothSpeed = 0.125f; // Smoothing speed

    public float yOffset; // Offset for the Y position to maintain

    public float fixedZPosition; //the fixed Y position of look at object

    // Variables to store the camera's last position and rotation
    private Vector3 lastPosition;

    private Quaternion lastRotation;

    // Variable to indicate if the camera is moving
    public bool isCameraMoving { get; private set; }

    void Start()
    {
        // Initialize the last position and rotation with the camera's current values
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        isCameraMoving = false;
    }

    void FixedUpdate()
    {
        if (lookAt != null)
        {
            // Define lookAt position with fixed Y using current camera Y
            float xPosition = lookAt.position.x;
            float yPosition = lookAt.position.y;
            float zPosition = lookAt.position.z;

            Vector3 lookAtPosition =
                new Vector3(xPosition,
                    yPosition + yOffset,
                    zPosition - fixedZPosition);

            // Smoothly interpolate between the camera's position and the lookAt's position
            Vector3 smoothedPosition =
                Vector3
                    .Lerp(transform.position,
                    lookAtPosition,
                    smoothSpeed * Time.deltaTime);

            // Apply the smoothed position to the camera
            transform.position = smoothedPosition;
        }
    }

    void Update()
    {
        // Check if the camera's position or rotation has changed
        if (
            transform.position != lastPosition ||
            transform.rotation != lastRotation
        )
        {
            isCameraMoving = true;

            // Update the last position and rotation
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
        else
        {
            isCameraMoving = false;
        }
    }

    // Optional: Method to get the current movement state
    public bool IsCameraMoving()
    {
        return isCameraMoving;
    }
}
