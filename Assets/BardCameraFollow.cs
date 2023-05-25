using UnityEngine;

public class BardCameraFollow : MonoBehaviour {

    public Transform target;
    public float offset;
    public float smoothTime = 0.3f;

    Vector3 _velocity;
    void Update() {
        // Get the position of the target object.
        Vector3 targetPosition = target.position;

        // Smoothly move the camera towards the target position.
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
    }
}