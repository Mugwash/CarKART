using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public GameObject fRight;
    public GameObject fLeft;
    public GameObject rRight;
    public GameObject rLeft;
    public GameObject carBody;
    public GameObject frontRightPos;
    public GameObject frontLeftPos;
    public GameObject rearRightPos;
    public GameObject rearLeftPos;
    public float topSpeed;
    private Rigidbody _carBodyRb;
    private float _fMoveInput;
    private float _sMoveInput;
    public GameObject[] tires;
    public float groundDamping;
    public float aeroDamping;
    public float driftDamping = 0.01f;
    private InputAction m_Accelerate;
    private InputAction m_Brake;
    private float speedInput;

    // Start is called before the first frame update
    void Start()
    {
        inputSetup();
        _carBodyRb = carBody.GetComponent<Rigidbody>();
        CreateObject(fLeft,carBody,"frontLeftPos");
        CreateObject(fRight,carBody,"frontRightPos");
        CreateObject(rLeft,carBody,"rearLeftPos");
        CreateObject(rRight,carBody,"rearRightPos");
        DisableCollisionWith(fLeft, fRight, rLeft, rRight, carBody);
        tires = new[] { fLeft, fRight, rLeft, rRight };
    }

    // Update is called once per frame
    void Update()
    {
        speedInput = m_Accelerate.ReadValue<float>() - m_Brake.ReadValue<float>();
        CarRotation();
        CarPosition();
        _fMoveInput = Input.GetAxis("Vertical");
        _sMoveInput = Input.GetAxis("Horizontal");
        var carRotation = carBody.transform.rotation;
        rRight.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        rLeft.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        fRight.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
        fLeft.transform.rotation = new Quaternion(carRotation.x, carRotation.y, carRotation.z, carRotation.w);
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

    private bool IsTouchingGround(GameObject gameObject,float dst)
    {
        // Get the collider component of the game object
        Collider component = gameObject.GetComponent<Collider>();
        // Create a ray that starts at the center of the collider and points downward
        Vector3 rayOrigin = component.bounds.center;
        Vector3 rayDirection = -gameObject.transform.up;
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

    public void Accelerate()
    {
        Debug.Log(speedInput);

    }

    void Movement()
    {
        var damping = 0f;
        var transformPosition = fLeft.transform.position+fRight.transform.position;
        var forwardDir = transformPosition - (rRight.transform.position+rLeft.transform.position);
        //Debug.Log(accelerateValue);

        if (IsTouchingGround(carBody, 1f))
        {
            _carBodyRb.AddForce( forwardDir *(speedInput * topSpeed) , ForceMode.Acceleration);
            damping = groundDamping;
        }
        else
        {
            var carForward = new Vector3(forwardDir.x, 0, forwardDir.z);
            _carBodyRb.AddForce(carForward * (speedInput * topSpeed), ForceMode.Acceleration);
            damping = aeroDamping;
        }
        
        _carBodyRb.velocity = Vector3.Lerp(_carBodyRb.velocity, Vector3.zero, damping * Time.deltaTime);
    }



    void CarRotation()
    {
        var step = 70.0f * Time.deltaTime;
        var avgUpDir = (GroundUpDir(fLeft,1f) + GroundUpDir(fRight,1f)+GroundUpDir(rRight,1f)+GroundUpDir(rLeft,1f)) / 4f;
        _carBodyRb.transform.rotation = Quaternion.RotateTowards(carBody.transform.rotation, Quaternion.LookRotation((fRight.transform.position+fLeft.transform.position) - (rRight.transform.position+rLeft.transform.position), avgUpDir), step);

        if (IsTouchingGround(fLeft,1f)&&IsTouchingGround(fRight,1f))
        {
            carBody.transform.Rotate(0,_sMoveInput*100f*Time.deltaTime,0,Space.Self);
        }
    }



    void CarPosition()
    {
        var position = carBody.transform.position;
        position=new Vector3(position.x, GetYValue(carBody, 0.8f), position.z);
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

    private Vector3 GroundUpDir(GameObject gameObject, float maxDST)
    {
        Collider component = gameObject.GetComponent<Collider>();

        // Create a ray that starts at the center of the collider and points downward
        Vector3 rayOrigin = component.bounds.center;
        Vector3 rayDirection = -gameObject.transform.up;
        // Create a RaycastHit variable to store the hit information
        Ray ray = new Ray(rayOrigin, rayDirection);

        // Check if the collider is hitting anything below it
        bool isHit = Physics.Raycast(ray, out _, maxDST);
        if (isHit)
        {
            Debug.DrawRay(gameObject.transform.position, GetRaycastDirection(gameObject)*5f, Color.yellow);
            return GetRaycastDirection(gameObject);
        }
        Debug.DrawRay(gameObject.transform.position, Vector3.up*5f, Color.yellow);
        return Vector3.up/10f;
    }    
    private Vector3 GetRaycastDirection(GameObject gameObject)
         {
             // Create a ray that starts at the gameObject's position and points downward
             Ray ray = new Ray(gameObject.transform.position, -gameObject.transform.up);
     
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
    
    private Vector3 GetForwardDirection(GameObject gameObject)
    {
        Ray ray = new Ray(gameObject.transform.position, -Vector3.up);
        if (Physics.Raycast(ray, out var hit))
        {
            Vector3 forwardDirection = Vector3.Cross(hit.normal, -gameObject.transform.right).normalized;
            return forwardDirection;
        }

        return Vector3.zero;
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
    public void inputSetup()
    {
        m_Accelerate = new InputAction("accelerate");
        m_Brake = new InputAction("brake");
        m_Accelerate.AddBinding("<Gamepad>/rightTrigger");
        m_Brake.AddBinding("<Gamepad>/leftTrigger");
        m_Accelerate.Enable();
        m_Brake.Enable();
    }
}
