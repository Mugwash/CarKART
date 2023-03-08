using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public GameObject fRight;
    public GameObject fLeft;
    public GameObject rRight;
    public GameObject rLeft;
    public GameObject carBody;
    [HideInInspector]
    public GameObject frontRightPos;
    [HideInInspector]
    public GameObject frontLeftPos;
    [HideInInspector]
    public GameObject rearRightPos;
    [HideInInspector]
    public GameObject rearLeftPos;
    public float topSpeed;
    private Rigidbody _carBodyRb;
    private float _sMoveInput;
    public GameObject[] tires;
    public float groundDamping;
    public float aeroDamping;
    public float driftDamping = 0.01f;
    private InputAction _mAccelerate;
    private InputAction _mBrake;
    private float _speedInput;
    public float numberOfRotations;
    private float _chasisTireDst;
    [SerializeField]
    private float steeringSpeed;
    [SerializeField] private AnimationCurve accelerationCurve;

    // Start is called before the first frame update
    void Start()
    {
        InputSetup();
        _carBodyRb = carBody.GetComponent<Rigidbody>();
        CreateObject(fLeft,carBody,"frontLeftPos");
        CreateObject(fRight,carBody,"frontRightPos");
        CreateObject(rLeft,carBody,"rearLeftPos");
        CreateObject(rRight,carBody,"rearRightPos");
        DisableCollisionWith(fLeft, fRight, rLeft, rRight, carBody);
        tires = new[] { fLeft, fRight, rLeft, rRight };
        _chasisTireDst = GetDstChasisTire(carBody, fLeft);
    }

    // Update is called once per frame
    void Update()
    {
        _speedInput = _mAccelerate.ReadValue<float>() - _mBrake.ReadValue<float>();
        CarRotation();
        CarPosition();
        Input.GetAxis("Vertical");
        _sMoveInput = Input.GetAxis("Horizontal");
        var carRotation = carBody.transform.rotation;
        rRight.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        rLeft.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        fRight.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        fLeft.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        RotateWheel(fLeft,_sMoveInput,50f,100f);
        RotateWheel(fRight,_sMoveInput,50f,100f);
        TirePosition(fRight,frontRightPos);
        TirePosition(fLeft,frontLeftPos);
        TirePosition(rRight,rearRightPos);
        TirePosition(rLeft,rearLeftPos);
    }

    private void FixedUpdate()
    {
        
        Movement();
        Physics.SyncTransforms();

    }

    void DisableCollisionWith(GameObject obj1, GameObject obj2, GameObject obj3, GameObject obj4,
        GameObject ignoreCollider)
    {
        var colliders = ignoreCollider.GetComponents<Collider>();
        foreach (var collider2 in colliders)
        {
            Physics.IgnoreCollision(obj1.GetComponent<Collider>(), collider2);
            Physics.IgnoreCollision(obj2.GetComponent<Collider>(), collider2);
            Physics.IgnoreCollision(obj3.GetComponent<Collider>(), collider2);
            Physics.IgnoreCollision(obj4.GetComponent<Collider>(), collider2);
        }
    }

    private bool IsTouchingGround(GameObject obj,float dst)
    {
        // Get the collider component of the game object
        Collider component = obj.GetComponent<Collider>();
        // Create a ray that starts at the center of the collider and points downward
        Vector3 rayOrigin = component.bounds.center;
        Vector3 rayDirection = -obj.transform.up;
        // Create a RaycastHit variable to store the hit information
        Ray ray = new Ray(rayOrigin,rayDirection);
        //Debug.DrawRay(rayOrigin, rayDirection*dst,Color.green);
        // Check if the collider is hitting anything below it
        bool isHit = Physics.Raycast(ray, out _, dst);
        
        return isHit;
    }

    void TirePosition(GameObject tire, GameObject tirePos)
    {
        var position = tirePos.transform.position;
        tire.transform.position =new Vector3(position.x,Mathf.Clamp(tire.transform.position.y,position.y-0.1f,position.y+0.1f),position.z);
        
    }

    void Movement()
    {
        float damping;
        var transformPosition = fLeft.transform.position+fRight.transform.position;
        var forwardDir = transformPosition - (rRight.transform.position+rLeft.transform.position);
        //Debug.Log(accelerateValue);
        if (IsTouchingGround(carBody, 1f))
        {
            float accelerationAmount = accelerationCurve.Evaluate(Time.time);
            float currentSpeed = _speedInput * topSpeed;
            Vector3 force = forwardDir * (currentSpeed * accelerationAmount);
            _carBodyRb.AddForce( force , ForceMode.Acceleration);
            damping = groundDamping;
        }
        else
        {
            var carForward = new Vector3(forwardDir.x, 0, forwardDir.z);
            _carBodyRb.AddForce(carForward * (_speedInput * topSpeed), ForceMode.Acceleration);
            damping = aeroDamping;
        }
        
        _carBodyRb.velocity = Vector3.Lerp(_carBodyRb.velocity, Vector3.zero, damping * Time.deltaTime);
    }



    void CarRotation()
    {
        var step = numberOfRotations * Time.deltaTime;
        var avgUpDir = (GroundUpDir(fLeft,1f) + GroundUpDir(fRight,1f)+GroundUpDir(rRight,1f)+GroundUpDir(rLeft,1f)) / 4f;
        _carBodyRb.transform.rotation = Quaternion.RotateTowards(carBody.transform.rotation, Quaternion.LookRotation((fRight.transform.position+fLeft.transform.position) - (rRight.transform.position+rLeft.transform.position), avgUpDir), step);

        if (IsTouchingGround(fLeft,1f)&&IsTouchingGround(fRight,1f))
        {
            Quaternion startRotation = carBody.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, _sMoveInput * 100f, 0) * startRotation;
            carBody.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, Time.deltaTime);

        }
    }



    void CarPosition()
    {
        var position = carBody.transform.position;
        position=new Vector3(position.x, GetYValue(carBody, _chasisTireDst+0.1f), position.z);
        //Debug.DrawRay(carBody.transform.position,-carBody.transform.up,Color.red,0.8f);
        carBody.transform.position = position;
    }

    float GetYValue(GameObject obj, float dst)
    {
        Collider component = obj.GetComponent<Collider>();
        var temp = carBody.transform.position.y;
        // Create a ray that starts at the center of the collider and points downward
        Vector3 rayOrigin = component.bounds.center;
        Vector3 rayDirection = -obj.transform.up;
        if (Physics.Raycast(rayOrigin, rayDirection, out var hit, dst))
        {
            Debug.DrawRay(rayOrigin, rayDirection * dst, Color.red);
            return hit.point.y;
        }
        return temp;
    }

    private Vector3 GroundUpDir(GameObject obj, float maxDst)
    {
        Collider component = obj.GetComponent<Collider>();

        // Create a ray that starts at the center of the collider and points downward
        Vector3 rayOrigin = component.bounds.center;
        Vector3 rayDirection = -obj.transform.up;
        // Create a RaycastHit variable to store the hit information
        Ray ray = new Ray(rayOrigin, rayDirection);

        // Check if the collider is hitting anything below it
        bool isHit = Physics.Raycast(ray, out _, maxDst);
        if (isHit)
        {
            Debug.DrawRay(obj.transform.position, GetRaycastDirection(obj)*5f, Color.yellow);
            return GetRaycastDirection(obj);
        }
        Debug.DrawRay(obj.transform.position, Vector3.up*5f, Color.yellow);
        return Vector3.up/10f;
    }    
    private Vector3 GetRaycastDirection(GameObject obj)
         {
             // Create a ray that starts at the gameObject's position and points downward
             Ray ray = new Ray(obj.transform.position, -obj.transform.up);
     
             // Set up the raycast hit variable

             // Perform the raycast and check if it hit a collider
             if (Physics.Raycast(ray, out var hit))
             {
                 // If it hit a collider, calculate the reflection direction
                 Vector3 reflection = Vector3.Reflect(ray.direction, hit.normal);
     
                 // Return the reflection direction
                 return reflection;
             }
     
             // If the raycast did not hit a collider, return Vector3.zero
             return -Vector3.up;
         }

    private void CreateObject(GameObject positionObject, GameObject parentObject, string objName)
    {
        GameObject newGameObject = new GameObject
        {
            name = objName,
            transform =
            {
                position = positionObject.transform.position
            }
        };
        newGameObject.transform.SetParent(parentObject.transform);
        GetType().GetField(objName).SetValue(this, newGameObject);
    }

    private void InputSetup()
    {
        _mAccelerate = new InputAction("accelerate");
        _mBrake = new InputAction("brake");
        _mAccelerate.AddBinding("<Gamepad>/rightTrigger");
        _mBrake.AddBinding("<Gamepad>/leftTrigger");
        _mAccelerate.Enable();
        _mBrake.Enable();
    }

    static float GetDstChasisTire(GameObject obj1, GameObject obj2)
    {
         var collider1 = obj1.GetComponent<Collider>();
         var collider2 = obj2.GetComponent<Collider>();
         
        return collider1.bounds.center.y - collider2.bounds.min.y;
    }
    
    float CalculateSpeedUsingRaycast(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit))
        {
            return hit.distance / Time.deltaTime;
        }
        return 0f;
    }
    
    public void RotateWheel(GameObject wheelObject, float steeringInput, float maxWheelAngle, float maxWheelRotationSpeed) {
        // Get the current rotation of the car body
        Quaternion currentRotation = carBody.transform.rotation;

        // Calculate the desired rotation based on the steering input
        Quaternion desiredRotation = Quaternion.Euler(0f, steeringInput * maxWheelAngle, 0f) * currentRotation;

        // Calculate the rotation speed based on the steering input
        float rotationSpeed = maxWheelRotationSpeed * Mathf.Abs(steeringInput);

        // Rotate the wheel towards the desired rotation
        wheelObject.transform.rotation = Quaternion.RotateTowards(wheelObject.transform.rotation, desiredRotation, rotationSpeed);
    }


}
