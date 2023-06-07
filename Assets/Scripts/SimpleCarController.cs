using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

[System.Serializable]
public class AxleInfo {
    [HideInInspector] public WheelCollider _leftWheel;
    [HideInInspector] public WheelCollider _rightWheel;
    public bool motor;
    public bool steering;
}

[System.Serializable]
public class WheelObject
{
    public GameObject leftWheel;
    public GameObject rightWheel;
}

[System.Serializable]
public class WheelValues
{
    //change variables in this class to public to see them in the inspector
    public float mass;
    public float wheelDampningRate;
    public float suspensionDistance;
    public float forceAppPointDistance;
    public float wheelColliderCenterY;
    public float spring;
    public float damper;
    public float targetPosition;
    public float fExtremumSlip;
    public float fExtremumValue;
    public float fAsymptoteSlip;
    public float fAsymptoteValue;
    public float fStiffness;
    public float sExtremumSlip;
    public float sExtremumValue;
    public float sAsymptoteSlip;
    public float sAsymptoteValue;
    public float sStiffness;
}

public class SimpleCarController : MonoBehaviour
{
    
    public List<AxleInfo> axleInfos;
    public List<WheelObject> wheelObjects;
    public WheelValues wheelValues;
    public float maxMotorTorque;
    public float maxSteeringAngle;
    public float brakeForce;
    public GameObject carBody;
    public float maxSpeedMph;
    public float maxReverseSpeed;

    private Rigidbody rb;
    private bool isBraking;


    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(carBody.transform.localPosition.x,carBody.GetComponent<Collider>().bounds.min.y,carBody.transform.localPosition.z);
        for(int i = 0; i < axleInfos.Count; i++)
        {
            axleInfos[i]._leftWheel = wheelObjects[i].leftWheel.AddComponent<WheelCollider>();
            axleInfos[i]._rightWheel = wheelObjects[i].rightWheel.AddComponent<WheelCollider>();
            SetWheelColliderValues(axleInfos[i]._leftWheel,wheelValues,GetMeshRadius(wheelObjects[i].leftWheel));
            SetWheelColliderValues(axleInfos[i]._rightWheel,wheelValues,GetMeshRadius(wheelObjects[i].rightWheel));
            CreateChildObject(wheelObjects[i].leftWheel);
            CreateChildObject(wheelObjects[i].rightWheel);
        }
        foreach (AxleInfo axleInfo in axleInfos)
        {
            DisableCollisionWith(axleInfo._leftWheel.gameObject, axleInfo._rightWheel.gameObject, carBody);
            
        }

    }
    // finds the corresponding visual wheel
    // correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider collider1)
    {
        if (collider1.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider1.transform.GetChild(0);

        collider1.GetWorldPose(out var position, out var rotation);

        var transform1 = visualWheel.transform;
        transform1.position = position;
        transform1.rotation = rotation;
    }

    public void FixedUpdate()
    {
        addDownforce(GetMilesPerHour(this.gameObject));
        float motor;
        if (Input.GetAxis("Vertical") > 0)
        {
            motor = maxMotorTorque * Input.GetAxis ("Vertical");
        }
        else
        {
            motor = 0;
        }
        
        rb.AddForce(transform.forward * motor);
        float steering = maxSteeringAngle * Input.GetAxis ("Horizontal");
        
        if(GetMilesPerHour(this.gameObject)>0 && Input.GetAxis("Vertical") < 0)
        {
            isBraking = true;
        }
        else
        {
            isBraking = false;
        }
        ApplyMaxSpeeds(maxSpeedMph,maxReverseSpeed);
        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo._leftWheel.steerAngle = steering;
                axleInfo._rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor)
            {
                axleInfo._leftWheel.motorTorque = motor;
                axleInfo._rightWheel.motorTorque = motor;
            }
            else
            {
                axleInfo._leftWheel.motorTorque = 0;
                axleInfo._rightWheel.motorTorque = 0;
            }

            if (isBraking)
            {
                axleInfo._leftWheel.brakeTorque = brakeForce;
                axleInfo._rightWheel.brakeTorque = brakeForce;
            }
            else
            {
                axleInfo._leftWheel.brakeTorque = 0;
                axleInfo._rightWheel.brakeTorque = 0;
            }
            ApplyLocalPositionToVisuals(axleInfo._leftWheel);
            ApplyLocalPositionToVisuals(axleInfo._rightWheel);
        }
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
        Collider collider1 = obj.GetComponent<Collider>();
        return collider1;
    }

    private static void SetWheelColliderValues(WheelCollider wheelCollider,WheelValues wheelValue,float wheelRadius)
    {
        wheelCollider.mass = wheelValue.mass;
        wheelCollider.radius = wheelRadius;
        wheelCollider.wheelDampingRate = wheelValue.wheelDampningRate;
        wheelCollider.suspensionDistance = wheelValue.suspensionDistance;
        wheelCollider.forceAppPointDistance = wheelValue.forceAppPointDistance;
        wheelCollider.center = new Vector3(0, wheelValue.wheelColliderCenterY, 0);
        
        JointSpring wheelColliderSuspensionSpring = default;
        wheelColliderSuspensionSpring.spring = wheelValue.spring;
        wheelColliderSuspensionSpring.damper = wheelValue.damper;
        wheelColliderSuspensionSpring.targetPosition = wheelValue.targetPosition;
        wheelCollider.suspensionSpring = wheelColliderSuspensionSpring;
        
        WheelFrictionCurve forwardFriction = default;
        forwardFriction.extremumSlip = wheelValue.fExtremumSlip;
        forwardFriction.extremumValue = wheelValue.fExtremumValue;
        forwardFriction.asymptoteSlip = wheelValue.fAsymptoteSlip;
        forwardFriction.asymptoteValue = wheelValue.fAsymptoteValue;
        forwardFriction.stiffness = wheelValue.fStiffness;
        wheelCollider.forwardFriction = forwardFriction;
        
        WheelFrictionCurve sidewaysFriction = default; 
        sidewaysFriction.extremumSlip = wheelValue.sExtremumSlip;
        sidewaysFriction.extremumValue = wheelValue.sExtremumValue;
        sidewaysFriction.asymptoteSlip = wheelValue.sAsymptoteSlip;
        sidewaysFriction.asymptoteValue = wheelValue.sAsymptoteValue;
        sidewaysFriction.stiffness = wheelValue.sStiffness;
        wheelCollider.sidewaysFriction = sidewaysFriction;
    }
    
    private void ApplyMaxSpeeds(float topSpeedMph , float topReverseSpeedMph)
    {
        if (GetMilesPerHour(this.gameObject) >= (topSpeedMph) || GetMilesPerHour(this.gameObject) <= topReverseSpeedMph)
        {
            this.gameObject.GetComponent<Rigidbody>().drag = 1;
        }
        else
        {
            this.gameObject.GetComponent<Rigidbody>().drag = 0;
        }
    }
    public float GetMeshRadius(GameObject obj)
    {
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        float radius = mesh.bounds.size.y/2;
        return radius;
    }
    
    // create a child object on the object passed in, and then move the meshfilter and meshrenderer to the child object
    public void CreateChildObject(GameObject obj)
    {
        GameObject childObject = new GameObject();
        childObject.transform.parent = obj.transform;
        childObject.name = obj.name + "Mesh";
        childObject.AddComponent<MeshFilter>();
        childObject.AddComponent<MeshRenderer>();
        childObject.GetComponent<MeshFilter>().mesh = obj.GetComponent<MeshFilter>().mesh;
        childObject.GetComponent<MeshRenderer>().material = obj.GetComponent<MeshRenderer>().material;
        obj.GetComponent<MeshFilter>().mesh = null;
        obj.GetComponent<MeshRenderer>().material = null;
    }

    private void addDownforce(float speed)
    {
        rb.AddForce(-transform.up * speed);
    }
}