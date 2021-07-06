using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Free3DMovement : MonoBehaviour
{
    public float acceleration = 60;
    public float mouseMovementSpeed = 5;
    public float sideRotationAcceleration = 20;
    public float sDesceleration = .1f;
    public float rDesceleration = .2f;
    private Vector3 speed;
    private float sideRotationSpeed;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        float sideInput = Input.GetAxisRaw("SideRotation");

        speed += transform.forward * verticalInput * acceleration * Time.deltaTime;
        speed += transform.right * horizontalInput * acceleration * Time.deltaTime;

        sideRotationSpeed += sideInput * sideRotationAcceleration * Time.deltaTime;
        
        transform.position += speed * Time.deltaTime;
        
        Vector3 rotation = new Vector3(-Input.GetAxis("Mouse Y") * mouseMovementSpeed
            ,Input.GetAxis("Mouse X") * mouseMovementSpeed
            , -sideRotationSpeed);

        transform.Rotate(rotation);
        
        speed = Vector3.Lerp(speed, Vector3.zero, sDesceleration);
        sideRotationSpeed = Mathf.Lerp(sideRotationSpeed, 0, rDesceleration);
    }
}
