using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelController : MonoBehaviour
{
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider backRight;
    [SerializeField] WheelCollider backLeft;
    [SerializeField] Transform frontRightTransform;
    [SerializeField] Transform frontLeftTransform;

    [SerializeField] float acceleration = 500;

    private float currentAcceleration;
    private float currentTurnAngle;
    private float maxTurnAngle = 15f;

    private void FixedUpdate() {
        currentAcceleration = acceleration * Input.GetAxis("Vertical");
        frontRight.motorTorque = currentAcceleration;
        frontLeft.motorTorque = currentAcceleration;

        currentTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
        frontLeft.steerAngle = currentTurnAngle;
        frontRight.steerAngle = currentTurnAngle;

        UpdateWheel(frontLeft, frontLeftTransform);
        UpdateWheel(frontRight, frontRightTransform);
    }

    private void UpdateWheel(WheelCollider collider, Transform wheelTransform) {
        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }
}
