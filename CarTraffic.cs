using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarTraffic : MonoBehaviour
{
    [Header("Traffic Settings")]
    public List<GameObject> carPrefabs; // List of car prefabs to choose from

    public Transform startPoint; // Starting position for the traffic

    public Transform endPoint; // Final position for the traffic

    public int numberOfCars = 5; // Number of cars to spawn initially

    public float speed = 5f; // Speed of the cars

    public float minSpawnInterval = 1f; // Minimum interval between car spawns

    public float maxSpawnInterval = 3f; // Maximum interval between car spawns

    private List<GameObject> activeCars = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnCars());
    }

    private IEnumerator SpawnCars()
    {
        while (true)
        {
            if (activeCars.Count < numberOfCars)
            {
                SpawnCar();
            }

            // Wait for a random interval before attempting to spawn the next car
            yield return new WaitForSeconds(Random
                        .Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    private void SpawnCar()
    {
        if (carPrefabs.Count == 0 || startPoint == null || endPoint == null)
        {
            Debug
                .LogWarning("Missing car prefabs or points for traffic simulation.");
            return;
        }

        // Randomly choose a car prefab
        GameObject selectedCar = carPrefabs[Random.Range(0, carPrefabs.Count)];

        // Instantiate the car at the start point
        GameObject car =
            Instantiate(selectedCar, startPoint.position, Quaternion.identity);
        activeCars.Add (car);

        // Start the car's movement
        StartCoroutine(MoveCar(car));
    }

    private IEnumerator MoveCar(GameObject car)
    {
        while (true)
        {
            if (car == null) yield break;

            // Calculate the direction to the endpoint
            Vector3 direction =
                (endPoint.position - car.transform.position).normalized;

            // Rotate the car to face the endpoint
            if (
                direction != Vector3.zero // Avoid errors if direction is zero
            )
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation =
                    Quaternion
                        .Slerp(car.transform.rotation,
                        targetRotation,
                        Time.deltaTime * speed);
            }

            // Move the car towards the endpoint
            car.transform.position =
                Vector3
                    .MoveTowards(car.transform.position,
                    endPoint.position,
                    speed * Time.deltaTime);

            // Check if the car has reached the endpoint
            if (
                Vector3.Distance(car.transform.position, endPoint.position) <
                0.1f
            )
            {
                // Destroy the car and remove it from the active list
                activeCars.Remove (car);
                Destroy (car);
                yield break;
            }

            yield return null;
        }
    }
}
