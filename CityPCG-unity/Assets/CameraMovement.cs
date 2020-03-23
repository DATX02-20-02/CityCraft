using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What: A Class that applies camera control to the application.
// Why: To allow inspecting the city from different angles and perspectives.
// How: By scanning keyboard strokes using unity's built in Update function using standard WASD movement with rotation dependency.

public class CameraMovement : MonoBehaviour{
    //public Transform cam;
    [SerializeField] float mouseSensitivity;
    [SerializeField] float moveSpeed;
    float xRotation = 0f;
    float yRotation = 0f;
    Vector3 up = new Vector3(0f, 1f, 0f);

    void Start(){
        mouseSensitivity = 500f;
        moveSpeed = 100f;
    }

    // Update is called once per frame
    void Update(){

        if (Input.GetKey("w")){
            transform.position += transform.forward * moveSpeed* Time.deltaTime;
        }
        if (Input.GetKey("a")){
            transform.position -= transform.right * moveSpeed* Time.deltaTime;
        }
        if (Input.GetKey("s")){
            transform.position -= transform.forward * moveSpeed* Time.deltaTime;
        }
        if (Input.GetKey("d")){
            transform.position += transform.right * moveSpeed* Time.deltaTime;
        }
        if (Input.GetKey("q")){
            transform.position -= up * moveSpeed* Time.deltaTime;
        }
        if (Input.GetKey("e")){
            transform.position += up * moveSpeed* Time.deltaTime;
        }

        if (!Input.GetMouseButton(1)){ // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButton(1)){ // Lock cursor for 3D camera movement
            Cursor.lockState = CursorLockMode.Locked; //Lock the mouse on the center

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime; //Get the x and y movement of the mouse
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;    // rotate up & down
            yRotation += mouseX;    // rotate left & right

            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Disallow looking behind

            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f); // Apply the rotation
        }
    }
}








