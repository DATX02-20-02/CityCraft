using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What: A Class that applies camera control to the application.
// Why: To allow inspecting the city from different angles and perspectives.
// How: By scanning keyboard strokes using Unity's built in Update function using standard WASD movement with rotation dependency.

public class CameraMovement : MonoBehaviour {

    [SerializeField] private float mouseSensitivity = 0f;
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float rotationSnapLimit = 0f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float prevMouseX = 0f;
    private float prevMouseY = 0f;
    private bool cursorLocked = false;
    private float cappedDeltaTime = 0f;

    private void Start() {
        this.xRotation = transform.localEulerAngles.x;
        this.yRotation = transform.localEulerAngles.y;
    }

    private void Update() {

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= 5.0f; // Weeeee!
        if (Input.GetKey(KeyCode.LeftControl))
            speed /= 5.0f; // ughhhh...

        cappedDeltaTime = Mathf.Max(Time.deltaTime, 0.1f);

        if (Input.GetKey("w")) {
            transform.position += transform.forward * speed * cappedDeltaTime;
        }
        if (Input.GetKey("a")) {
            transform.position -= transform.right * speed * cappedDeltaTime;
        }
        if (Input.GetKey("s")) {
            transform.position -= transform.forward * speed * cappedDeltaTime;
        }
        if (Input.GetKey("d")) {
            transform.position += transform.right * speed * cappedDeltaTime;
        }
        if (Input.GetKey("q")) {
            transform.position -= transform.up * speed * cappedDeltaTime;
        }
        if (Input.GetKey("e") || Input.GetKey(KeyCode.Space)) {
            transform.position += transform.up * speed * cappedDeltaTime;
        }

        // Lock cursor for 3D camera movement
        if (Input.GetMouseButton(1)) {
            // Lock the mouse on the center
            if (!cursorLocked) {
                Cursor.lockState = CursorLockMode.Locked;
                cursorLocked = true;
            }

            // Get the x and y movement of the mouse
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Reduce rotation snaps
            if (Mathf.Abs(mouseX - prevMouseX) < rotationSnapLimit && Mathf.Abs(mouseY - prevMouseY) < rotationSnapLimit) {
                // Rotate camera
                xRotation -= mouseY;    // rotate up & down
                yRotation += mouseX;    // rotate left & right

                xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Disallow looking behind

                transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f); // Apply the rotation
            }

            prevMouseX = mouseX;
            prevMouseY = mouseY;
        }
        else {
            if (cursorLocked) {
                Cursor.lockState = CursorLockMode.None;
                cursorLocked = false;
            }
        }
    }
}
