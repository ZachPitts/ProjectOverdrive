using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class ArcadeCarController : MonoBehaviour
{

    [Header("References")]
    public Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private InputController inputController;
    [SerializeField] private GameObject frontLeftWheel;
    [SerializeField] private GameObject frontRightWheel;
    [SerializeField] private GameObject backLeftWheel;
    [SerializeField] private GameObject backRightWheel;

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float dampenerStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    [SerializeField] private bool[] wheelsAreGrounded = new bool[4];
    public bool isGrounded;

    [Header("Input")]
    public float moveInput = 0;
    private float steerInput = 0;

    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private float dragCoefficient = 1f;
    [SerializeField] private float steerResponse = 10f;
    [SerializeField] private float yawDamping = 0.5f;
    private float steerInputSmoothed = 0f;
    private float wheelSpinAngle;

    [Header("Power and Drag")]
    [SerializeField] private float engineAcceleration = 10f;
    [SerializeField] private AnimationCurve enginePowerVsSpeed = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

    [SerializeField] private float aeroDragAccel = 10f;
    [SerializeField] private AnimationCurve aeroDragVsSpeed = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public Vector3 currentCarLocalVelocity = Vector3.zero;
    public float currentSpeed;
    private float carVelocityRatio = 0;

    private void Update()
    {
        if(inputController != null)
        {
            GetInput();
        }

        currentSpeed = currentCarLocalVelocity.magnitude;

        float wheelAngle = steerInput * 30f;
        float forwardSpeed = currentCarLocalVelocity.z;
        float angularSpeedRad = forwardSpeed / wheelRadius;
        float angularSpeedDeg = angularSpeedRad * Mathf.Rad2Deg;

        

        float deltaRotation = angularSpeedDeg * Time.deltaTime;
        wheelSpinAngle -= deltaRotation;

        Quaternion spin = Quaternion.Euler(wheelSpinAngle, 180, 0);

        frontLeftWheel.transform.localRotation = Quaternion.Euler(wheelSpinAngle, 180 + wheelAngle, 0);
        frontRightWheel.transform.localRotation = Quaternion.Euler(wheelSpinAngle, 180 + wheelAngle, 0);
        backLeftWheel.transform.localRotation = spin;
        backRightWheel.transform.localRotation = spin;

    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        
    }



    #region Car Status Check

    
    private void GroundCheck()
    {

        isGrounded = true;

        for(int i = 0; i < wheelsAreGrounded.Length; i++)
        {
            if(wheelsAreGrounded[i] == false)
            {
                isGrounded = false;
            }
        }
    }


    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
    }

    #endregion


    #region Input Handling


    private void GetInput()
    {
        moveInput = inputController.throttle;

        // smooth steering input in fixedUpdate-time
        float target = inputController.steering;
        steerInputSmoothed = Mathf.Lerp(steerInputSmoothed, target, steerResponse * Time.fixedDeltaTime);

        steerInput = steerInputSmoothed;
    }
    public void SetInputs(float forward, float steering)
    {
        moveInput = forward;
        steerInput = steering;

        Debug.Log($"MoveInput = {moveInput}, Steering = {steerInput}");
    }

    #endregion


    private void Movement()
    {
        if(isGrounded)
        {
            Acceleration();
            Turn();
            SidewaysDrag();
            //Deceleration();
            carRB.linearDamping = 3;
        }
        else
        {
            carRB.linearDamping = 0.1f;
        }
    }

    private void Acceleration()
    {
        //carRB.AddForceAtPosition(acceleration * moveInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);

        // forward speed normalized
        float speed01 = Mathf.Clamp01(Mathf.Abs(currentCarLocalVelocity.z) / maxSpeed);

        // engine pull fades with speed
        float engineFactor = enginePowerVsSpeed.Evaluate(speed01);
        Vector3 engineAccelerationVelocity = transform.forward * (engineAcceleration * moveInput * engineFactor);

        // aero drag grows with speed
        // drag should oppose current velocity direction
        Vector3 v = carRB.linearVelocity;
        float vMag = v.magnitude;
        if (vMag > 0.01f)
        {
            float dragFactor = aeroDragVsSpeed.Evaluate(speed01);

            // quadratic-ish drag feel
            float quad = speed01 * speed01;

            Vector3 dragAcceleration = -(v / vMag) * (aeroDragAccel * dragFactor * quad);

            carRB.AddForce(dragAcceleration, ForceMode.Acceleration);
        }

        // apply engine at your acceleration point
        carRB.AddForceAtPosition(engineAccelerationVelocity, accelerationPoint.position, ForceMode.Acceleration);

    }

    private void Deceleration()
    {
        carRB.AddForceAtPosition(acceleration * moveInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Turn()
    {
        // local forward speed
        float forwardSpeed = currentCarLocalVelocity.z;

        // 0..1 speed factory based on how fast we're moving
        float speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed / maxSpeed));

        float steerFactor = steeringCurve.Evaluate(speed01);
        
        // if nearly stoppped, allow steering
        float direction = (Mathf.Abs(forwardSpeed) < 0.5f) ? 1f : Mathf.Sign(forwardSpeed);

        // yaw torque
        float yawTorque = steerStrength * steerInput * steerFactor * direction;

        carRB.AddTorque(transform.up * yawTorque, ForceMode.Acceleration);

        float throttle01 = Mathf.Clamp01(Mathf.Abs(moveInput));

        float yawRate = Vector3.Dot(carRB.angularVelocity, transform.up);
        float dampScale = Mathf.Lerp(1.0f, 0.2f, throttle01);
        carRB.AddTorque(-transform.up * yawRate * yawDamping * dampScale, ForceMode.Acceleration);

    }

    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        float throttle01 = Mathf.Clamp01(Mathf.Abs(moveInput));
        float gripScale = Mathf.Lerp(1.0f, 0.5f, throttle01);

        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient * gripScale;

        Vector3 dragForce = transform.right * dragMagnitude;

        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }

    private void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLenght = restLength + springTravel;

            if(Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLenght + wheelRadius, layerMask))
            {

                wheelsAreGrounded[i] = true;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = restLength - currentSpringLength / springTravel;

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float dampForce = dampenerStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);
                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsAreGrounded[i] = false;

                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLenght) * -rayPoints[i].up, Color.green);
            }
        }
    }

}
