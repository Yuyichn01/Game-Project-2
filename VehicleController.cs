using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public Rigidbody2D backWheel;

    public Rigidbody2D frontWheel;

    public float speed = 30;

    private float movement;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        movement = Input.GetAxis("Horizontal") / 2;
    }

    private void FixedUpdate()
    {
        backWheel.AddTorque(-movement * speed * Time.fixedDeltaTime);
        frontWheel.AddTorque(-movement * speed * Time.fixedDeltaTime);
    }
}
