using UnityEngine;

public class AutoMove : MonoBehaviour
{
    // Speed of movement
    public float speed = 2.0f;

    // Distance to travel before changing direction
    public float moveDistance = 5.0f;

    // Initial direction (-1 for left, 1 for right)
    private int direction = -1;

    // Starting position
    private Vector3 startPosition;

    void Start()
    {
        // Store the starting position
        startPosition = transform.position;
    }

    void Update()
    {
        // Calculate the direction vector based on the object's rotation
        Vector3 directionVector = transform.right * direction;

        // Calculate the new position based on speed and direction
        Vector3 newPosition =
            transform.position + directionVector * speed * Time.deltaTime;
        transform.position = newPosition;

        // Check if the object has reached the target distance
        if (Vector3.Distance(newPosition, startPosition) >= moveDistance)
        {
            // Reverse direction
            direction *= -1;

            // Flip the object visually without affecting its movement logic
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;

            // Update the starting position to the current position
            startPosition = transform.position;
        }
    }
}
