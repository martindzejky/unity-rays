using UnityEngine;

public class CameraController : MonoBehaviour {
    public float sensitivityX = 15f;
    public float sensitivityY = 15f;
    public float movementSpeed = 5f;

    private bool mouseLocked = false;
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Update() {
        if (Input.GetButtonDown("Fire1")) {
            mouseLocked = !mouseLocked;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            mouseLocked = false;
        }

        Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !mouseLocked;

        if (mouseLocked) {
            rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp (rotationY, -70f, 70f);

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);

            transform.Translate(
                Input.GetAxis("Horizontal") * Time.deltaTime * movementSpeed,
                0f,
                Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed,
                Space.Self
            );
        }
    }
}
