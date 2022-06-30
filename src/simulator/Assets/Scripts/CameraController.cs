using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float cameraSpeed = 2f;
    [SerializeField] float speedMultiplier = 2f;
    [SerializeField] float sensitivity = 1.0f;
 
    private Vector3 anchorPoint;
    private Quaternion anchorRotation;
 
    private void Update() {
        ControlMovement();
        ControlRotation();
    }

    private void ControlRotation() {
        if (Input.GetMouseButtonDown(1)) {
            anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            anchorRotation = transform.rotation;
        }
        if (Input.GetMouseButton(1)) {
            Quaternion rotation = anchorRotation;
            Vector3 increment = anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            rotation.eulerAngles += increment * sensitivity;
            transform.rotation = rotation;
        }
    }

    private void ControlMovement() {
        Vector3 move = Vector3.zero;
        float speed = cameraSpeed * Time.deltaTime * 9f;
        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");
        float qeAxis = Input.GetAxis("QE");
        move += Vector3.right * horizontalAxis;
        move += Vector3.forward * verticalAxis;
        move += Vector3.up * qeAxis;
        move *= speed;
        if (Input.GetKey(KeyCode.LeftShift))
            move *= speedMultiplier;
        transform.Translate(move);
    }
}
