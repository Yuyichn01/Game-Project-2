using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public Rigidbody2D backWheel;

    public Rigidbody2D frontWheel;

    public float acceleration = 30;

    public float maximumSpeed = 60; // Limit the speed

    private float movement = 0;

    void Start()
    {
        maximumSpeed *= 100;
    }

    // Update is called once per frame
    void Update()
    {
        movement = Input.GetAxis("Horizontal") / 2;
    }

    private void FixedUpdate()
    {
        // Check if the current velocity magnitude exceeds the maximum speed
        if (
            Math.Abs(backWheel.angularVelocity) < Math.Abs(maximumSpeed) &&
            Math.Abs(frontWheel.angularVelocity) < Math.Abs(maximumSpeed)
        )
        {
            float torque = movement * acceleration * Time.fixedDeltaTime;
            backWheel.AddTorque(-torque);
            frontWheel.AddTorque(-torque);
        }

        // Log the applied torque for debugging purposes
        Debug.Log(backWheel.angularVelocity);
    }
}
