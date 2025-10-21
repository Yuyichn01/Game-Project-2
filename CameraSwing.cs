using UnityEngine;

public class CameraSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingSpeed = 1f; // Speed of the swing

    public float verticalSwingAmount = 1f; // Maximum vertical swing distance

    public float horizontalSwingAmount = 1f; // Maximum horizontal swing distance

    private Vector3 initialPosition;

    private float swingTimer = 0f;

    private void Start()
    {
        // Store the initial position of the camera
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        // Calculate the swing offsets using sine waves
        float verticalOffset =
            Mathf.Sin(swingTimer * swingSpeed) * verticalSwingAmount;
        float horizontalOffset =
            Mathf.Cos(swingTimer * swingSpeed) * horizontalSwingAmount;

        // Apply the swing to the camera's local position
        transform.localPosition =
            initialPosition + new Vector3(horizontalOffset, verticalOffset, 0f);

        // Increment the swing timer
        swingTimer += Time.deltaTime;
    }
}
