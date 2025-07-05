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

    /*[Header("Tilt Settings")]
    public float tiltAmount = 10f; // Maximum tilt angle in degrees

    public float smoothSpeedTilt = 5f; // Smoothing factor

    public float borderThreshold = 50f; // Distance from the screen border to trigger tilt

    private float targetTiltX = 0f;

    private float targetTiltY = 0f;*/
    void Start()
    {
        /*// Initialize the last position and rotation with the camera's current values
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        isCameraMoving = false;*/
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

    /*void Update()
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
        Vector3 mousePosition = Input.mousePosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate horizontal tilt (opposite of previous logic)
        if (mousePosition.x < borderThreshold)
            targetTiltY = -tiltAmount; // Look left
        else if (mousePosition.x > screenWidth - borderThreshold)
            targetTiltY = tiltAmount; // Look right
        else
            targetTiltY = 0f; // Reset

        // Calculate vertical tilt (opposite of previous logic)
        if (mousePosition.y < borderThreshold)
            targetTiltX = tiltAmount; // Look down
        else if (mousePosition.y > screenHeight - borderThreshold)
            targetTiltX = -tiltAmount; // Look up
        else
            targetTiltX = 0f; // Reset

        // Smoothly rotate the camera
        Quaternion targetRotation =
            Quaternion.Euler(targetTiltX, targetTiltY, 0f);
        transform.rotation =
            Quaternion
                .Lerp(transform.rotation,
                targetRotation,
                Time.deltaTime * smoothSpeedTilt);
    }

    // Optional: Method to get the current movement state
    public bool IsCameraMoving()
    {
        return isCameraMoving;
    }
   */
}
