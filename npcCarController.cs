using System.Collections.Generic;
using UnityEngine;

public class npcCarController : MonoBehaviour
{
    
    [SerializeField]
    private List<Transform> cars; // List of escalator steps

    [SerializeField]
    private float stepSpeed = 2.0f; // Speed of the steps

    [SerializeField]
    private Transform startPoint; // The transform representing the start position

    [SerializeField]
    private Transform endPoint; // The transform representing the end position

    [SerializeField]
    private float separationDistance = 2.0f; // Distance separating each step

    private void Start()
    {
        // Initially stagger the positions of the steps based on the separation distance
        for (int i = 0; i < cars.Count; i++)
        {
            cars[i].position = CalculateInitialPosition(i);
        }
    }

    private void Update()
    {
        // Move each step toward the end point
        for (int i = 0; i < cars.Count; i++)
        {
            MoveStep(cars[i], i);
        }
    }

    // Calculate the initial position of each step based on its index and separation distance
    private Vector3 CalculateInitialPosition(int index)
    {
        float stepOffset = separationDistance * index; // Offset each step by the separation distance
        Vector3 direction = (endPoint.position - startPoint.position).normalized; // Direction of movement
        return startPoint.position - direction * stepOffset; // Set the initial position offset by the calculated distance
    }

    // Moves the step towards the end position, and if it reaches the end, resets it to the start position with the correct spacing
    private void MoveStep(Transform step, int index)
    {
        // Move the step toward the end point
        step.position = Vector3.MoveTowards(step.position, endPoint.position, stepSpeed * Time.deltaTime);

        // Check if the step has reached the end point
        if (Vector3.Distance(step.position, endPoint.position) < 0.01f) // Small threshold to account for precision errors
        {
            // Reset the step to the start point, offset by its index
            step.position = CalculateInitialPosition(index);
        }
    }
}
