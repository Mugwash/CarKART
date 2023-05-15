using UnityEngine;
using System.Collections.Generic;

[System.Serializable]

public class AxleInfo {
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;
}
     
public class SimpleCarController : MonoBehaviour {
    public List<AxleInfo> axleInfos; 
    public float maxMotorTorque;
    public float maxSteeringAngle;
    public GameObject carBody;
    public void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = carBody.transform.position;
        foreach (AxleInfo axleInfo in axleInfos) {
            DisableCollisionWith(axleInfo.leftWheel.gameObject, axleInfo.rightWheel.gameObject, carBody);
        }
        
    }

    // finds the corresponding visual wheel
    // correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) {
            return;
        }
     
        Transform visualWheel = collider.transform.GetChild(0);
     
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
     
        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }
     
    public void FixedUpdate()
    {
        float motor = maxMotorTorque * Input.GetAxis("Vertical");
        float steering = maxSteeringAngle * Input.GetAxis("Horizontal");
     
        foreach (AxleInfo axleInfo in axleInfos) {
            setSideFrictionExtremiumValues(axleInfo.leftWheel, 4f , 20);
            setSideFrictionExtremiumValues(axleInfo.rightWheel, 4f , 20);
            if (axleInfo.steering) {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor) {
                axleInfo.leftWheel.motorTorque= axleInfo.rightWheel.motorTorque  = motor;
            }
            else
            {
                axleInfo.leftWheel.motorTorque = 0;
                axleInfo.rightWheel.motorTorque = 0;
            }
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        Debug.Log(GetMilesPerHour(this.gameObject));
    }
    
    
    void DisableCollisionWith(GameObject obj1, GameObject obj2,
        GameObject ignoreCollider)
    {
        var colliders = ignoreCollider.GetComponents<Collider>();
        foreach (var collider2 in colliders)
        {
            Physics.IgnoreCollision(obj1.GetComponent<Collider>(), collider2);
            Physics.IgnoreCollision(obj2.GetComponent<Collider>(), collider2);
        }
    }
    
    
    private float GetVelocity(GameObject obj)
    {
        float velocityInDirection = Vector3.Dot(obj.GetComponent<Rigidbody>().velocity, carBody.transform.forward);
        return velocityInDirection;
    }
    
    private float GetMilesPerHour(GameObject obj)
    {
        float velocityInDirection = GetVelocity(obj);
        float milesPerHour = velocityInDirection * 2.23693629f;
        return milesPerHour;
    }
    
    private Collider GetCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        return collider;
    }
    
    private void setSideFrictionExtremiumValues(WheelCollider wheelCollider, float extremumSlip, float extremumValue)
    {
        WheelFrictionCurve frictionCurve = wheelCollider.sidewaysFriction;
        frictionCurve.extremumSlip = extremumSlip;
        frictionCurve.extremumValue = extremumValue;
        wheelCollider.sidewaysFriction = frictionCurve;
    }
}