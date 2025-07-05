using System.Collections.Generic;
using UnityEngine;

public class EscalatorController : MonoBehaviour
{
    public bool startEscalator = false;

    public bool stepOnEscalator = false;

    private Rigidbody2D userRb;

    [SerializeField]
    private List<Transform> steps; // List of escalator steps

    [SerializeField]
    private float stepSpeed = 2.0f; // Speed of the steps

    [SerializeField]
    private Transform startPoint; // The transform representing the start position

    [SerializeField]
    private Transform endPoint; // The transform representing the end position

    [SerializeField]
    private float separationDistance = 2.0f; // Distance separating each step

    private List<Rigidbody2D> objectsOnEscalator = new List<Rigidbody2D>(); // List to track objects on the escalator

    [SerializeField]
    private Transform userStartPoint; // The start point of the escalator

    [SerializeField]
    private Transform userEndPoint; // The end point of the escalator

    [SerializeField]
    private float moveSpeed = 2.0f; // Speed at which objects move along the escalator

    private void Start()
    {
        if (startEscalator == true)
        {
            // Initially stagger the positions of the steps based on the separation distance
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].position = CalculateInitialPosition(i);
            }
        }
    }

    private void Update()
    {
        if (startEscalator == true)
        {
            // Move each step toward the end point
            for (int i = 0; i < steps.Count; i++)
            {
                MoveStep(steps[i], i);
            }
        }
    }

    // Calculate the initial position of each step based on its index and separation distance
    private Vector3 CalculateInitialPosition(int index)
    {
        float stepOffset = separationDistance * index; // Offset each step by the separation distance
        Vector3 direction =
            (endPoint.position - startPoint.position).normalized; // Direction of movement
        return startPoint.position - direction * stepOffset; // Set the initial position offset by the calculated distance
    }

    // Moves the step towards the end position, and if it reaches the end, resets it to the start position with the correct spacing
    private void MoveStep(Transform step, int index)
    {
        // Move the step toward the end point
        step.position =
            Vector3
                .MoveTowards(step.position,
                endPoint.position,
                stepSpeed * Time.deltaTime);

        // Check if the step has reached the end point
        if (
            Vector3.Distance(step.position, endPoint.position) < 0.01f // Small threshold to account for precision errors
        )
        {
            // Reset the step to the start point, offset by its index
            step.position = startPoint.position;
        }
    }

    // This method is called when an object enters the trigger collider of the escalator
    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Add the object to the list to move it
            objectsOnEscalator.Add (rb);
        }
    }

    // This method is called when an object exits the trigger collider of the escalator
    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Remove the object from the list when it leaves the escalator
            objectsOnEscalator.Remove (rb);
        }
        stepOnEscalator = false;
    }

    private void FixedUpdate()
    {
        if (stepOnEscalator)
        {
            // Move all objects currently on the escalator
            foreach (Rigidbody2D rb in objectsOnEscalator)
            {
                MoveObjectAlongEscalator (rb);
            }
        }
    }

    private void MoveObjectAlongEscalator(Rigidbody2D rb)
    {
        // Calculate direction and move the object towards the end point
        Vector2 direction =
            (endPoint.position - startPoint.position).normalized;
        rb
            .MovePosition(rb.position +
            direction * moveSpeed * Time.fixedDeltaTime);
    }
}
